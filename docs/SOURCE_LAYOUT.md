# Source layout

All C# source lives in `src/`, current game/MelonLoader assemblies in `references/`
(symlinks into the local Data Center install — never commit DLLs), and project
documentation in `docs/`.

| File | Why it exists |
|---|---|
| `src/FiberTrunkMod.cs` | MelonMod entry point (startup, scenes, hotkey) |
| `src/MyPluginInfo.cs` | Plugin id/name/version constants |
| `src/GregHost.cs` | gregCore soft-dependency probe |
| `src/FiberTrunkConfig.cs` | Effective config incl. 12/24/48 presets (F1 wins, prefs fallback) |
| `src/FiberTrunkCoreConfig.cs` | Only file referencing gregCore (JIT split) |
| `src/TrunkState.cs` | Tracked pull, connection record, strand queue |
| `src/Patches.cs` | Observational Harmony seams + `TrunkScope` panel gate |
| `src/PanelPortFinder.cs` | Free same-panel port search |
| `src/TrunkCloner.cs` | Delayed strand creation via vanilla methods |
| `src/TrunkGhostManager.cs` | Thick trunk ghost, strand pump, F4 preset panel |
