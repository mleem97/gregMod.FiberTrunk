using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(GregMod.FiberTrunk.FiberTrunkMod),
    GregMod.FiberTrunk.MyPluginInfo.PLUGIN_NAME,
    GregMod.FiberTrunk.MyPluginInfo.PLUGIN_VERSION,
    GregMod.FiberTrunk.MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonGame("Waseku", "Data Center")]

namespace GregMod.FiberTrunk
{
    /// <summary>
    /// gregMod.FiberTrunk — pull one trunk cable between two fiber panels,
    /// complete a full multi-fiber trunk (12/24/48 strands).
    ///
    /// The vanilla game connects exactly one cable per pull (1-to-1). This mod
    /// keeps that flow intact and adds, on top, for panel-to-panel pulls:
    ///  - one thick trunk ghost following you while carrying, and
    ///  - automatic strand creation on completion: the remaining strands are
    ///    replayed through vanilla's own methods onto free ports of the same
    ///    two fiber panels — one walked route, a full trunk.
    ///
    /// Set the strand count in the gregCore F1 Mod Config UI
    /// ("gregMod.FiberTrunk" -> "Strands per trunk"), or in the F4 panel when
    /// gregCore is not installed. Non-panel clicks always stay vanilla 1-to-1.
    /// </summary>
    public sealed class FiberTrunkMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            try
            {
                TrunkGhostManager.RegisterIl2CppType();
                FiberTrunkConfig.Load();

                var harmony = new HarmonyLib.Harmony("com.gregmod.fibertrunk");
                Patches.Apply(harmony);

                if (GregHost.HasCore)
                {
                    try
                    {
                        FiberTrunkConfig.RegisterF1Entries();
                        FiberTrunkCoreConfig.RegisterHub(
                            MyPluginInfo.PLUGIN_VERSION,
                            TrunkGhostManager.OpenPanel,
                            TrunkGhostManager.ClosePanel);
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Warning($"[FiberTrunk] gregCore wiring failed: {ex.GetBaseException().Message}");
                    }
                }

                LoggerInstance.Msg(
                    $"[FiberTrunk] {MyPluginInfo.PLUGIN_VERSION} loaded. " +
                    $"Strands={FiberTrunkConfig.StrandCount}, Enabled={FiberTrunkConfig.Enabled}. " +
                    $"Press {FiberTrunkConfig.ToggleKey} for the panel" +
                    (GregHost.HasCore ? " (strands also in F1 Mod Config)." : "."));
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[FiberTrunk] Startup failed: {ex.GetBaseException().Message}");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try { TrunkGhostManager.EnsureExists(); }
            catch { /* best-effort: manager respawns next scene load */ }
        }

        public override void OnUpdate()
        {
            try
            {
                var kb = Keyboard.current;
                if (kb == null) return;
                var key = kb[FiberTrunkConfig.ToggleKey];
                if (key != null && key.wasPressedThisFrame && !IsPauseMenuActive())
                    TrunkGhostManager.TogglePanel();
            }
            catch { /* input best-effort */ }
        }

        /// <summary>True while a game pause/settings canvas is on screen.</summary>
        internal static bool IsPauseMenuActive()
        {
            try
            {
                var all = Resources.FindObjectsOfTypeAll<Canvas>();
                if (all == null) return false;
                foreach (var c in all)
                {
                    if (c == null || !c.isActiveAndEnabled) continue;
                    var go = c.gameObject;
                    if (go == null) continue;
                    if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
                    if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                    var n = go.name ?? "";
                    if (n.IndexOf("Pause", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("EscapeMenu", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("InGameMenu", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("OptionsMenu", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("SettingsMenu", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch { /* best-effort */ }
            return false;
        }
    }
}
