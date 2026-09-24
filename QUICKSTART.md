# Quickstart — gregMod.FiberTrunk

> Pull one trunk cable between two fiber panels — complete 12, 24 or 48 strands.

Repo: [https://github.com/mleem97/gregMod.FiberTrunk](https://github.com/mleem97/gregMod.FiberTrunk) · Version: `0.1.0` · License: Apache-2.0.

## 1. Clone

```bash
git clone https://github.com/mleem97/gregMod.FiberTrunk.git
cd gregMod.FiberTrunk
```

## 2. Build

```bash
# Sync game/loader assemblies first (repo root helper)
../ModRepositories/tools/sync-melon-assemblies.sh

dotnet build gregMod.FiberTrunk.csproj -c Release
```

The DLL lands in `bin/Release/net6.0/gregMod.FiberTrunk.dll`.

## 3. Install

Copy the DLL to the game Mods folder:

```bash
# Linux example
cp bin/Release/net6.0/gregMod.FiberTrunk.dll \
  "$HOME/.local/share/Steam/steamapps/common/Data Center/Mods/"
```

(Or from the repo root: `./build.sh FiberTrunk --deploy`.)

## 4. Use

1. Start the game (with or without gregCore).
2. With gregCore: press **F1** -> Mod Config -> `gregMod.FiberTrunk` ->
   set **Strands per trunk** to `24` (or `12` / `48`).
   Without gregCore: press **F4** and pick a preset in the panel.
3. Click a port on fiber **panel A**, walk & manage the trunk route once —
   one thick ghost follows you.
4. Click a port on fiber **panel B**. The remaining strands are created
   automatically on free ports of both panels.
5. Non-panel clicks (switches, servers) always stay normal 1-to-1 cables.

Details: [README.md](README.md), [docs/USAGE.md](docs/USAGE.md),
[docs/COMPATIBILITY.md](docs/COMPATIBILITY.md).
If you run into problems: file an issue
([Issues](https://github.com/mleem97/gregMod.FiberTrunk/issues)) or read
[CONTRIBUTING.md](CONTRIBUTING.md).
