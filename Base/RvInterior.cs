using System;
using System.Collections.Generic;
using RVRepairVan.Managers;

namespace RVRepairVan.Base
{
    /// <summary>
    /// Upgrade 1 - "gut the interior". Clears the RV's fixed furniture so there is floor to build on.
    ///
    /// The object list is exact, taken from a live measurement (see
    /// Workspace/docs/RVRepairVan/VANILLA-RV-PROPERTY.md): six children under
    /// <c>RV/rv/Main/Interior</c> plus the partition <c>RV/rv/Main/Wall.001</c>. A curated list on purpose -
    /// name heuristics over the whole hierarchy are how the competing mod ends up hiding things it meant to keep.
    ///
    /// The original active state is snapshotted before anything is switched off, so a revert cannot turn on an
    /// object the game had deliberately disabled.
    /// </summary>
    internal static class RvInterior
    {
        private static readonly string[] InteriorChildren =
        {
            "Bed", "Bench", "Cabinets", "Chair", "Chair (1)", "Dash",
        };

        private const string WallPath = "rv/Main/Wall.001";
        private const string InteriorPath = "rv/Main/Interior";

        // name -> was it active before we touched it
        private static readonly Dictionary<string, bool> _original = new Dictionary<string, bool>();
        private static bool _applied;

        internal static void Reset() { _original.Clear(); _applied = false; }

        internal static void Apply()
        {
            if (_applied) return;
            try
            {
                Transform model = ModelRoot();
                if (model == null) { Core.LogDebug("[Interior] RV model not found yet - will retry on the next apply."); return; }

                int hidden = 0;
                Transform interior = model.Find(InteriorPath);
                if (interior != null)
                {
                    foreach (string name in InteriorChildren)
                    {
                        Transform t = interior.Find(name);
                        if (t == null) continue;
                        Remember(name, t.gameObject.activeSelf);
                        if (t.gameObject.activeSelf) { t.gameObject.SetActive(false); hidden++; }
                    }
                }
                else Core.Log.Warning("[Interior] '" + InteriorPath + "' not found under the RV model.");

                Transform wall = model.Find(WallPath);
                if (wall != null)
                {
                    Remember("Wall.001", wall.gameObject.activeSelf);
                    if (wall.gameObject.activeSelf) { wall.gameObject.SetActive(false); hidden++; }
                }

                _applied = true;
                Core.Log.Msg("[Interior] gutted - " + hidden + " object(s) hidden.");
            }
            catch (Exception e) { Core.Log.Warning("[Interior] apply failed: " + e.Message); }
        }

        /// <summary>Put the interior back the way it was found. Only used by the debug revoke path.</summary>
        internal static void Revert()
        {
            try
            {
                Transform model = ModelRoot();
                if (model == null) return;
                Transform interior = model.Find(InteriorPath);
                foreach (string name in InteriorChildren)
                {
                    Transform t = interior != null ? interior.Find(name) : null;
                    if (t != null && _original.TryGetValue(name, out bool was)) t.gameObject.SetActive(was);
                }
                Transform wall = model.Find(WallPath);
                if (wall != null && _original.TryGetValue("Wall.001", out bool wallWas)) wall.gameObject.SetActive(wallWas);
                _applied = false;
                Core.Log.Msg("[Interior] restored to its original state.");
            }
            catch (Exception e) { Core.Log.Warning("[Interior] revert failed: " + e.Message); }
        }

        private static void Remember(string key, bool active)
        {
            if (!_original.ContainsKey(key)) _original[key] = active;
        }

        // The furniture hangs off the INTACT model ("RV"), not the wreck - so this only resolves once repaired.
        private static Transform ModelRoot()
        {
            Transform root = RVManager.Root;
            return root != null ? root.Find("RV") : null;
        }
    }
}
