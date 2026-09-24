# gregMod.FiberTrunk

> Pull **one** trunk cable between two fiber panels — complete **12, 24 or 48 strands**.

Copy `gregMod.FiberTrunk.dll` to `Data Center/Mods/`.

![License](https://img.shields.io/github/license/mleem97/gregMod.FiberTrunk?style=for-the-badge)

## Links

- **Repository:** [https://github.com/mleem97/gregMod.FiberTrunk](https://github.com/mleem97/gregMod.FiberTrunk)
- **Issues:** [https://github.com/mleem97/gregMod.FiberTrunk/issues](https://github.com/mleem97/gregMod.FiberTrunk/issues)
- **Releases:** [https://github.com/mleem97/gregMod.FiberTrunk/releases](https://github.com/mleem97/gregMod.FiberTrunk/releases)

## Overview

In real life you pull **one 24-fiber (or larger) trunk** to link two fiber
panels over a long distance — not 24 individual cables. The vanilla game only
does 1-to-1 pulls.

**gregMod.FiberTrunk** keeps the vanilla flow intact and adds, for
**panel-to-panel** pulls:

1. **One thick trunk ghost** following you while you carry the cable.
2. **Automatic strand creation** on completion: the remaining strands
   (12/24/48, default 24) are replayed through vanilla's own methods onto
   **free ports of the same two fiber panels** — one walked route, a full
   trunk of discrete fiber strands.

Set the strand count in the gregCore **F1 Mod Config UI**
(`gregMod.FiberTrunk` -> `Strands per trunk`), or pick the **12/24/48
presets** in the **F4 panel** when gregCore is not installed. Clicks on
non-panel ports always stay normal 1-to-1 cables.

See [docs/INDEX.md](docs/INDEX.md) for the complete documentation, and
[docs/USAGE.md](docs/USAGE.md) for the workflow.

## Compatibility

| Platform    | Status    |
| ----------- | --------- |
| Windows x64 | Supported |
| Linux x64   | Supported |

Game/loader baseline: see [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md).

## Features

- Strand presets 12/24/48 (free range 1–48, default 24).
- Panel-to-panel scope: switches/servers never trigger trunk mode.
- Optional fibre-port requirement (`RequireFibrePorts`, default off).
- Single thick trunk preview ghost (width factor + color configurable).
- Standalone F4 panel + gregCore F1 config entries + HUD registration.
- No vanilla behaviour is suppressed or replaced; failed strands are discarded
  and reported, the original cable is never touched.
- Proven engine lineage: same observe-and-replay pattern as
  [gregMod.MultiCable](https://github.com/mleem97/gregMod.MultiCable).

## Installation

See [QUICKSTART.md](QUICKSTART.md).

## Build from Source

```bash
git clone https://github.com/mleem97/gregMod.FiberTrunk.git
cd gregMod.FiberTrunk
dotnet build gregMod.FiberTrunk.csproj -c Release
```

Details: [QUICKSTART.md](QUICKSTART.md), [CONTRIBUTING.md](CONTRIBUTING.md).

## Repository Layout

```
├── README.md            # This file
├── QUICKSTART.md        # Quickstart
├── CHANGELOG.md         # Changelog (Keep a Changelog)
├── CONTRIBUTING.md      # Contributing
├── SECURITY.md          # Security reports
├── CODE_OF_CONDUCT.md   # Code of conduct
├── AGENTS.md            # Notes for AI agents
├── LICENSE              # Apache-2.0
├── VERSION              # Single source of truth for the version
├── manifest.json        # Mod manifest
├── docs/                # Documentation ([Index](docs/INDEX.md))
├── scripts/             # Build/helper scripts
├── tests/               # Tests
├── references/          # Game/loader assemblies (symlinks, never committed)
├── examples/            # Examples
└── src/                 # C# source
```

## API Documentation

See [`docs/INDEX.md`](docs/INDEX.md).

## Credits

| Role       | Contributor                                        |
| ---------- | -------------------------------------------------- |
| **Codebase** | [mleem97](https://github.com/mleem97)            |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

Apache-2.0 — see [`LICENSE`](LICENSE).

## 🚀 Join the gregFramework Team!

Do you enjoy building mods, tools, or docs? Get in touch: **apply@gregframework.eu** or via
[Discord](https://discord.gg/greg) — Code, Assets, Docs, Testing, Infra, Community.

---

**gregFramework — powered by the community.**
