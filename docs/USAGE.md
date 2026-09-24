# Usage — gregMod.FiberTrunk

## Goal

Pull **one trunk cable** between two fiber (patch) panels and get **12, 24
or 48 strands** over the single walked route — like a real-life 24fo trunk
for long distances — instead of pulling 24 individual cables.

## Setup

1. Install the DLL (see [QUICKSTART.md](../QUICKSTART.md)).
2. Set the strand count:
   - **With gregCore:** press **F1** → Mod Config → `gregMod.FiberTrunk` →
     **Strands per trunk** (`1–48`, default `24`).
   - **Without gregCore:** press **F4** and hit a **12 / 24 / 48** preset
     (or move the slider).
3. Recommended: leave **Auto-trunk** ON. `Require fibre ports` stays OFF
   unless your panels flag fibre ports and you want the gate.

## Trunk workflow (example: 24fo, panel A → panel B)

1. Click a **free port** on fiber **panel A** (a physical panel port).
   Status switches to `Carrying trunk cable <id>` and one **thick ghost**
   follows your vanilla cable.
2. Walk to panel B and manage the trunk route exactly like vanilla (trays,
   ladders, long hall — one trip).
3. Click a **free port** on fiber **panel B**.
   - The vanilla cable completes normally (route evaluation runs).
   - ~1.5 seconds later the mod creates the remaining **23 strands** on free
     ports of panel A and panel B and re-runs route evaluation.
4. Check the panel `Last:` line, e.g.
   `Trunk x24: cable 41 + 23 strand(s) [42,43,…].`.
5. If the panels are partially occupied, the mod creates what fits and says
   so (e.g. a 24fo trunk on panels with 20 free pairs yields 20 strands).

## Notes

- **Panel-to-panel only.** Clicks on switches, servers, or mixed endpoints
  never trigger trunk mode — they stay normal 1-to-1 cables with an
  explanatory status line.
- Strands need **free panel ports on both panels**. Matching: same panel,
  same link type, same SFP/fibre character, unoccupied, nearest-first.
- Each strand consumes spool length like a normal cable. A 24fo trunk is
  24× length — bring a big spool; if it runs out, vanilla gates apply and
  the strand is discarded with a warning. The origin cable is never affected.
- Strands are **real vanilla cables**: they save/load, replicate in co-op
  through the same code paths as clicked cables, and carry traffic.
- Click **physical panel ports**. Config-UI port buttons are a different
  flow and are not tracked as endpoints.
- **Stop tracking (keeps cable)** only clears the mod's observation state; it
  never discards your live cable.

## Settings reference

| Setting | F1 (gregCore) | F4 panel (no gregCore) | Default | Meaning |
|---|---|---|---|---|
| Enabled | yes | toggle | ON | Master switch; OFF = vanilla behaviour |
| Strands per trunk | yes (1–48) | presets + slider | 24 | Strands per panel-to-panel pull |
| Require fibre ports | yes | toggle | OFF | Track only fibre-flagged panel ports |
| Auto-trunk | yes | toggle | ON | Create strands on completion |
| Trunk ghost color | prefs only | — | `#FFA500` | HTML color of the thick preview line |
| Trunk ghost width | prefs only | — | `2.5×` | Thickness multiple of cable width (cosmetic) |
| Strand delay | prefs only | — | `1.5 s` | Settle time before strand creation |
| Panel hotkey | prefs only | — | `F4` | Opens/closes the panel |

`prefs only` = `MelonPreferences.cfg`, category `gregMod.FiberTrunk`.
When gregCore is installed, the four core settings are owned by F1 and the
panel shows them read-only.
