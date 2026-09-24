# Changelog — gregMod.FiberTrunk

Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/). Version: see [`VERSION`](VERSION).

## [Unreleased]

## [0.1.0] — 2026-09-24

### Added

- Initial release: panel-to-panel fiber trunks.
- Strand presets 12/24/48 (free range 1–48, default 24) via gregCore F1 Mod
  Config UI (`gregMod.FiberTrunk` -> `Strands per trunk`) with
  MelonPreferences fallback + F4 preset panel.
- Panel-to-panel scope: pulls started/completed on non-panel ports stay
  vanilla 1-to-1; optional `RequireFibrePorts` gate.
- Single thick trunk preview ghost while carrying (width factor + color
  configurable).
- Automatic strand creation on completion onto free ports of the same two
  fiber panels, replayed through vanilla methods (`ReserveCableId` /
  `AssignNewPosition` / `GenerateFinalPath` + route re-evaluation).
- Standalone F4 panel (status, presets, toggles, stop-tracking), gregCore HUD +
  F1 hub opener registration.
- Observational Harmony seams only (`CableLink.InteractOnClick`,
  `CablePositions.CreateNewCable` / `DiscardCable`,
  `NetworkMap.RegisterCableConnection`); no vanilla behaviour suppressed.
- Recursion guard, per-strand error isolation with `DiscardCable` rollback,
  partial-success reporting (e.g. 24fo trunk on partially occupied panels).
- Docs: `docs/USAGE.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`.
- Engine lineage: same observe-and-replay pattern as gregMod.MultiCable.
