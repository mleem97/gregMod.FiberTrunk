using System;
using MelonLoader;
using UnityEngine.InputSystem;

namespace GregMod.FiberTrunk
{
    /// <summary>
    /// Configuration for gregMod.FiberTrunk.
    ///
    /// Two-layer model (same as gregMod.MultiCable):
    ///  1. MelonPreferences (always available, editable in MelonPreferences.cfg).
    ///  2. gregCore ModConfigSystem / F1 config UI (when gregCore is installed).
    ///     F1 values take precedence at read time; the F4 panel shows them
    ///     read-only in that case. Without gregCore the F4 panel edits the
    ///     MelonPreferences values directly (including the 12/24/48 presets).
    ///
    /// Effective values are resolved on demand (every trunk completion and every
    /// panel repaint) so F1 edits apply without a restart.
    /// </summary>
    internal static class FiberTrunkConfig
    {
        internal const string ModId = "gregMod.FiberTrunk";

        internal const int MinStrands = 1;
        internal const int MaxStrands = 48;

        /// <summary>Real-life trunk presets offered as one-click buttons.</summary>
        internal static readonly int[] StrandPresets = { 12, 24, 48 };

        internal const int DefaultStrands = 24;

        private static MelonPreferences_Category _cat;
        private static MelonPreferences_Entry<bool> _enabled;
        private static MelonPreferences_Entry<int> _strandCount;
        private static MelonPreferences_Entry<bool> _requireFibrePorts;
        private static MelonPreferences_Entry<string> _trunkColor;
        private static MelonPreferences_Entry<float> _trunkWidthFactor;
        private static MelonPreferences_Entry<bool> _autoTrunk;
        private static MelonPreferences_Entry<float> _cloneDelaySec;
        private static MelonPreferences_Entry<string> _toggleKey;

        internal static Key ToggleKey = Key.F4;

        internal static void Load()
        {
            try
            {
                _cat = MelonPreferences.CreateCategory(ModId, "FiberTrunk");
                _enabled = _cat.CreateEntry("Enabled", true, "Enabled",
                    "Master switch. When off, the game behaves exactly like vanilla (1-to-1 cables).");
                _strandCount = _cat.CreateEntry("StrandCount", DefaultStrands, "Strands per trunk",
                    "How many fiber strands one panel-to-panel pull creates (presets 12/24/48, range 1-48). Also editable in the gregCore F1 config UI.");
                _requireFibrePorts = _cat.CreateEntry("RequireFibrePorts", false, "Require fibre ports",
                    "When on, trunk tracking only starts on ports flagged as fibre ports. Off = any patch-panel ports.");
                _trunkColor = _cat.CreateEntry("TrunkColor", "#FFA500", "Trunk ghost color",
                    "HTML color of the trunk preview ghost (e.g. #FFA500 orange).");
                _trunkWidthFactor = _cat.CreateEntry("TrunkWidthFactor", 2.5f, "Trunk ghost width factor",
                    "Thickness of the single trunk preview line, as a multiple of the normal cable width. Cosmetic only.");
                _autoTrunk = _cat.CreateEntry("AutoTrunk", true, "Auto-create trunk strands",
                    "When the carried trunk cable is completed panel-to-panel, automatically create the remaining strands on free panel ports.");
                _cloneDelaySec = _cat.CreateEntry("CloneDelaySec", 1.5f, "Strand delay (s)",
                    "Delay after trunk completion before strands are created, so vanilla route evaluation can settle.");
                _toggleKey = _cat.CreateEntry("ToggleKey", "F4", "Panel hotkey",
                    "Input System key opening the FiberTrunk panel (e.g. F4, F6, F11).");
                _cat.SaveToFile(false);

                if (Enum.TryParse<Key>(_toggleKey.Value, true, out var k) && k != Key.None)
                    ToggleKey = k;
                else
                    MelonLogger.Warning($"[FiberTrunk] Unknown ToggleKey '{_toggleKey.Value}', defaulting to F4.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Config load failed, using built-in defaults: {ex.GetBaseException().Message}");
            }
        }

        // ── Effective values (F1 wins when gregCore is present) ──────────────

        internal static bool Enabled => GetBool("Enabled", _enabled, true);

        internal static int StrandCount
        {
            get
            {
                int v = GetInt("StrandCount", _strandCount, DefaultStrands);
                if (v < MinStrands) v = MinStrands;
                if (v > MaxStrands) v = MaxStrands;
                return v;
            }
        }

        internal static bool RequireFibrePorts => GetBool("RequireFibrePorts", _requireFibrePorts, false);

        internal static bool AutoTrunk => GetBool("AutoTrunk", _autoTrunk, true);

        internal static string TrunkColorRaw => _trunkColor != null ? _trunkColor.Value : "#FFA500";

        internal static float TrunkWidthFactor
        {
            get
            {
                float v = _trunkWidthFactor != null ? _trunkWidthFactor.Value : 2.5f;
                if (v < 1f) v = 1f;
                if (v > 6f) v = 6f;
                return v;
            }
        }

        internal static float CloneDelaySec
        {
            get
            {
                float v = _cloneDelaySec != null ? _cloneDelaySec.Value : 1.5f;
                if (v < 0f) v = 0f;
                if (v > 10f) v = 10f;
                return v;
            }
        }

        /// <summary>True while the F1 gregCore config UI owns the core settings.</summary>
        internal static bool F1OwnsCoreSettings => GregHost.HasCore;

        // ── Panel write path (prefs only; used when gregCore is absent) ──────

        internal static void SetEnabled(bool v)
        {
            if (_enabled == null) return;
            _enabled.Value = v;
            MelonPreferences.Save();
        }

        internal static void SetStrandCount(int v)
        {
            if (_strandCount == null) return;
            if (v < MinStrands) v = MinStrands;
            if (v > MaxStrands) v = MaxStrands;
            _strandCount.Value = v;
            MelonPreferences.Save();
        }

        internal static void SetRequireFibrePorts(bool v)
        {
            if (_requireFibrePorts == null) return;
            _requireFibrePorts.Value = v;
            MelonPreferences.Save();
        }

        internal static void SetAutoTrunk(bool v)
        {
            if (_autoTrunk == null) return;
            _autoTrunk.Value = v;
            MelonPreferences.Save();
        }

        // ── Backing-store helpers ─────────────────────────────────────────────

        private static bool GetBool(string key, MelonPreferences_Entry<bool> pref, bool fallback)
        {
            // F1 (gregCore ModConfigSystem) takes precedence when available.
            if (GregHost.HasCore)
            {
                try { return FiberTrunkCoreConfig.GetBoolValue(ModId, key, pref != null ? pref.Value : fallback); }
                catch { /* fall through to prefs */ }
            }
            try { return pref != null ? pref.Value : fallback; }
            catch { return fallback; }
        }

        private static int GetInt(string key, MelonPreferences_Entry<int> pref, int fallback)
        {
            if (GregHost.HasCore)
            {
                try { return FiberTrunkCoreConfig.GetIntValue(ModId, key, pref != null ? pref.Value : fallback); }
                catch { /* fall through to prefs */ }
            }
            try { return pref != null ? pref.Value : fallback; }
            catch { return fallback; }
        }

        // ── gregCore registration (called only when HasCore; own type for JIT split)
        internal static void RegisterF1Entries()
        {
            try { FiberTrunkCoreConfig.RegisterEntries(); }
            catch (Exception ex)
            {
                MelonLogger.Warning("[FiberTrunk] F1 config registration failed: " + ex.GetBaseException().Message);
            }
        }
    }
}
