# Architecture — gregMod.FiberTrunk

> One walked route, a full trunk. Vanilla flow is never suppressed;
> non-panel pulls stay 1-to-1.

## Components

```text
panel port click ──► Patches + TrunkScope ──► TrunkState ──► TrunkGhostManager ──► TrunkCloner
   │                       │                       │                   │                    │
   │ vanilla cable         │ track iff             │ tracked id,       │ ONE thick ghost +  │ replay vanilla
   │ flows untouched       │ panel-to-panel        │ records, jobs     │ delayed job pump   │ entry points
```

| File | Responsibility |
|---|---|
| `src/FiberTrunkMod.cs` | MelonMod entry: config load, Harmony apply, gregCore wiring, F4 hotkey, manager bootstrap per scene |
| `src/FiberTrunkConfig.cs` | Two-layer config: MelonPreferences always; gregCore F1 (`ModConfigSystem`) wins when present. Effective values resolved on demand |
| `src/FiberTrunkCoreConfig.cs` | **Only** file with direct gregCore references (JIT split behind `GregHost.HasCore`): F1 entries, hub/HUD/panel-opener registration |
| `src/GregHost.cs` | Soft-dependency probe (`Type.GetType`, no hard load) |
| `src/TrunkState.cs` | Tracked cable id, start/end click candidates, authoritative `ConnectionRecord`, cached `NetworkMap` instance, delayed `CloneJob` queue, panel status |
| `src/Patches.cs` | 4 observational Harmony seams + `TrunkScope` panel gate (see below); recursion guard via `TrunkCloner.IsCloning` |
| `src/PanelPortFinder.cs` | Free-port search on the same panel (parent-panel equality, type + SFP/fibre match, `cableIDsOnLink == 0`), nearest-first |
| `src/TrunkCloner.cs` | Delayed strand creation via `ReserveCableId` → 2× `AssignNewPosition` → `GenerateFinalPath` if needed → `RequestRouteEvaluation`; per-strand rollback with `DiscardCable` |
| `src/TrunkGhostManager.cs` | Il2Cpp `MonoBehaviour`: per-frame single thick trunk ghost from the live vanilla waypoint list; strand-queue pump; F4 `BeginArea` panel with 12/24/48 presets |
| `src/MyPluginInfo.cs` | Plugin id / name / version constants |

## Harmony seams (all observational)

| Target | Hook | Purpose |
|---|---|---|
| `CableLink.InteractOnClick` | prefix (record only) | Capture clicked port; role (start/end) inferred from carry state |
| `CablePositions.CreateNewCable` | postfix | Detect pull start → `TrunkState.Begin(__result)` **iff** the start port is a panel port (`TrunkScope`) |
| `NetworkMap.RegisterCableConnection` | postfix | Authoritative completion record; enqueue strand job **iff** the end port is a panel port, else untrack with info |
| `CablePositions.DiscardCable` | postfix | Cancel tracking when the carried cable is discarded |

No prefix returns `false` anywhere. The mod adds exactly one behaviour:
delayed strand creation through public vanilla methods, panel-to-panel only.

## Data flows

1. **Carry:** panel click → `CreateNewCable` id → `Begin(id)` → manager copies
   `CablePositions.GetCablePositions(id)` (fallback `GetRawCablePositions`)
   each frame into ONE thick `LineRenderer` (width × factor, slight lift).
2. **Complete:** `RegisterCableConnection` args → `ConnectionRecord` (+ links
   from click candidates) → scope check → `CloneJob{ExecuteAt = now +
   CloneDelaySec}`.
3. **Strands:** pump dequeues → `PanelPortFinder` pairs free panel ports →
   per strand: `ReserveCableId`, `AssignNewPosition(start)`,
   `AssignNewPosition(end)`, `GenerateFinalPath` if `!IsCableComplete`, else
   `DiscardCable` + warn → single `RequestRouteEvaluation` at the end.
4. **Config:** F1 `ModConfigSystem` (when gregCore present) else
   `MelonPreferences`; panel reads effective values, presets/slider edit
   prefs only when F1 does not own them.

## Failure handling

- Seam missing at startup → warning, degraded tracking, vanilla unaffected.
- Non-panel endpoint → silent untrack + info line (normal cable preserved).
- Strand step throws → that strand is discarded, origin cable untouched.
- No free panel pair → job reports and stops (partial pairs still created).
- Own strand completions re-enter the `RegisterCableConnection` postfix but
  return early under `IsCloning` — no grandchild jobs, no infinite loop.

## Lineage

Same observe-and-replay engine as gregMod.MultiCable, by copy (one repo per
mod, no hard dependency). Engine-level bug fixes should be mirrored in both
repos.

Record changes here + [`CHANGELOG.md`](../CHANGELOG.md) (Unreleased).
