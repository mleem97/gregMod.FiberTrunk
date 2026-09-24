using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;

namespace GregMod.FiberTrunk
{
    /// <summary>
    /// Creates trunk strands for a completed panel-to-panel pull by replaying
    /// the same vanilla entry points a physical port click would drive:
    ///
    ///   CablePositions.ReserveCableId()
    ///     -> CablePositions.AssignNewPosition(id, startLink, isStart, ...)
    ///     -> CablePositions.AssignNewPosition(id, endLink, ..., isEnd, ...)
    ///     -> CablePositions.GenerateFinalPath(id) if not complete
    ///   WaypointInitializationSystem.RequestRouteEvaluation()
    ///
    /// Each strand lands on free ports of the SAME start/end fiber panels, so
    /// the vanilla-generated paths run parallel to the original pull — one
    /// walked route, a full trunk of discrete fiber strands.
    ///
    /// Safety:
    ///  - IsCloning guards the RegisterCableConnection/CreateNewCable patches
    ///    against recursion (our strands never enqueue grandchildren).
    ///  - Every step is individually try/caught; a failed strand is discarded
    ///    via DiscardCable and reported, never left half-registered.
    ///  - Partial success (fewer free ports than wanted, e.g. a 24fo trunk on
    ///    24-port panels with some ports taken) is reported, not fatal.
    /// </summary>
    internal static class TrunkCloner
    {
        internal static bool IsCloning;

        internal static void Execute(CloneJob job)
        {
            if (job == null || job.Origin == null) return;
            var rec = job.Origin;
            int siblingsWanted = Math.Max(0, job.WantedTotal - 1);
            if (siblingsWanted <= 0) return;

            var cp = CablePositions.instance;
            if (cp == null)
            {
                TrunkState.LastResult = "Trunk failed: CablePositions not ready.";
                MelonLogger.Warning("[FiberTrunk] Trunk aborted: CablePositions.instance is null.");
                return;
            }

            if (rec.StartLink == null || rec.EndLink == null)
            {
                TrunkState.LastResult = "Trunk skipped: start/end port click was not observed (free-port search needs it).";
                MelonLogger.Warning("[FiberTrunk] Trunk skipped: StartLink/EndLink unknown — click physical panel ports for full tracking.");
                return;
            }

            var claimed = new HashSet<IntPtr>();
            var startSibs = PanelPortFinder.FindFreeSiblings(rec.StartLink, siblingsWanted, claimed);
            foreach (var s in startSibs) claimed.Add(s.Pointer);
            var endSibs = PanelPortFinder.FindFreeSiblings(rec.EndLink, siblingsWanted, claimed);
            foreach (var s in endSibs) claimed.Add(s.Pointer);

            int pairs = Math.Min(startSibs.Count, endSibs.Count);
            if (pairs == 0)
            {
                TrunkState.LastResult =
                    $"Trunk skipped: no free panel port pairs (start: {PanelPortFinder.Describe(rec.StartLink)}, end: {PanelPortFinder.Describe(rec.EndLink)}).";
                MelonLogger.Warning("[FiberTrunk] Trunk skipped: no free panel port pair found.");
                return;
            }
            if (pairs < siblingsWanted)
                MelonLogger.Warning($"[FiberTrunk] Only {pairs}/{siblingsWanted} strand pair(s) available — creating what fits.");

            int created = 0;
            var createdIds = new List<int>();
            IsCloning = true;
            try
            {
                for (int i = 0; i < pairs; i++)
                {
                    int newId = -1;
                    try
                    {
                        newId = cp.ReserveCableId();
                        var sl = startSibs[i];
                        var el = endSibs[i];

                        cp.AssignNewPosition(newId, sl.transform, true, false, rec.StartType, rec.StartServerID ?? "");
                        cp.AssignNewPosition(newId, el.transform, false, true, rec.EndType, rec.EndServerID ?? "");

                        if (!cp.IsCableComplete(newId))
                        {
                            try { cp.GenerateFinalPath(newId); }
                            catch (Exception ex)
                            {
                                MelonLogger.Warning($"[FiberTrunk] GenerateFinalPath({newId}) failed: {ex.Message}");
                            }
                        }

                        if (cp.IsCableComplete(newId))
                        {
                            created++;
                            createdIds.Add(newId);
                            MelonLogger.Msg($"[FiberTrunk] Trunk strand {newId} created " +
                                $"({PanelPortFinder.Describe(sl)} <-> {PanelPortFinder.Describe(el)}).");
                        }
                        else
                        {
                            try { cp.DiscardCable(newId); } catch { /* best-effort */ }
                            MelonLogger.Warning($"[FiberTrunk] Trunk strand {newId} incomplete after replay — discarded.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[FiberTrunk] Strand creation failed: {ex.GetBaseException().Message}");
                        if (newId >= 0)
                        {
                            try { cp.DiscardCable(newId); } catch { /* best-effort */ }
                        }
                    }
                }
            }
            finally
            {
                IsCloning = false;
            }

            try
            {
                var wis = WaypointInitializationSystem.Instance;
                if (wis != null) wis.RequestRouteEvaluation();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Route re-evaluation failed: {ex.Message}");
            }

            if (created > 0)
            {
                string note = pairs < siblingsWanted
                    ? $" (only {pairs}/{siblingsWanted} free port pairs — panels may be partially occupied)"
                    : "";
                TrunkState.LastResult = $"Trunk x{job.WantedTotal}: cable {rec.CableId} + {created} strand(s) [{string.Join(",", createdIds)}]{note}.";
                MelonLogger.Msg($"[FiberTrunk] Trunk complete: origin {rec.CableId} + {created} strand(s).");
            }
            else
            {
                TrunkState.LastResult = "Trunk failed: strand replay did not complete — see console. Origin cable is unaffected.";
            }
        }
    }
}
