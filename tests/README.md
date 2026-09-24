# tests — gregMod.FiberTrunk

Tests, fixtures, and test documentation.

Back: [README.md](../README.md) · Docs: [docs/INDEX.md](../docs/INDEX.md).

## What can be tested without the game

- `dotnet build gregMod.FiberTrunk.csproj -c Release` must stay at
  **0 warnings, 0 errors** (interop-signature drift shows up here first).

## In-game checklist (see docs/COMPATIBILITY.md)

1. Mod loads; F4 panel opens; 12/24/48 presets work; no console errors.
2. 24fo pull panel→panel: thick ghost follows; 23 strands land on free ports.
3. Partial occupancy reporting; non-panel clicks stay 1-to-1 passthrough.
4. Discard mid-carry clears tracking; failures never touch the origin cable.
5. Save/load keeps strands; co-op replicates.
6. With gregCore (F1 entries own the settings) and without (panel edits prefs).
