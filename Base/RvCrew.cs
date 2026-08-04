using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Property;
using RVRepairVan.Config;
using RVRepairVan.Managers;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace RVRepairVan.Base
{
    /// <summary>
    /// Upgrade 3 - "crew quarters". Makes the RV a place employees can actually be stationed.
    ///
    /// Three things are missing in vanilla, all measured (Workspace/docs/RVRepairVan/VANILLA-RV-PROPERTY.md):
    ///
    /// 1. <c>EmployeeCapacity == 0</c>. That is the real blocker - <c>EmployeeManager.CreateEmployee_Server</c>
    ///    refuses at <c>Employees.Count >= EmployeeCapacity</c>, and the transfer dialogue only lists properties
    ///    with capacity above zero. Confirmed live: <c>addemployee botanist rv</c> did nothing.
    /// 2. <c>EmployeeIdlePoints</c> is an empty array, and <c>Employee.AssignProperty</c> indexes it without a
    ///    bounds check. Raising the capacity without filling this throws inside Initialize and leaves a
    ///    half-built NPC standing around - so both happen together, capacity last.
    /// 3. The interior is off the NavMesh, and vanilla never bakes at runtime. Without a mesh every reachability
    ///    check returns null and the employee has nothing it can walk to.
    ///
    /// Deliberately NOT done: patching <c>Employee.CanWork</c>. It has no property logic at all - it wants
    /// <c>GetHome() != null</c>, i.e. a locker placed inside. Forcing it true also makes <c>IsPayAvailable()</c>
    /// permanently false, which is how the competing mod ends up with employees that work for free and keep
    /// complaining they have no locker. The honest unlock is the build grid plus a locker the player places.
    /// </summary>
    internal static class RvCrew
    {
        private const string IdleRootName = "RVRepairVan_IdlePoints";
        private const string NavRootName = "RVRepairVan_NavFloor";

        private static bool _applied;
        private static bool _navBaked;
        private static readonly HashSet<int> _bakedAgents = new HashSet<int>();

        /// <summary>True once the upgrade is owned and applied - read by the Fixer patch below.</summary>
        internal static bool CrewUnlocked { get; private set; }

        internal static void Reset()
        {
            _applied = false;
            _navBaked = false;
            _bakedAgents.Clear();
            CrewUnlocked = false;
        }

        /// <summary>
        /// Set just the capacity and the idle points, as early as the save tells us the crew quarters are paid
        /// for. This runs from RepairSave.OnLoaded because the game's employee loader restores a property's crew
        /// by re-running CreateEmployee_Server, which refuses while <c>Employees.Count >= EmployeeCapacity</c>.
        /// Applying the capacity with the rest of the build-out (a couple of seconds later, once the RV has been
        /// repaired) is too late: the employees have already been skipped.
        ///
        /// The NavMesh bake is deliberately NOT done here - it needs the repaired RV, and the normal apply pass
        /// picks it up shortly afterwards.
        /// </summary>
        internal static void ApplyCapacityEarly()
        {
            try
            {
                if (!RVRepairVanPreferences.UpgradesEnabled) return;
                if (!RvUpgrades.Owned(RvUpgrade.CrewQuarters)) return;

                Transform root = RVManager.Root;
                Property prop = root != null ? root.GetComponent<Property>() : null;
                if (prop == null) return;

                int size = RVRepairVanPreferences.CrewSize;
                BuildIdlePoints(root, prop, size);
                if (prop.EmployeeIdlePoints != null && prop.EmployeeIdlePoints.Length >= size)
                {
                    prop.EmployeeCapacity = size;
                    CrewUnlocked = true;
                    Core.Log.Msg("[Crew] capacity raised early to " + size + " so the saved crew can load.");
                }
            }
            catch (Exception e) { Core.Log.Warning("[Crew] early capacity failed: " + e.Message); }
        }

        internal static void Apply()
        {
            if (_applied) return;
            try
            {
                Transform root = RVManager.Root;
                Property prop = root != null ? root.GetComponent<Property>() : null;
                if (prop == null) { Core.LogDebug("[Crew] RV property not ready - will retry on the next apply."); return; }

                int size = RVRepairVanPreferences.CrewSize;
                BuildIdlePoints(root, prop, size);

                // Capacity LAST: until the idle points exist, a capacity above zero is a crash waiting for the
                // first hire (Employee.AssignProperty indexes EmployeeIdlePoints[EmployeeIndex] unguarded).
                if (prop.EmployeeIdlePoints != null && prop.EmployeeIdlePoints.Length >= size)
                {
                    prop.EmployeeCapacity = size;
                    CrewUnlocked = true;
                }
                else
                {
                    Core.Log.Warning("[Crew] idle points missing - leaving EmployeeCapacity at "
                        + prop.EmployeeCapacity + " so hiring cannot crash.");
                    return;
                }

                BakeNavMesh(root, prop);

                _applied = true;
                Core.Log.Msg("[Crew] quarters ready - capacity=" + prop.EmployeeCapacity
                    + " idlePoints=" + (prop.EmployeeIdlePoints != null ? prop.EmployeeIdlePoints.Length : 0)
                    + " navBaked=" + _navBaked);
            }
            catch (Exception e) { Core.Log.Warning("[Crew] apply failed: " + e.Message); }
        }

        // --- idle points ----------------------------------------------------------------------------------

        /// <summary>
        /// Place the idle spots on the actual cabin floor. Derived from the RV's own build grids rather than from
        /// hand-written offsets: the grids ARE the interior floor (they sit at y = 1.08 spanning the cabin), so
        /// their bounds give a footprint that stays correct whatever the RV's orientation. A first attempt used
        /// local-space offsets and put the crew along the wrong axis, because the RV root's axes are not aligned
        /// with the cabin's long side.
        /// </summary>
        private static void BuildIdlePoints(Transform root, Property prop, int count)
        {
            try
            {
                Transform holder = root.Find(IdleRootName);
                if (holder == null)
                {
                    var go = new GameObject(IdleRootName);
                    go.transform.SetParent(root, false);
                    holder = go.transform;
                }

                for (int i = holder.childCount; i < count; i++)
                {
                    var pt = new GameObject("Idle_" + i);
                    pt.transform.SetParent(holder, false);
                }

                Vector3[] spots = FloorSpots(prop, count);
                for (int i = 0; i < holder.childCount && i < spots.Length; i++)
                {
                    Transform t = holder.GetChild(i);
                    t.position = spots[i];
                    t.rotation = root.rotation;
                }

                int n = Mathf.Min(count, holder.childCount);
                var arr = new Transform[n];
                for (int i = 0; i < n; i++) arr[i] = holder.GetChild(i);
                prop.EmployeeIdlePoints = arr;   // implicit conversion to Il2CppReferenceArray
            }
            catch (Exception e) { Core.Log.Warning("[Crew] idle points failed: " + e.Message); }
        }

        /// <summary>World-space points spread evenly across the cabin floor, taken from the build grids' extent.</summary>
        private static Vector3[] FloorSpots(Property prop, int count)
        {
            var spots = new Vector3[count];
            Bounds? floor = FloorBounds(prop);
            if (floor == null)
            {
                // No grids to measure - fall back to the RV origin so the array is at least filled and hiring
                // cannot throw. Employees will idle in one spot, which is ugly but not broken.
                Vector3 at = RVManager.Root != null ? RVManager.Root.position : Vector3.zero;
                for (int i = 0; i < count; i++) spots[i] = at;
                return spots;
            }

            Bounds b = floor.Value;
            // Spread along the longer horizontal axis, inset so nobody stands in a wall.
            bool alongX = b.size.x >= b.size.z;
            float length = (alongX ? b.size.x : b.size.z) - 1.0f;
            if (length < 0.5f) length = 0.5f;
            float start = -length * 0.5f;
            float step = count > 1 ? length / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float d = start + step * i;
                spots[i] = new Vector3(
                    b.center.x + (alongX ? d : 0f),
                    b.center.y,
                    b.center.z + (alongX ? 0f : d));
            }
            return spots;
        }

        /// <summary>Bounds of the RV's build grids = the walkable cabin floor. Null when there are no grids.</summary>
        private static Bounds? FloorBounds(Property prop)
        {
            try
            {
                var grids = prop.Grids;
                if (grids == null || grids.Count == 0) return null;
                bool any = false;
                Bounds b = default;
                for (int i = 0; i < grids.Count; i++)
                {
                    Il2CppScheduleOne.Tiles.Grid g = grids[i];
                    if (g == null) continue;
                    if (!any) { b = new Bounds(g.transform.position, Vector3.zero); any = true; }
                    else b.Encapsulate(g.transform.position);
                }
                if (!any) return null;
                b.Expand(new Vector3(1.5f, 0f, 1.5f));   // a grid's transform is its corner-ish anchor, not its extent
                return b;
            }
            catch { return null; }
        }

        // --- navmesh --------------------------------------------------------------------------------------

        /// <summary>
        /// Bake a small surface over the cabin floor plus a ramp down to the door, for every agent type the RV's
        /// employees actually use. Idempotent: the surface object is reused, and the "baked" flag is only set
        /// after a bake really succeeded, so a failure can be retried instead of being locked out.
        /// </summary>
        private static void BakeNavMesh(Transform root, Property prop)
        {
            try
            {
                if (_navBaked) return;

                var agents = new HashSet<int>();
                try { agents.Add(Il2CppScheduleOne.DevUtilities.NavMeshUtility.GetNavMeshAgentID("Humanoid")); } catch { agents.Add(0); }
                if (prop.Employees != null)
                    for (int i = 0; i < prop.Employees.Count; i++)
                    {
                        var emp = prop.Employees[i];
                        if (emp == null) continue;
                        var agent = emp.GetComponent<NavMeshAgent>();
                        if (agent != null) agents.Add(agent.agentTypeID);
                    }

                bool any = false;
                foreach (int id in agents)
                {
                    if (_bakedAgents.Contains(id)) { any = true; continue; }
                    if (BakeForAgent(root, prop, id)) { _bakedAgents.Add(id); any = true; }
                }

                _navBaked = any;
                if (!any) Core.Log.Warning("[Crew] NavMesh bake produced nothing - employees may not path inside.");
            }
            catch (Exception e) { Core.Log.Warning("[Crew] navmesh failed: " + e.Message); }
        }

        /// <summary>
        /// Bake one agent type over an invisible floor slab plus a ramp to the ground.
        ///
        /// The colliders must be SOLID: with <c>useGeometry = PhysicsColliders</c> the bake ignores triggers, so a
        /// first attempt using <c>isTrigger = true</c> reported success and produced no walkable surface at all.
        /// They are destroyed the moment the bake is done, which keeps them out of the player's way without
        /// touching the global collision matrix (the competing mod instead sets layer 31 to ignore every other
        /// layer and never restores it).
        /// </summary>
        private static bool BakeForAgent(Transform root, Property prop, int agentTypeID)
        {
            GameObject holder = null;
            try
            {
                string name = NavRootName + "_" + agentTypeID;
                Transform existing = root.Find(name);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);   // rebuild cleanly

                holder = new GameObject(name);
                holder.transform.SetParent(root, false);
                holder.transform.position = root.position;
                holder.transform.rotation = root.rotation;

                Bounds? floor = FloorBounds(prop);
                if (floor == null) { Core.Log.Warning("[Crew] no floor bounds - NavMesh bake skipped."); return false; }
                Bounds b = floor.Value;

                // The cabin floor, sized from the grids, and a ramp from its edge down to the ground so the baked
                // island connects to the world mesh instead of floating unreachable above it.
                AddSlab(holder.transform, "Floor", new Vector3(b.center.x, b.center.y, b.center.z),
                    root.rotation, new Vector3(Mathf.Max(b.size.x, 1f) + 1f, 0.2f, Mathf.Max(b.size.z, 1f) + 1f));

                bool alongX = b.size.x >= b.size.z;
                float half = (alongX ? b.size.x : b.size.z) * 0.5f + 1.2f;
                Vector3 rampCenter = new Vector3(
                    b.center.x - (alongX ? half : 0f), b.center.y * 0.5f, b.center.z - (alongX ? 0f : half));
                AddSlab(holder.transform, "Ramp", rampCenter, root.rotation * Quaternion.Euler(0f, 0f, alongX ? -35f : 0f),
                    new Vector3(3.5f, 0.2f, 3.5f));

                var surface = holder.GetComponent<NavMeshSurface>();
                if (surface == null)
                {
                    holder.AddComponent(Il2CppInterop.Runtime.Il2CppType.From(typeof(NavMeshSurface)));
                    surface = holder.GetComponent<NavMeshSurface>();
                }
                if (surface == null) return false;

                try { surface.collectObjects = CollectObjects.Children; } catch { }
                try { surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; } catch { }
                try { surface.agentTypeID = agentTypeID; surface.defaultArea = 0; } catch { }
                surface.BuildNavMesh();

                DestroySlabColliders(holder.transform);
                Core.LogDebug("[Crew] baked NavMesh for agent " + agentTypeID + ".");
                return true;
            }
            catch (Exception e)
            {
                if (holder != null) { try { DestroySlabColliders(holder.transform); } catch { } }
                Core.LogDebug("[Crew] bake for agent " + agentTypeID + " failed: " + e.Message);
                return false;
            }
        }

        private static void AddSlab(Transform parent, string name, Vector3 worldPos, Quaternion worldRot, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.center = Vector3.zero;
            // Solid on purpose - the bake ignores triggers. Removed again in DestroySlabColliders once baked.
        }

        private static void DestroySlabColliders(Transform holder)
        {
            var boxes = holder.GetComponentsInChildren<BoxCollider>(true);
            if (boxes == null) return;
            foreach (BoxCollider c in boxes)
                if (c != null) UnityEngine.Object.Destroy(c);
        }
    }

    /// <summary>
    /// The Fixer refuses to staff the RV: <c>ModifyChoiceList</c> hard-excludes the "rv" property code. Once the
    /// crew quarters are paid for, put it back into the list - the rest of the hire flow already handles any
    /// owned property generically, so nothing else needs touching.
    /// </summary>
    [HarmonyPatch(typeof(DialogueController_Fixer), nameof(DialogueController_Fixer.ModifyChoiceList))]
    internal static class Rv_Fixer_ModifyChoiceList_Patch
    {
        // Parameter names must match the target EXACTLY - Harmony injects by name, and getting this wrong is not a
        // silent no-op here but a hard patch failure at startup. The real signature is
        // ModifyChoiceList(string dialogueLabel, ref List<DialogueChoiceData> existingChoices).
        private static void Postfix(string dialogueLabel, ref Il2CppSystem.Collections.Generic.List<DialogueChoiceData> existingChoices)
        {
            try
            {
                if (!RvCrew.CrewUnlocked || existingChoices == null) return;
                if (dialogueLabel != "SELECT_LOCATION") return;

                for (int i = 0; i < existingChoices.Count; i++)
                    if (existingChoices[i] != null && existingChoices[i].ChoiceLabel == "rv") return;   // already listed

                PropertyManager pm = Singleton<PropertyManager>.Instance;
                Property rv = pm != null ? pm.GetProperty("rv") : null;
                if (rv == null || !rv.IsOwned || rv.EmployeeCapacity <= 0) return;

                var entry = new DialogueChoiceData { ChoiceLabel = "rv", ChoiceText = rv.PropertyName };
                existingChoices.Add(entry);
            }
            catch (Exception e) { Core.LogDebug("[Crew] fixer patch: " + e.Message); }
        }
    }
}
