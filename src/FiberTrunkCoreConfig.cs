using System;
using MelonLoader;

namespace GregMod.FiberTrunk
{
    /// <summary>
    /// All direct gregCore references live in this class and this class only.
    /// It is called exclusively behind <see cref="GregHost.HasCore"/>, so the
    /// mod still loads when gregCore is absent (JIT split: referencing methods
    /// are never jitted without gregCore present).
    ///
    /// Covers:
    ///  - F1 config UI entries (DataCenterModLoader.ModConfigSystem),
    ///  - mod registry / HUD key hint / F1 hub panel opener.
    /// </summary>
    internal static class FiberTrunkCoreConfig
    {
        internal static void RegisterEntries()
        {
            DataCenterModLoader.ModConfigSystem.RegisterBool(
                FiberTrunkConfig.ModId, "Enabled", "Enabled", true,
                "Master switch. When off, the game behaves exactly like vanilla (1-to-1 cables).");
            DataCenterModLoader.ModConfigSystem.RegisterInt(
                FiberTrunkConfig.ModId, "StrandCount", "Strands per trunk", FiberTrunkConfig.DefaultStrands,
                FiberTrunkConfig.MinStrands, FiberTrunkConfig.MaxStrands,
                "How many fiber strands one panel-to-panel pull creates (presets 12/24/48).");
            DataCenterModLoader.ModConfigSystem.RegisterBool(
                FiberTrunkConfig.ModId, "RequireFibrePorts", "Require fibre ports", false,
                "When on, trunk tracking only starts on ports flagged as fibre ports.");
            DataCenterModLoader.ModConfigSystem.RegisterBool(
                FiberTrunkConfig.ModId, "AutoTrunk", "Auto-create trunk strands", true,
                "When the carried trunk cable is completed panel-to-panel, auto-create the remaining strands.");
        }

        internal static bool GetBoolValue(string modId, string key, bool fallback)
        {
            try { return DataCenterModLoader.ModConfigSystem.GetBoolValue(modId, key, fallback); }
            catch { return fallback; }
        }

        internal static int GetIntValue(string modId, string key, int fallback)
        {
            try { return DataCenterModLoader.ModConfigSystem.GetIntValue(modId, key, fallback); }
            catch { return fallback; }
        }

        internal static void RegisterHub(string version, Action onOpen, Action onClose)
        {
            try
            {
                gregCore.Core.Mods.GregModRegistry.Register(
                    FiberTrunkConfig.ModId, "FiberTrunk", version,
                    new string[] { "fibertrunk" });
                gregCore.UI.GregHudRegistry.Register(
                    "fibertrunk", FiberTrunkConfig.ToggleKey.ToString(), "FiberTrunk");
                gregCore.UI.GregMenuRegistry.RegisterOpener("fibertrunk", () =>
                {
                    try { onOpen?.Invoke(); } catch { /* best-effort */ }
                });
                gregCore.UI.GregMenuRegistry.RegisterCloser("fibertrunk", () =>
                {
                    try { onClose?.Invoke(); } catch { /* best-effort */ }
                });
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[FiberTrunk] Hub registration failed: " + ex.GetBaseException().Message);
            }
        }
    }
}
