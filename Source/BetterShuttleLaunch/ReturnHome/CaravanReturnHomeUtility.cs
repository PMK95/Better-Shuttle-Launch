using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.Shuttles;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.ReturnHome
{
    public static class CaravanReturnHomeUtility
    {
        public static bool CanStartCaravanReturnHomeWithLandingSelection(Caravan caravan, MapParent home, out string disabledReason)
        {
            disabledReason = null;
            if (!CanUseHomeDestination(home, out disabledReason))
            {
                return false;
            }

            if (!TryGetCaravanLaunchParts(caravan, out _, out _, out disabledReason))
            {
                return false;
            }

            AcceptanceReport canLaunch = CaravanShuttleUtility.CanLaunchCaravanShuttle(caravan);
            if (canLaunch.Accepted)
            {
                return true;
            }

            disabledReason = canLaunch.Reason;
            return false;
        }

        public static void StartCaravanReturnHomeWithLandingSelection(Caravan caravan, MapParent home)
        {
            if (!CanStartCaravanReturnHomeWithLandingSelection(caravan, home, out string disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            TryGetCaravanLaunchParts(caravan, out Building_PassengerShuttle shuttle, out CompTransporter transporter, out _);
            CompLaunchable launchable = shuttle.LaunchableComp;
            List<CompTransporter> transporters = new List<CompTransporter> { transporter };
            GlobalTargetInfo target = new GlobalTargetInfo(home);
            CompLaunchable.ChoseWorldTarget(
                target,
                caravan.Tile,
                transporters,
                launchable.MaxLaunchDistanceEver(target.Tile.Layer),
                (tile, action) => CaravanShuttleUtility.LaunchShuttle(caravan, tile, action),
                launchable,
                shuttle.FuelLevel);
        }

        public static bool CanStartCaravanReturnHomeAtLastDepartureCell(Caravan caravan, MapParent home, out string disabledReason)
        {
            disabledReason = null;
            if (!CanUseHomeDestination(home, out disabledReason))
            {
                return false;
            }

            if (!TryGetCaravanLaunchParts(caravan, out Building_PassengerShuttle shuttle, out _, out disabledReason))
            {
                return false;
            }

            if (LaunchQueueGameComponent.Current == null
                || !LaunchQueueGameComponent.Current.TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location)
                || !location.IsUsableFor(home))
            {
                disabledReason = "BSL_LastDepartureCellUnavailable".Translate();
                return false;
            }

            return PassengerShuttleLaunchBridge.CanChooseSpecificLandingCellFromCaravan(caravan, home, location.Cell, location.Rotation, out disabledReason);
        }

        public static void StartCaravanReturnHomeAtLastDepartureCell(Caravan caravan, MapParent home)
        {
            if (!CanUseHomeDestination(home, out string disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!TryGetCaravanLaunchParts(caravan, out Building_PassengerShuttle shuttle, out _, out disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (LaunchQueueGameComponent.Current == null
                || !LaunchQueueGameComponent.Current.TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location)
                || !location.IsUsableFor(home))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!PassengerShuttleLaunchBridge.TryChooseSpecificLandingCellFromCaravan(caravan, home, location.Cell, location.Rotation, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
        }

        private static bool TryGetCaravanLaunchParts(Caravan caravan, out Building_PassengerShuttle shuttle, out CompTransporter transporter, out string disabledReason)
        {
            shuttle = caravan?.Shuttle;
            transporter = shuttle?.TransporterComp;
            disabledReason = null;
            if (caravan != null && caravan.Faction == Faction.OfPlayer && shuttle != null && shuttle.LaunchableComp != null && transporter != null)
            {
                return true;
            }

            disabledReason = "BSL_ShuttleUnavailable".Translate();
            return false;
        }

        private static bool CanUseHomeDestination(MapParent home, out string disabledReason)
        {
            disabledReason = null;
            if (home != null && home.Spawned && home.HasMap)
            {
                return true;
            }

            disabledReason = "BSL_DestinationInvalid".Translate();
            return false;
        }
    }
}
