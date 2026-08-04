using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Property;
using RVRepairVan.Managers;

namespace RVRepairVan.Base
{
    /// <summary>
    /// Upgrade 4 - "loading dock". Lets suppliers deliver to the RV.
    ///
    /// Two independent gates, both of which have to open:
    ///
    /// 1. <c>RV.CanDeliverToProperty()</c> is overridden to false. That is the real filter - the destination list
    ///    is <c>OwnedProperties.Where(x => x.CanDeliverToProperty())</c> in DeliveryShop. (The competing mod
    ///    instead scans a whole assembly for anything named GetPotentialDestinations, which is both the wrong
    ///    hook and a process-killer on IL2CPP.)
    /// 2. <c>LoadingDockCount == 0</c> blocks the order with "Selected destination has no loading docks", so an
    ///    actual dock has to exist. A LoadingDock needs a ParkingLot, a VehicleDetector and access points, which
    ///    is far more than can sensibly be built by hand - so clone one of the Manor's.
    /// </summary>
    internal static class RvDeliveryDock
    {
        private const string DockName = "RVRepairVan_LoadingDock";

        // Where the delivery van should park, relative to the RV.
        private static readonly Vector3 DockLocal = new Vector3(1.0f, 0.0f, 11.5f);
        private static readonly Quaternion DockRot = Quaternion.Euler(0f, 180f, 0f);

        private static readonly string[] TemplatePaths =
        {
            "@Properties/Manor/Loading Docks/Loading Dock",
            "@Properties/Manor/Loading Docks/Loading Dock (1)",
            "@Properties/Manor/Loading Docks/Loading Dock (2)",
        };

        /// <summary>True once the dock is attached - read by the CanDeliverToProperty patch below.</summary>
        internal static bool DockReady { get; private set; }

        private static bool _applied;

        internal static void Reset() { _applied = false; DockReady = false; }

        internal static void Apply()
        {
            if (_applied) return;
            try
            {
                Transform root = RVManager.Root;
                Property prop = root != null ? root.GetComponent<Property>() : null;
                if (prop == null) { Core.LogDebug("[Dock] RV property not ready - will retry on the next apply."); return; }

                // Already attached (e.g. a second apply in the same session)?
                if (HasOurDock(prop)) { _applied = true; DockReady = true; return; }

                LoadingDock template = FindTemplate();
                if (template == null)
                {
                    // Deliberately do NOT set _applied here: without the template this is retryable, and the
                    // competing mod's bug is exactly that it latches the "created" flag before it can fail.
                    Core.Log.Warning("[Dock] no LoadingDock template found - retrying on the next apply.");
                    return;
                }

                LoadingDock dock = UnityEngine.Object.Instantiate(template, root.TransformPoint(DockLocal), root.rotation * DockRot);
                if (dock == null) { Core.Log.Warning("[Dock] instantiate failed - retrying on the next apply."); return; }

                dock.name = DockName;
                dock.ParentProperty = prop;
                dock.transform.SetParent(root, true);

                var list = new List<LoadingDock>();
                if (prop.LoadingDocks != null)
                    foreach (LoadingDock d in prop.LoadingDocks)
                        if (d != null) list.Add(d);
                list.Add(dock);
                prop.LoadingDocks = list.ToArray();   // implicit conversion to Il2CppReferenceArray

                _applied = true;
                DockReady = true;
                Core.Log.Msg("[Dock] loading dock attached (" + list.Count + " total on the RV).");
            }
            catch (Exception e) { Core.Log.Warning("[Dock] apply failed: " + e.Message); }
        }

        private static bool HasOurDock(Property prop)
        {
            try
            {
                if (prop.LoadingDocks == null) return false;
                foreach (LoadingDock d in prop.LoadingDocks)
                    if (d != null && d.name == DockName) return true;
            }
            catch { }
            return false;
        }

        private static LoadingDock FindTemplate()
        {
            foreach (string path in TemplatePaths)
            {
                try
                {
                    GameObject go = GameObject.Find(path);
                    LoadingDock d = go != null ? go.GetComponent<LoadingDock>() : null;
                    if (d != null) return d;
                }
                catch { }
            }
            // Fall back to any dock in the scene: the Manor paths are the likely ones, but a renamed hierarchy
            // should degrade to "some working dock" rather than to no delivery at all.
            try
            {
                var all = UnityEngine.Object.FindObjectsOfType<LoadingDock>();
                if (all != null)
                    foreach (LoadingDock d in all)
                        if (d != null && d.name != DockName) return d;
            }
            catch { }
            return null;
        }
    }

    /// <summary>
    /// Put the RV into the delivery destination list.
    ///
    /// The obvious hook is <c>RV.CanDeliverToProperty()</c>, which returns false - but that override has no managed
    /// body in IL2CPP, it is a generated interop stub. Harmony rebuilds it into a dynamic method that then throws
    /// a NullReferenceException on EVERY call, and the game calls it constantly: a first attempt flooded the log
    /// with thousands of "During invoking native->managed trampoline" errors and broke loading a save.
    ///
    /// <c>DeliveryShop.GetPotentialDestinations()</c> is a real method with a real body and is the single place
    /// the list is built, so append to its result instead. Targeted at this one method on this one type - not the
    /// competing mod's assembly-wide name scan, which also runs <c>Assembly.GetTypes()</c> over the interop
    /// assembly and kills the process.
    /// </summary>
    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Phone.Delivery.DeliveryShop), "GetPotentialDestinations")]
    internal static class Rv_GetPotentialDestinations_Patch
    {
        private static void Postfix(Il2CppSystem.Collections.Generic.List<Property> __result)
        {
            try
            {
                if (!RvDeliveryDock.DockReady || __result == null) return;

                Transform root = RVManager.Root;
                Property rv = root != null ? root.GetComponent<Property>() : null;
                if (rv == null || !rv.IsOwned || rv.LoadingDockCount <= 0) return;

                for (int i = 0; i < __result.Count; i++)
                    if (__result[i] != null && __result[i].PropertyCode == rv.PropertyCode) return;   // already there

                __result.Add(rv);
            }
            catch (Exception e) { Core.LogDebug("[Dock] destination patch: " + e.Message); }
        }
    }
}
