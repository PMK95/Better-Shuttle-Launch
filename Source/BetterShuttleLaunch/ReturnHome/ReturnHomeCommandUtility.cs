using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.Shuttles;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.ReturnHome
{
    public static class ReturnHomeCommandUtility
    {
        public static bool CanStartReturnHomeWithLandingSelection(Building_PassengerShuttle shuttle, MapParent home, out string disabledReason)
        {
            disabledReason = null;
            if (!CanUseHomeDestination(home, out disabledReason))
            {
                return false;
            }

            if (!PassengerShuttleLaunchBridge.TryGetLaunchParts(shuttle, out CompLaunchable launchable, out _, out disabledReason))
            {
                return false;
            }

            AcceptanceReport canLaunch = launchable.CanLaunch();
            if (canLaunch.Accepted)
            {
                return true;
            }

            disabledReason = canLaunch.Reason;
            return false;
        }

        public static void StartReturnHomeWithLandingSelection(Building_PassengerShuttle shuttle, MapParent home)
        {
            if (!CanStartReturnHomeWithLandingSelection(shuttle, home, out string disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            PassengerShuttleLaunchBridge.TryChooseSpecificWorldTarget(shuttle, new GlobalTargetInfo(home), shuttle.LaunchableComp.TryLaunch);
        }

        public static bool CanStartReturnHomeAtLastDepartureCell(Building_PassengerShuttle shuttle, MapParent home, out string disabledReason)
        {
            disabledReason = null;
            if (!CanUseHomeDestination(home, out disabledReason))
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

            return PassengerShuttleLaunchBridge.CanChooseSpecificLandingCell(shuttle, home, location.Cell, location.Rotation, out disabledReason);
        }

        public static void StartReturnHomeAtLastDepartureCell(Building_PassengerShuttle shuttle, MapParent home)
        {
            if (!CanUseHomeDestination(home, out string disabledReason))
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

            if (!PassengerShuttleLaunchBridge.TryChooseSpecificLandingCell(shuttle, home, location.Cell, location.Rotation, shuttle.LaunchableComp.TryLaunch, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
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
