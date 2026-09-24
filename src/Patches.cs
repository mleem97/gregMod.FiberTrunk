using System;
using System.Reflection;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace GregMod.FiberTrunk
{
    /// <summary>
    /// Harmony seams. Every patch is strictly observational except the additive
    /// sibling creation (TrunkCloner), which runs delayed and never during the
    /// original call. No prefix ever suppresses vanilla behaviour
    /// (all prefixes return void / always call through).
    ///
    /// Recursion guard: <see cref="TrunkCloner.IsCloning"/> makes the
    /// RegisterCableConnection postfix ignore cables created by the mod itself.
    /// </summary>
    internal static class Patches
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            int applied = 0;

            applied += TryPatch(harmony, typeof(CableLink), "InteractOnClick",
                prefix: typeof(PatchPortClick).GetMethod(nameof(PatchPortClick.Prefix),
                    BindingFlags.Static | BindingFlags.NonPublic));

            applied += TryPatch(harmony, typeof(CablePositions), "CreateNewCable",
                postfix: typeof(PatchCableCreated).GetMethod(nameof(PatchCableCreated.Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic));

            applied += TryPatch(harmony, typeof(NetworkMap), "RegisterCableConnection",
                postfix: typeof(PatchCableConnected).GetMethod(nameof(PatchCableConnected.Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic));

            applied += TryPatch(harmony, typeof(CablePositions), "DiscardCable",
                postfix: typeof(PatchCableDiscarded).GetMethod(nameof(PatchCableDiscarded.Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic));

            MelonLogger.Msg($"[FiberTrunk] Harmony seams applied: {applied}/4.");
        }

        private static int TryPatch(HarmonyLib.Harmony harmony, Type target, string method,
            MethodInfo prefix = null, MethodInfo postfix = null)
        {
            try
            {
                var m = target.GetMethod(method,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null)
                {
                    MelonLogger.Warning($"[FiberTrunk] Seam missing: {target.Name}.{method} — tracking degraded.");
                    return 0;
                }
                harmony.Patch(m,
                    prefix != null ? new HarmonyLib.HarmonyMethod(prefix) : null,
                    postfix != null ? new HarmonyLib.HarmonyMethod(postfix) : null);
                MelonLogger.Msg($"[FiberTrunk] Seam hooked: {target.Name}.{method}.");
                return 1;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Failed to hook {target.Name}.{method}: {ex.GetBaseException().Message}");
                return 0;
            }
        }
    }

    /// <summary>Records every physical port click (start/end role inferred by carry state).</summary>
    internal static class PatchPortClick
    {
        internal static void Prefix(CableLink __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!FiberTrunkConfig.Enabled) return;

                if (TrunkState.IsTracking)
                    TrunkState.EndLinkCandidate = __instance;
                else
                    TrunkState.StartLinkCandidate = __instance;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Port-click seam failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// A trunk starts at a patch-panel port. Anything else (switches, servers,
    /// …) is vanilla territory and passes through untracked — panel-to-panel
    /// only, like a real fiber trunk between two fiber panels.
    /// </summary>
    internal static class TrunkScope
    {
        internal static bool IsPanelPort(CableLink link, bool requireFibre)
        {
            try
            {
                if (link == null) return false;
                if (link.parentPatchPanel == null) return false;
                if (link.typeOfLink != CableLink.TypeOfLink.PatchPanel) return false;
                if (requireFibre && !link.isFibrePort) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static string DescribePanel(CableLink link)
        {
            try
            {
                if (link == null || link.parentPatchPanel == null) return "unknown panel";
                var panel = link.parentPatchPanel;
                string id = panel.patchPanelId ?? "";
                if (string.IsNullOrEmpty(id)) id = panel.name ?? "panel";
                return $"panel {id}";
            }
            catch { return "unknown panel"; }
        }
    }

    /// <summary>Detects the start of a vanilla cable pull (tracked only from panel ports).</summary>
    internal static class PatchCableCreated
    {
        internal static void Postfix(int __result)
        {
            try
            {
                if (__result < 0) return;
                if (!FiberTrunkConfig.Enabled) return;
                if (TrunkCloner.IsCloning) return; // our own strand ids are not tracked
                int want = FiberTrunkConfig.StrandCount;
                if (want <= 1) return; // vanilla passthrough

                // Panel-to-panel only: ignore pulls started anywhere else.
                if (!TrunkScope.IsPanelPort(TrunkState.StartLinkCandidate, FiberTrunkConfig.RequireFibrePorts))
                    return;

                TrunkState.Begin(__result);
                TrunkState.LastResult = $"Trunk cable {__result} from {TrunkScope.DescribePanel(TrunkState.StartLinkCandidate)} — {want} strands, one thick ghost following you.";
                MelonLogger.Msg($"[FiberTrunk] Tracking trunk pull of cable {__result} (x{want} strands).");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Cable-created seam failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Captures the authoritative connection record when vanilla completes a
    /// cable and enqueues strand creation (delayed, see TrunkGhostManager).
    /// Only panel-to-panel completions become trunks; anything else untracks
    /// silently and stays a normal 1-to-1 cable.
    /// </summary>
    internal static class PatchCableConnected
    {
        internal static void Postfix(NetworkMap __instance,
            int cableId, Vector3 startPos, Vector3 endPos,
            CableLink.TypeOfLink startType, CableLink.TypeOfLink endType,
            string startSwitchID, string endSwitchID,
            int startCustomerID, int endCustomerID,
            string startServerID, string endServerID)
        {
            try
            {
                if (__instance != null)
                    TrunkState.NetworkMapInstance = __instance;
                if (TrunkCloner.IsCloning) return; // ignore our own strands
                if (!FiberTrunkConfig.Enabled) return;
                if (!TrunkState.IsTracking || cableId != TrunkState.TrackedCableId) return;

                int want = FiberTrunkConfig.StrandCount;

                var rec = new ConnectionRecord
                {
                    CableId = cableId,
                    StartPos = startPos,
                    EndPos = endPos,
                    StartType = startType,
                    EndType = endType,
                    StartSwitchID = startSwitchID,
                    EndSwitchID = endSwitchID,
                    StartCustomerID = startCustomerID,
                    EndCustomerID = endCustomerID,
                    StartServerID = startServerID,
                    EndServerID = endServerID,
                    StartLink = TrunkState.StartLinkCandidate,
                    EndLink = TrunkState.EndLinkCandidate,
                    Waypoints = SnapshotWaypoints(cableId),
                };
                TrunkState.LastConnection = rec;
                TrunkState.StopTracking(null);

                // Trunk scope: the far end must be a panel port too.
                if (!TrunkScope.IsPanelPort(rec.EndLink, FiberTrunkConfig.RequireFibrePorts))
                {
                    TrunkState.LastResult = $"Cable {cableId} completed on a non-panel port — stays a normal cable (trunks are panel-to-panel).";
                    return;
                }

                if (want <= 1 || !FiberTrunkConfig.AutoTrunk)
                {
                    TrunkState.LastResult = $"Trunk cable {cableId} completed panel-to-panel (x{want} mode, auto-trunk off).";
                    return;
                }

                TrunkState.Enqueue(new CloneJob
                {
                    Origin = rec,
                    WantedTotal = want,
                    ExecuteAt = Time.time + FiberTrunkConfig.CloneDelaySec,
                });
                TrunkState.LastResult = $"Trunk cable {cableId} completed — {want - 1} strand(s) queued.";
                MelonLogger.Msg($"[FiberTrunk] Trunk cable {cableId} completed, queued {want - 1} strand(s).");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Cable-connected seam failed: {ex.Message}");
            }
        }

        private static Vector3[] SnapshotWaypoints(int cableId)
        {
            try
            {
                var cp = CablePositions.instance;
                if (cp == null) return null;
                var pts = cp.GetCablePositions(cableId);
                if (pts == null || pts.Count == 0)
                    pts = cp.GetRawCablePositions(cableId);
                if (pts == null || pts.Count == 0) return null;
                var arr = new Vector3[pts.Count];
                for (int i = 0; i < pts.Count; i++) arr[i] = pts[i];
                return arr;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Cancels tracking when vanilla discards the carried cable.</summary>
    internal static class PatchCableDiscarded
    {
        internal static void Postfix(int cableId)
        {
            try
            {
                if (TrunkState.IsTracking && cableId == TrunkState.TrackedCableId)
                {
                    TrunkState.StopTracking($"Carry of cable {cableId} discarded — tracking cleared.");
                    MelonLogger.Msg($"[FiberTrunk] Tracked cable {cableId} discarded, tracking cleared.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Cable-discarded seam failed: {ex.Message}");
            }
        }
    }
}
