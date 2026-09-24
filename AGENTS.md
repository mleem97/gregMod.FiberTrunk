# AGENTS.md — Notes for AI agents (gregMod.FiberTrunk)

Repo: [https://github.com/mleem97/gregMod.FiberTrunk](https://github.com/mleem97/gregMod.FiberTrunk) · License: Apache-2.0 · Version: see `VERSION`.

## Duties

1. **Read first:** `README.md`, `docs/INDEX.md`, `CONTRIBUTING.md` — only then make changes.
2. **Do not commit secrets** (keys, tokens, `.env`). Use keys only via environment variables.
3. **Preserve history:** no `push --force`, no history rewrite without instruction.
4. **Verify changes:** before reporting completion, build/test whatever the repo supports (`QUICKSTART.md`).
5. **Keep docs in sync:** for new features, update `README.md` + `docs/` + `CHANGELOG.md` (Unreleased).
6. **Conventions:** Conventional Commits (`feat:`, `fix:`, `docs:`, `chore:` …), one logical change per commit.
7. **When in doubt:** stop and ask instead of guessing — especially for deletes, migrations, CI.

## Mod-specific rules

- **Panel-to-panel scope is the core promise.** `TrunkScope.IsPanelPort`
  gates both carry start and completion; non-panel pulls must stay vanilla
  1-to-1 with an explanatory status line, never a trunk.
- **Never suppress vanilla cable behaviour.** All Harmony patches are
  observational; the only additive step is `TrunkCloner.Execute`, which runs
  delayed (strand pump in `TrunkGhostManager.Update`) and guarded by
  `TrunkCloner.IsCloning` against recursion.
- **gregCore is a soft dependency.** Direct gregCore references live only in
  `src/FiberTrunkCoreConfig.cs`, called exclusively behind `GregHost.HasCore`
  (JIT split). The mod must load and work without gregCore (F4 panel +
  MelonPreferences).
- **0Harmony.dll ships v1 (`Harmony`) and v2 (`HarmonyLib`) APIs.** Always use
  fully qualified `HarmonyLib.Harmony` / `HarmonyLib.HarmonyMethod`.
- **No Il2Cpp-crossing delegates.** The F4 panel uses `GUILayout.BeginArea`,
  not `GUILayout.Window` (its `GUI.WindowFunction` delegate is unreliable
  under IL2CPP interop).
- **Panel key is F4** (F7 = Trainer, F8 = MultiCable, F9 = MusicPlayer,
  F10 = NotesHUD). Do not collide.
- **Reverse-engineering evidence** for game internals belongs in
  `docs/COMPATIBILITY.md`. The `Assembly-CSharp` interop dummies contain no
  IL — call graphs beyond signatures must be confirmed at runtime.
- **Lineage:** the observe-and-replay engine is shared with gregMod.MultiCable
  by copy (one repo per mod). Keep both implementations in sync when fixing
  engine-level bugs, but never add a hard dependency between the mods.

## Layout

See [README.md](README.md) → Repository Layout. Central entry points: `docs/INDEX.md`, `scripts/`, `tests/`.
Source: `src/` (`FiberTrunkMod`, `FiberTrunkConfig`, `FiberTrunkCoreConfig`,
`TrunkState`, `Patches` + `TrunkScope`, `PanelPortFinder`, `TrunkCloner`,
`TrunkGhostManager`).
