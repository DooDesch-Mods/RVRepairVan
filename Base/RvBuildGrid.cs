using System;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Tiles;
using RVRepairVan.Config;
using RVRepairVan.Managers;

namespace RVRepairVan.Base
{
    /// <summary>
    /// Upgrade 2 - "workshop floor". Extends the RV's build grid.
    ///
    /// Measurement decided the shape of this one, twice over. The RV ships two grids on the interior floor
    /// (<c>@Properties/RV/RV/Container/Grid</c> with 18 tiles and <c>Grid (1)</c> with 14). They read as inactive
    /// on a wrecked RV, but that is only because they hang off the intact model - repairing the RV switches them
    /// on by itself. So the vanilla 32 tiles come FREE with the repair, and selling "turn the grid on" would be
    /// selling nothing.
    ///
    /// What the upgrade actually buys is more floor: <c>BuildGridExtraTiles</c> cloned tiles registered into the
    /// grid on top of the vanilla ones. Activation is still done here, harmlessly and idempotently, so the
    /// upgrade also covers any case where the grids are not already on.
    /// </summary>
    internal static class RvBuildGrid
    {
        private static bool _applied;
        private static int _clonedTiles;

        internal static void Reset() { _applied = false; _clonedTiles = 0; }

        /// <summary>
        /// Rebuild the added tiles while the property is loading, before the game restores the objects placed on
        /// them. The extra tiles only exist at runtime: if a saved machine sits on one and the tile is not back
        /// yet, <c>Grid.GetTile</c> returns null and <c>GridItem</c> destroys itself - the next save then loses
        /// the machine permanently. Called from the PropertyManager.LoadProperty postfix, which returns before
        /// PropertyLoader starts loading object records.
        /// </summary>
        internal static void ProvisionForLoad(Property prop)
        {
            try
            {
                if (prop == null || !RVRepairVanPreferences.UpgradesEnabled) return;
                if (!RvUpgrades.Owned(RvUpgrade.WorkshopFloor)) return;
                ApplyTo(prop);
            }
            catch (Exception e) { Core.Log.Warning("[Grid] load provisioning failed: " + e.Message); }
        }

        internal static void Apply()
        {
            if (_applied) return;
            Property prop = PropertyOf();
            if (prop == null) { Core.LogDebug("[Grid] RV property not ready - will retry on the next apply."); return; }
            ApplyTo(prop);
        }

        private static void ApplyTo(Property prop)
        {
            try
            {
                int enabled = 0, tiles = 0;
                var grids = prop.Grids;
                if (grids != null)
                    for (int i = 0; i < grids.Count; i++)
                    {
                        Grid g = grids[i];
                        if (g == null) continue;
                        if (!g.gameObject.activeSelf) { g.gameObject.SetActive(true); enabled++; }
                        tiles += g.Tiles != null ? g.Tiles.Count : 0;
                    }

                if (enabled == 0 && tiles == 0)
                {
                    Core.Log.Warning("[Grid] the RV reports no build grid - equipment cannot be placed inside.");
                    return;   // leave _applied false so the next pass retries
                }

                int extra = RVRepairVanPreferences.BuildGridExtraTiles;
                if (extra > 0) ExpandGrid(prop, extra);

                // Latch only once the work is actually done, so a failed pass is retried rather than swallowed.
                _applied = true;
                Core.Log.Msg("[Grid] workshop floor open - " + enabled + " grid(s) switched on, " + tiles
                    + " tile(s) total, " + _clonedTiles + " of them added by this upgrade.");
            }
            catch (Exception e) { Core.Log.Warning("[Grid] apply failed: " + e.Message); }
        }

        /// <summary>
        /// Optional extension: clone an existing tile outward in rings around the grid's own tiles. A cloned tile
        /// only counts as real once it is in all four of the grid's books - <c>_coordinateToTile</c> (what GetTile
        /// reads), <c>Tiles</c>, <c>CoordinateTilePairs</c> and <c>RegisterTile</c> - so all four are written, each
        /// guarded on its own: a half-registered tile is worse than a skipped one.
        /// Only runs when the player raises BuildGridExtraTiles above 0.
        /// </summary>
        private static void ExpandGrid(Property prop, int extra)
        {
            try
            {
                if (_clonedTiles >= extra) return;
                var grids = prop.Grids;
                if (grids == null || grids.Count == 0) return;

                Grid grid = grids[0];
                if (grid == null || grid.Tiles == null || grid.Tiles.Count == 0)
                {
                    Core.Log.Warning("[Grid] no template tile to clone from - extra tiles skipped.");
                    return;
                }

                Tile template = grid.Tiles[0];
                if (template == null) return;
                float size = Grid.TileSize;          // static on Grid, not per-instance
                float offset = template.AvailableOffset;

                // Rings around the template keep new tiles adjacent to the existing floor instead of trailing off
                // in one direction. Eight rings is far more than BuildGridExtraTiles' cap could ever need.
                int added = 0;
                for (int ring = 1; ring <= 8 && added < extra; ring++)
                    for (int dx = -ring; dx <= ring && added < extra; dx++)
                        for (int dz = -ring; dz <= ring && added < extra; dz++)
                        {
                            if (Mathf.Abs(dx) != ring && Mathf.Abs(dz) != ring) continue;   // walk the ring edge only
                            int cx = template.x + dx, cz = template.y + dz;
                            var coord = new Coordinate(cx, cz);
                            if (grid.GetTile(coord) != null) continue;   // already covered

                            // Only lay floor that is actually inside the RV. A tile outside the property's bounds
                            // is unusable in the build UI and just looks like the grid leaks through the wall -
                            // a first version happily cloned five of those into the street.
                            Vector3 world = grid.transform.TransformPoint(new Vector3(cx * size, 0f, cz * size));
                            bool inside;
                            try { inside = prop.DoBoundsContainPoint(world); } catch { inside = true; }
                            if (!inside) continue;

                            if (CloneTile(grid, template, cx, cz, size, offset)) added++;
                        }

                _clonedTiles += added;
                Core.Log.Msg("[Grid] added " + added + " extra tile(s) (requested " + extra + ").");
            }
            catch (Exception e) { Core.Log.Warning("[Grid] expand failed: " + e.Message); }
        }

        private static bool CloneTile(Grid grid, Tile template, int cx, int cz, float size, float offset)
        {
            try
            {
                GameObject go = UnityEngine.Object.Instantiate(template.gameObject, grid.transform);
                go.name = "RVRepairVan_Tile_" + cx + "_" + cz;
                go.transform.localPosition = new Vector3(cx * size, 0f, cz * size);
                go.SetActive(true);

                Tile tile = go.GetComponent<Tile>();
                if (tile == null) { UnityEngine.Object.Destroy(go); return false; }
                tile.InitializePropertyTile(cx, cz, offset, grid);

                // RegisterTile already appends to BOTH Tiles and CoordinateTilePairs, so adding to those by hand
                // registers every tile twice - the measured symptom was a grid reporting 66 tiles after 24 were
                // added to 18. The one book it does NOT write is _coordinateToTile, which is what GetTile reads.
                try { grid.RegisterTile(tile); } catch { }
                try { if (grid._coordinateToTile != null) grid._coordinateToTile[new Coordinate(cx, cz)] = tile; } catch { }
                return true;
            }
            catch { return false; }
        }

        private static Property PropertyOf()
        {
            Transform root = RVManager.Root;
            return root != null ? root.GetComponent<Property>() : null;
        }
    }
}
