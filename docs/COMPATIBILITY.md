# Compatibility — gregMod.FiberTrunk

## Baseline (built and signature-checked against)

- Game: **Data Center** by **Waseku** (`app.info`: `Waseku / Data Center`).
- Interop assemblies dated **2026-09-19** (`MelonLoader/Il2CppAssemblies`,
  `GameAssembly.dll` of the local Steam install).
- Loader: **MelonLoader 0.7.x**, mod target **net6.0-x64**.
- `gregCore` (optional): soft dependency only — mod loads and works without it.
- Platforms: Windows x64 / Linux x64 (same assemblies as sibling Greg mods).

## Reverse-engineering evidence (method signatures from interop dummies)

Patch panels — `Il2Cpp.PatchPanel : UsableObject`:

- `cableLinkPorts: CableLink[]`, `patchPanelId: String`, `patchPanelType: Int32`
- `GetPairedLink(CableLink)`, `IsAnyCableConnected()`, `InteractOnClick()`,
  `GenerateUniquePatchPanelId()`

Ports — `Il2Cpp.CableLink : Interact` (scope gate reads these):

- `parentPatchPanel: PatchPanel` (null unless panel-bound),
  `typeOfLink: TypeOfLink (None/Server/Switch/Base/LB/PatchPanel)`,
  `isSFPPort / isFibrePort`, `cableIDsOnLink` (occupancy),
  `switchID`, parents (`parentSwitch/parentServer/parentPatchPanel/
  parentInternet`), `transform`

Cable geometry — `Il2Cpp.CablePositions` (`instance` singleton):

- `Int32 CreateNewCable()`, `Int32 ReserveCableId()`
- `AssignNewPosition(Int32 cableId, Transform linkTransform, Boolean isStartPoint,
  Boolean isEndPoint, CableLink.TypeOfLink typeOfLink, String serverID)`
- `GenerateFinalPath(Int32)`, `DiscardCable(Int32)`, `Boolean IsCableComplete(Int32)`
- `List<Vector3> GetCablePositions(Int32)` / `GetRawCablePositions(Int32)`
- Properties: `activeCableId`, `cableWidth`, `currentCableLength`, `totalCableLengthLaid`

Network logic — `Il2Cpp.WaypointInitializationSystem` (`Instance` singleton):

- `CreateCableWithSpawners(Int32, List<Vector3>)`, `RequestRouteEvaluation()`

Connection record — `Il2Cpp.NetworkMap` (instance cached from patch):

- `RegisterCableConnection(Int32 cableId, Vector3 startPos, Vector3 endPos,
  TypeOfLink startType, TypeOfLink endType, String startSwitchID,
  String endSwitchID, Int32 startCustomerID, Int32 endCustomerID,
  String startServerID, String endServerID)`

Spool — `Il2Cpp.CableSpinner`: `cableLenght / cableLenghtInUse` (sic),
`cableType: Int32`, `IsCableLenghtEnough()`, `LowerAmountOfCable(Single)`.

> The interop dummies contain **no IL** — beyond signatures, vanilla call order
> is inferred from runtime seams, not asserted. The mod therefore replays only
> public entry points and verifies each step (`IsCableComplete`, try/catch +
> `DiscardCable` rollback).

## Known limits (v1)

1. **In-game verification pending** — build passes (`0 warnings, 0 errors`);
   load/carry/strand behaviour must still be confirmed in a live game
   (see `tests/README.md` checklist).
2. **Physical panel ports only.** Config-UI port buttons are not tracked as
   endpoints; the trunk then skips with an explanatory message.
3. **Port availability rules.** A 24fo trunk needs 24 free pairs; partially
   occupied panels yield partial trunks (reported, not fatal).
4. **Spool consumption** follows whatever the replayed vanilla path does (24×
   length for a 24fo trunk) — bring a big spool; failures are reported, the
   origin cable is never affected.
5. Game updates that rename the four seam methods degrade the mod to
   **vanilla passthrough with a console warning** (fail-safe by design).

## Test status

- [x] `dotnet build -c Release` — clean (0 warnings, 0 errors).
- [ ] Mod loads in game, F4 panel opens, presets work, no console errors.
- [ ] 24fo pull panel→panel: thick ghost follows, 23 strands land on free ports.
- [ ] 12/48 presets; partial-occupancy reporting; non-panel clicks passthrough.
- [ ] Discard mid-carry clears tracking; failures never touch the origin cable.
- [ ] Save/load keeps strands; co-op replicates.
- [ ] With and without gregCore (F1 entries vs local panel editing).
