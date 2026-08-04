using System;
using DooDesch.Localization;
using RVRepairVan.Config;
using RVRepairVan.Managers;
using RVRepairVan.Net;
using RVRepairVan.Persistence;
using RVRepairVan.Quests;

namespace RVRepairVan.Base
{
    /// <summary>Upgrades Marco sells once the RV is repaired. Stored as a bitmask, so the order is free.</summary>
    [Flags]
    internal enum RvUpgrade
    {
        None = 0,
        GutInterior = 1,     // clears the bed/bench/cabinets/chairs/partition to make room
        WorkshopFloor = 2,   // turns on the RV's build grid so equipment can be placed
        CrewQuarters = 4,    // employee capacity + idle points + a walkable interior
        LoadingDock = 8,     // a delivery destination at the RV
    }

    /// <summary>
    /// The RV build-out: four things Marco sells after the repair, each paid for, each applied through the same
    /// short cinematic. Nothing here is free - that is the point of the tier, and it is what separates this from
    /// simply switching the features on.
    ///
    /// The upgrades are runtime effects on scene objects (a grid activated, an array filled, a dock instantiated),
    /// not serialized world state. So the save holds only the bitmask and <see cref="ApplyOwned"/> re-applies
    /// everything on each scene load. Every module's Apply must therefore be idempotent.
    ///
    /// Co-op mirrors the questline: a client sends <see cref="RvOp.BuyUpgrade"/>, the host validates funds and
    /// prerequisites, charges the shared pool once, and broadcasts the new mask.
    /// </summary>
    internal static class RvUpgrades
    {
        // Applied in dependency order - the crew needs the grid, which needs the room.
        private static readonly RvUpgrade[] Order =
        {
            RvUpgrade.GutInterior, RvUpgrade.WorkshopFloor, RvUpgrade.CrewQuarters, RvUpgrade.LoadingDock,
        };

        /// <summary>The full owned mask. Setting it on the host replicates, exactly like Questline.Stage.</summary>
        internal static int Mask
        {
            get => RepairStateStore.GetUpgrades();
            private set
            {
                RepairStateStore.SetUpgrades(value);
                if (NetworkBus.Online && NetworkBus.IsServer) NetworkBus.BroadcastToAll(RvOp.UpgradeSync, value);
            }
        }

        internal static bool Owned(RvUpgrade up) => (Mask & (int)up) != 0;

        internal static int Price(RvUpgrade up)
        {
            switch (up)
            {
                case RvUpgrade.GutInterior: return RVRepairVanPreferences.PriceGutInterior;
                case RvUpgrade.WorkshopFloor: return RVRepairVanPreferences.PriceWorkshopFloor;
                case RvUpgrade.CrewQuarters: return RVRepairVanPreferences.PriceCrewQuarters;
                case RvUpgrade.LoadingDock: return RVRepairVanPreferences.PriceLoadingDock;
                default: return 0;
            }
        }

        /// <summary>What has to be owned first. The dock stands alone; the interior chain builds on itself.</summary>
        private static RvUpgrade Prerequisite(RvUpgrade up)
        {
            switch (up)
            {
                case RvUpgrade.WorkshopFloor: return RvUpgrade.GutInterior;
                case RvUpgrade.CrewQuarters: return RvUpgrade.WorkshopFloor;
                default: return RvUpgrade.None;
            }
        }

        /// <summary>The Marco choice text, with the live price - refreshed like the repair choice.</summary>
        internal static string ChoiceText(RvUpgrade up)
        {
            string money = MoneyManager.FormatAmount(Price(up));
            switch (up)
            {
                case RvUpgrade.GutInterior: return L10n.T("Gut the interior. ({0})", money);
                case RvUpgrade.WorkshopFloor: return L10n.T("Build me a workshop floor. ({0})", money);
                case RvUpgrade.CrewQuarters: return L10n.T("Make room for a crew. ({0})", money);
                case RvUpgrade.LoadingDock: return L10n.T("I need a loading dock. ({0})", money);
                default: return "";
            }
        }

        private static string DoneLine(RvUpgrade up)
        {
            switch (up)
            {
                case RvUpgrade.GutInterior:
                    return L10n.T("Stripped her out. Bed, bench, the lot - it's a box on wheels now. Do something with it.");
                case RvUpgrade.WorkshopFloor:
                    return L10n.T("Floor's braced and levelled. Bolt whatever you want to it, I don't want to know what.");
                case RvUpgrade.CrewQuarters:
                    return L10n.T("She'll hold a crew now. Put a locker in there or they'll stand around looking at you.");
                case RvUpgrade.LoadingDock:
                    return L10n.T("Dock's in. Tell your supplier to bring it here and stop making you drive.");
                default: return "";
            }
        }

        /// <summary>Should Marco offer this upgrade right now? Drives the dialogue choice's visibility check.</summary>
        internal static bool Available(RvUpgrade up)
        {
            if (!RVRepairVanPreferences.Enabled || !RVRepairVanPreferences.UpgradesEnabled) return false;
            if (!RepairStateStore.GetRepaired()) return false;   // Marco sells build-outs, not repairs
            if (Owned(up)) return false;
            RvUpgrade need = Prerequisite(up);
            return need == RvUpgrade.None || Owned(need);
        }

        // --- buying ---------------------------------------------------------------------------------------

        /// <summary>
        /// Player picked an upgrade at Marco. On a co-op client this is an intent - the host charges the shared
        /// pool and broadcasts; we still play the cinematic locally so it does not look like nothing happened.
        /// </summary>
        internal static void Buy(RvUpgrade up)
        {
            try
            {
                if (!Available(up)) return;

                if (NetworkBus.Online && !NetworkBus.IsServer)
                {
                    // The host owns the money and the mask. Play the black locally with no effect of our own -
                    // the host's UpgradeSync applies the actual change while we are looking at the veil.
                    NetworkBus.SendToHost(RvOp.BuyUpgrade, (int)up);
                    Effects.RepairCinematic.PlayUpgrade(null, null);
                    return;
                }

                HostBuy(up, withCinematic: true);
            }
            catch (Exception e) { Core.Log.Warning("[Upgrade] buy " + up + " failed: " + e.Message); }
        }

        /// <summary>
        /// Host (or offline): validate, charge once, apply, replicate. Charging happens BEFORE the cinematic for
        /// the same reason the repair does it - an interrupted fade must never be able to bill twice.
        /// </summary>
        internal static void HostBuy(RvUpgrade up, bool withCinematic)
        {
            try
            {
                if (!Available(up)) return;
                int price = Price(up);
                if (S1API.Money.Money.GetCashBalance() < price)
                {
                    Core.Log.Msg("[Upgrade] " + up + " rejected - short of " + MoneyManager.FormatAmount(price) + ".");
                    Questline.SayMarco(L10n.T("Come back when you've got {0}.", MoneyManager.FormatAmount(price)));
                    return;
                }
                S1API.Money.Money.ChangeCashBalance(-price, true, true);

                Action commit = () =>
                {
                    Mask = Mask | (int)up;   // setter replicates to clients
                    Apply(up);
                    Core.Log.Msg("[Upgrade] " + up + " bought for " + MoneyManager.FormatAmount(price) + ".");
                };

                if (withCinematic)
                    Effects.RepairCinematic.PlayUpgrade(() => commit(), () => Questline.SayMarco(DoneLine(up)));
                else
                    commit();   // a client acted - commit now, their own cinematic is already running
            }
            catch (Exception e) { Core.Log.Warning("[Upgrade] host buy " + up + " failed: " + e.Message); }
        }

        /// <summary>Client: adopt the host's mask and apply whatever is newly owned.</summary>
        internal static void ApplySync(int mask)
        {
            if (NetworkBus.IsServer) return;
            RepairStateStore.SetUpgrades(mask);
            ApplyOwned();
            Core.LogDebug("[Upgrade] client applied UpgradeSync mask=" + mask);
        }

        // --- applying -------------------------------------------------------------------------------------

        /// <summary>
        /// Re-apply every owned upgrade, in dependency order. Called after a load and after a sync; each module's
        /// Apply is idempotent, so running this repeatedly is harmless.
        /// </summary>
        internal static void ApplyOwned()
        {
            if (!RVRepairVanPreferences.UpgradesEnabled) return;
            foreach (RvUpgrade up in Order)
                if (Owned(up)) Apply(up);
        }

        private static void Apply(RvUpgrade up)
        {
            try
            {
                switch (up)
                {
                    case RvUpgrade.GutInterior: RvInterior.Apply(); break;
                    case RvUpgrade.WorkshopFloor: RvBuildGrid.Apply(); break;
                    case RvUpgrade.CrewQuarters: RvCrew.Apply(); break;
                    case RvUpgrade.LoadingDock: RvDeliveryDock.Apply(); break;
                }
            }
            catch (Exception e) { Core.Log.Warning("[Upgrade] apply " + up + " failed: " + e.Message); }
        }

        /// <summary>Per-scene reset of the modules' cached transforms. The mask itself lives in the save.</summary>
        internal static void Reset()
        {
            RvInterior.Reset();
            RvBuildGrid.Reset();
            RvCrew.Reset();
            RvDeliveryDock.Reset();
        }

#if DEBUG
        /// <summary>Debug: grant or revoke upgrades directly (rvupg). Host-side; the setter replicates.</summary>
        internal static void DebugSetMask(int mask)
        {
            Mask = mask;
            RvInterior.Reset(); RvBuildGrid.Reset(); RvCrew.Reset(); RvDeliveryDock.Reset();
            ApplyOwned();
            Core.Log.Msg("[Debug] rvupg -> mask=" + mask + " (" + (RvUpgrade)mask + ")");
        }

        internal static void DebugDump()
        {
            Core.Log.Msg("[Debug] upgrades mask=" + Mask + " enabled=" + RVRepairVanPreferences.UpgradesEnabled
                + " repaired=" + RepairStateStore.GetRepaired());
            foreach (RvUpgrade up in Order)
                Core.Log.Msg("[Debug]   " + up + " owned=" + Owned(up) + " available=" + Available(up)
                    + " price=" + Price(up));
        }
#endif
    }
}
