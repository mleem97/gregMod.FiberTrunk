using System;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;

namespace GregMod.FiberTrunk
{
    /// <summary>
    /// In-game runtime: trunk ghost, delayed strand pump, F4 panel.
    ///
    /// Trunk ghost (the "one fat cable follows you" visual): while a vanilla
    /// cable is being carried panel-to-panel, the mod copies the live waypoint
    /// list every frame and renders ONE thick preview line right on the path
    /// (width = cable width x TrunkWidthFactor, slight lift to avoid z-fighting
    /// the vanilla ghost). One pull, one physical trunk.
    ///
    /// Strand pump: executes queued <see cref="CloneJob"/>s once their
    /// ExecuteAt time passes, so vanilla route evaluation can settle first.
    /// </summary>
    public class TrunkGhostManager : MonoBehaviour
    {
        private static bool _panelVisible;
        private static Rect _panelRect = new Rect(20, 20, 440, 120);

        private GameObject _ghost;
        private LineRenderer _line;

        internal static void EnsureExists()
        {
            try
            {
                if (FindObjectOfType<TrunkGhostManager>() == null)
                {
                    var go = new GameObject("FiberTrunkManager");
                    go.AddComponent<TrunkGhostManager>();
                    DontDestroyOnLoad(go);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Manager spawn failed: {ex.Message}");
            }
        }

        internal static void TogglePanel() => _panelVisible = !_panelVisible;
        internal static void OpenPanel() => _panelVisible = true;
        internal static void ClosePanel() => _panelVisible = false;
        internal static bool IsPanelVisible => _panelVisible;

        public TrunkGhostManager(IntPtr ptr) : base(ptr) { }

        public void Update()
        {
            try { PumpStrandQueue(); } catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Strand pump failed: {ex.Message}");
            }
            try { UpdateTrunkGhost(); } catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Trunk ghost update failed: {ex.Message}");
            }
        }

        public void OnGUI()
        {
            if (!_panelVisible) return;
            try
            {
                // NOTE: GUILayout.Window needs an Il2Cpp-crossing delegate
                // (GUI.WindowFunction) which is unreliable under IL2CPP
                // interop, so the panel is a fixed BeginArea instead.
                GUILayout.BeginArea(_panelRect, "FiberTrunk — panel-to-panel fiber trunks", GUI.skin.window);
                DrawPanel();
                GUILayout.EndArea();
            }
            catch (Exception ex)
            {
                try { GUILayout.EndArea(); } catch { /* best-effort */ }
                MelonLogger.Warning($"[FiberTrunk] Panel failed: {ex.Message}");
                _panelVisible = false;
            }
        }

        public void OnDestroy()
        {
            ClearGhost();
        }

        // ── Strand pump ──────────────────────────────────────────────────────

        private static void PumpStrandQueue()
        {
            if (TrunkState.PendingJobs.Count == 0) return;
            if (TrunkCloner.IsCloning) return;
            var job = TrunkState.PendingJobs.Peek();
            if (job == null)
            {
                TrunkState.PendingJobs.Dequeue();
                return;
            }
            if (Time.time < job.ExecuteAt) return;
            TrunkState.PendingJobs.Dequeue();
            TrunkCloner.Execute(job);
        }

        // ── Trunk ghost ──────────────────────────────────────────────────────

        private void UpdateTrunkGhost()
        {
            bool want = FiberTrunkConfig.Enabled
                && TrunkState.IsTracking
                && FiberTrunkConfig.StrandCount > 1;

            if (!want)
            {
                if (_ghost != null) ClearGhost();
                return;
            }

            Vector3[] pts = ReadLivePath(TrunkState.TrackedCableId);
            if (pts == null || pts.Length < 2)
            {
                if (_ghost != null) ClearGhost();
                return;
            }

            EnsureGhost();
            if (_line == null) return;

            Color col = ParseColor(FiberTrunkConfig.TrunkColorRaw, new Color(1f, 0.65f, 0f, 1f));
            float width = ResolveTrunkWidth();

            _line.positionCount = pts.Length;
            for (int i = 0; i < pts.Length; i++)
                _line.SetPosition(i, pts[i] + Vector3.up * 0.012f);
            _line.startWidth = width;
            _line.endWidth = width;
            _line.startColor = col;
            _line.endColor = col;
            try { if (_line.material != null) _line.material.color = col; } catch { /* best-effort */ }
        }

        private static Vector3[] ReadLivePath(int cableId)
        {
            try
            {
                var cp = CablePositions.instance;
                if (cp == null) return null;
                var raw = cp.GetCablePositions(cableId);
                if (raw == null || raw.Count == 0)
                    raw = cp.GetRawCablePositions(cableId);
                if (raw == null || raw.Count < 2) return null;
                var arr = new Vector3[raw.Count];
                for (int i = 0; i < raw.Count; i++) arr[i] = raw[i];
                return arr;
            }
            catch
            {
                return null;
            }
        }

        private void EnsureGhost()
        {
            if (_ghost != null && _line != null) return;
            ClearGhost();
            _ghost = new GameObject("FiberTrunk_Trunk");
            DontDestroyOnLoad(_ghost);
            _line = _ghost.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            try
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                _line.material = mat;
            }
            catch { /* renderer still draws with default material */ }
        }

        private void ClearGhost()
        {
            try { if (_ghost != null) Destroy(_ghost); } catch { /* best-effort */ }
            _ghost = null;
            _line = null;
        }

        private static float ResolveTrunkWidth()
        {
            try
            {
                var cp = CablePositions.instance;
                if (cp != null && cp.cableWidth > 0f)
                    return cp.cableWidth * FiberTrunkConfig.TrunkWidthFactor;
            }
            catch { /* fall through */ }
            return 0.05f * FiberTrunkConfig.TrunkWidthFactor;
        }

        private static Color ParseColor(string raw, Color fallback)
        {
            try
            {
                string hex = (raw ?? "").Trim();
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            }
            catch { /* fall through */ }
            return fallback;
        }

        // ── F4 panel ─────────────────────────────────────────────────────────

        private static void DrawPanel()
        {
            GUILayout.BeginVertical();

            bool f1 = FiberTrunkConfig.F1OwnsCoreSettings;
            GUILayout.Label(f1
                ? "Settings source: F1 gregCore config (panel is read-only)."
                : "Settings source: local preferences (no gregCore).",
                GUILayout.MaxWidth(410));

            bool enabled = FiberTrunkConfig.Enabled;
            int strands = FiberTrunkConfig.StrandCount;
            bool fibreOnly = FiberTrunkConfig.RequireFibrePorts;
            bool auto = FiberTrunkConfig.AutoTrunk;

            if (f1)
            {
                GUILayout.Label($"Enabled: {(enabled ? "ON" : "OFF")}");
                GUILayout.Label($"Strands per trunk: {strands}");
                GUILayout.Label($"Require fibre ports: {(fibreOnly ? "ON" : "OFF")}");
                GUILayout.Label($"Auto-trunk: {(auto ? "ON" : "OFF")}");
                GUILayout.Label("Change these in F1 -> Mod Config -> gregMod.FiberTrunk.");
            }
            else
            {
                bool newEnabled = GUILayout.Toggle(enabled, "Enabled (master switch)");
                if (newEnabled != enabled) FiberTrunkConfig.SetEnabled(newEnabled);

                GUILayout.Label($"Strands per trunk: {strands}  (presets 12/24/48, 1 = vanilla)");
                GUILayout.BeginHorizontal();
                foreach (int preset in FiberTrunkConfig.StrandPresets)
                {
                    if (GUILayout.Button(preset.ToString()))
                        FiberTrunkConfig.SetStrandCount(preset);
                }
                GUILayout.EndHorizontal();
                float slider = GUILayout.HorizontalSlider(strands, FiberTrunkConfig.MinStrands, FiberTrunkConfig.MaxStrands);
                int newStrands = (int)Math.Round(slider);
                if (newStrands != strands) FiberTrunkConfig.SetStrandCount(newStrands);

                bool newFibre = GUILayout.Toggle(fibreOnly, "Require fibre ports");
                if (newFibre != fibreOnly) FiberTrunkConfig.SetRequireFibrePorts(newFibre);

                bool newAuto = GUILayout.Toggle(auto, "Auto-create trunk strands on completion");
                if (newAuto != auto) FiberTrunkConfig.SetAutoTrunk(newAuto);
            }

            GUILayout.Space(6);
            GUILayout.Label("Status:", GUILayout.MaxWidth(410));
            string status = TrunkState.IsTracking
                ? $"Carrying trunk cable {TrunkState.TrackedCableId} — {strands} strands, one thick ghost following you."
                : "Idle (start a pull from a patch-panel port).";
            if (TrunkState.PendingJobs.Count > 0)
                status += $" {TrunkState.PendingJobs.Count} strand job(s) queued.";
            GUILayout.Label(status, GUILayout.MaxWidth(410));
            GUILayout.Label($"Last: {TrunkState.LastResult}", GUILayout.MaxWidth(410));

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop tracking (keeps cable)"))
            {
                TrunkState.StopTracking("Tracking stopped by user — vanilla cable untouched.");
                MelonLogger.Msg("[FiberTrunk] Tracking stopped by user.");
            }
            if (GUILayout.Button("Close"))
                _panelVisible = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("Workflow: set strands -> click a port on fiber panel A -> walk & manage " +
                "the trunk once -> click a port on fiber panel B. Strands land on free panel ports; " +
                "non-panel clicks stay normal 1-to-1 cables.", GUILayout.MaxWidth(410));

            GUILayout.EndVertical();
        }

        internal static void RegisterIl2CppType()
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<TrunkGhostManager>();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[FiberTrunk] Il2Cpp type registration failed: {ex.Message}");
            }
        }
    }
}
