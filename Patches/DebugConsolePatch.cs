#if DEBUG
using System;
using HarmonyLib;
using Il2CppScheduleOne.Product.Packaging;            // PackagingDefinition

namespace RVRepairVan.Patches
{
    /// <summary>
    /// DEBUG-only test helper. Typing <c>rvtest</c> in the dev console drops a set of pre-packaged products at every
    /// quality into the inventory, so the Marco sample / quality-multiplier flow can be tested without manually
    /// equipping + setquality + packageproduct each one (packageproduct only works on the equipped-in-hand item).
    /// Usage: <c>rvtest</c> (jar OG Kush) or <c>rvtest &lt;packaging&gt; &lt;productId&gt;</c>. Compiled out of Release.
    ///
    /// Both Console.SubmitCommand overloads are patched (string + List&lt;string&gt;): the real console UI and
    /// scripted submitters call the string overload, but depending on build/config either overload may be the
    /// one whose managed prefix actually fires, so catching both is the reliable path (mirrors the Litterally /
    /// Siesta / Snitch / Hotline console patches). Dispatch dedupes per frame so a command with side effects
    /// (rvstage / rvtest) is never applied twice when both prefixes fire for a single submission.
    /// </summary>
    internal static class DebugConsolePatch
    {
        private static readonly EQuality[] Qualities =
            { EQuality.Trash, EQuality.Poor, EQuality.Standard, EQuality.Premium, EQuality.Heavenly };

        internal static bool TryHandle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;
            return Dispatch(raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        internal static bool TryHandle(Il2CppSystem.Collections.Generic.List<string> args)
        {
            if (args == null || args.Count == 0) return false;
            string[] parts = new string[args.Count];
            for (int i = 0; i < args.Count; i++) parts[i] = args[i];
            return Dispatch(parts);
        }

        private static int _lastFrame = -1;
        private static string _lastSig = "";

        /// <summary>Returns true if the command was one of ours (and should be swallowed), false to let the game handle it.</summary>
        private static bool Dispatch(string[] parts)
        {
            if (parts.Length == 0) return false;
            string cmd = parts[0].ToLower();
            if (cmd != "rvtest" && cmd != "rvstage" && cmd != "rvdrops" && cmd != "rvclear"
                && cmd != "rvgiveclient" && cmd != "rvdiag" && cmd != "rvreinject")
                return false;   // not ours - let the game handle it

            // Both overloads can fire for a single submission (the string body calls the List body), so run the
            // side effects once per identical command+frame and swallow the duplicate.
            string sig = string.Join(" ", parts);
            int frame = Time.frameCount;
            if (frame == _lastFrame && sig == _lastSig) return true;   // already handled this frame
            _lastFrame = frame; _lastSig = sig;

            try
            {
                switch (cmd)
                {
                    case "rvtest":
                        GiveTestProducts(parts.Length > 1 ? parts[1] : "jar", parts.Length > 2 ? parts[2] : "ogkush");
                        break;
                    case "rvstage":   // skip errands while testing - run ON THE HOST (it broadcasts to clients)
                        if (parts.Length > 1 && int.TryParse(parts[1], out int st)) RVRepairVan.Quests.Questline.DebugSetStage(st);
                        else Core.Log.Warning("[Debug] rvstage <n>  (1=Start 5=Referred 6=MarcoMet 7=ReadyToPay 8=Trusted 9=Paid). Run on the HOST.");
                        break;
                    case "rvdrops":   // dump every dead drop: name, position, empty? - to diagnose the pickup drop
                        DumpDrops();
                        break;
                    case "rvclear":   // wipe accumulated test crates/packages from ALL drops + re-reserve a fresh one
                        ClearErrandItems();
                        RVRepairVan.Quests.Questline.DebugResetErrandDrop();
                        break;
                    case "rvdiag":    // dump the quest gate + every injected dialogue choice
                        RVRepairVan.Quests.Questline.DebugDiag();
                        break;
                    case "rvreinject":   // 'rvreinject degraded' exercises the no-container fallback (adds duplicates)
                        RVRepairVan.Quests.Questline.DebugReinject(parts.Length > 1 && parts[1].ToLower() == "degraded");
                        break;
                    case "rvgiveclient":   // host: tell every CLIENT to spawn packaged test products (jar OG Kush)
                        RVRepairVan.Net.NetworkBus.BroadcastToAll(RVRepairVan.Net.RvOp.DebugGiveItems);
                        Core.Log.Msg("[Debug] rvgiveclient: broadcast DebugGiveItems to clients (run this on the HOST).");
                        break;
                }
            }
            catch (Exception e) { Core.Log.Warning("[Debug] console cmd failed: " + e.Message); }
            return true;   // ours - swallow it either way
        }

        internal static void ClearErrandItems()
        {
            try
            {
                var all = S1API.DeadDrops.DeadDropManager.All;
                if (all == null) { Core.Log.Msg("[Debug] rvclear: no drops."); return; }
                int n = 0;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    n += all[i].Storage.RemoveAllOfDefinition("rv_ming_crate");
                    n += all[i].Storage.RemoveAllOfDefinition("rv_marco_package");
                }
                Core.Log.Msg("[Debug] rvclear: removed " + n + " quest item(s) from all drops.");
            }
            catch (Exception e) { Core.Log.Warning("[Debug] rvclear failed: " + e.Message); }
        }

        internal static void DumpDrops()
        {
            try
            {
                var all = S1API.DeadDrops.DeadDropManager.All;
                if (all == null) { Core.Log.Msg("[Debug] rvdrops: DeadDropManager.All is null."); return; }
                Core.Log.Msg("[Debug] rvdrops: " + all.Length + " dead drops (empty=no items):");
                for (int i = 0; i < all.Length; i++)
                {
                    var d = all[i];
                    if (d == null) continue;
                    Core.Log.Msg("  [" + i + "] '" + d.Name + "' pos=(" + Mathf.RoundToInt(d.Position.x)
                        + "," + Mathf.RoundToInt(d.Position.z) + ") empty=" + d.IsEmpty);
                }
            }
            catch (Exception e) { Core.Log.Warning("[Debug] rvdrops failed: " + e.Message); }
        }

        internal static void GiveTestProducts(string packagingId, string productId)
        {
            PlayerInventory inv = PlayerSingleton<PlayerInventory>.Instance;
            if (inv == null) { Core.Log.Warning("[Debug] rvtest: no PlayerInventory."); return; }

            ProductDefinition def = Il2CppScheduleOne.Registry.GetItem(productId.ToLower())?.TryCast<ProductDefinition>();
            PackagingDefinition pkg = Il2CppScheduleOne.Registry.GetItem(packagingId.ToLower())?.TryCast<PackagingDefinition>();
            if (def == null) { Core.Log.Warning("[Debug] rvtest: unknown product '" + productId + "'."); return; }
            if (pkg == null) { Core.Log.Warning("[Debug] rvtest: unknown packaging '" + packagingId + "'."); return; }

            int given = 0;
            foreach (EQuality q in Qualities)
            {
                ProductItemInstance inst = def.GetDefaultInstance(1)?.TryCast<ProductItemInstance>();
                if (inst == null) continue;
                inst.Quality = q;
                inst.SetPackaging(pkg);   // the exact call the 'packageproduct' console command uses
                if (!inv.CanItemFitInInventory(inst)) { Core.Log.Warning("[Debug] rvtest: inventory full at " + q + "."); break; }
                inv.AddItemToInventory(inst);
                given++;
            }
            Core.Log.Msg("[Debug] rvtest: gave " + given + " packaged " + def.Name + " (" + pkg.Name + ") - one per quality.");
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.Console), nameof(Il2CppScheduleOne.Console.SubmitCommand), new Type[] { typeof(string) })]
    internal static class Rv_Console_SubmitCommand_String_Patch
    {
        private static bool Prefix(string args)
        {
            try { return !DebugConsolePatch.TryHandle(args); } catch { return true; }
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.Console), nameof(Il2CppScheduleOne.Console.SubmitCommand), new Type[] { typeof(Il2CppSystem.Collections.Generic.List<string>) })]
    internal static class Rv_Console_SubmitCommand_List_Patch
    {
        private static bool Prefix(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try { return !DebugConsolePatch.TryHandle(args); } catch { return true; }
        }
    }
}
#endif
