using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.Settings;
using BetterShuttleLaunch.Shuttles;
using BetterShuttleLaunch.UI;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.ReturnHome
{
    public static class ReturnHomeCommandUtility
    {
        public static Gizmo CreateReturnHomeCommand(Building_PassengerShuttle shuttle)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "BSL_ReturnHome".Translate(),
                defaultDesc = "BSL_ReturnHomeDesc".Translate(),
                icon = CompLaunchable.LaunchCommandTex,
                action = () => StartReturnHome(shuttle)
            };

            if (!PassengerShuttleLaunchBridge.TryGetLaunchParts(shuttle, out CompLaunchable launchable, out _, out string failReason))
            {
                command.Disable(failReason);
                return command;
            }

            AcceptanceReport canLaunch = launchable.CanLaunch();
            if (!canLaunch.Accepted)
            {
                command.Disable(canLaunch.Reason);
            }

            return command;
        }

        public static bool CanStartReturnHome(Building_PassengerShuttle shuttle, out string disabledReason)
        {
            disabledReason = null;
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

        public static void StartReturnHome(Building_PassengerShuttle shuttle)
        {
            IReadOnlyList<MapParent> homes = HomeDestinationFinder.FindPlayerHomeMapParents();
            if (homes.Count == 0)
            {
                Messages.Message("BSL_NoValidHome".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (homes.Count == 1)
            {
                LaunchTowardHome(shuttle, homes[0]);
                return;
            }

            Find.WindowStack.Add(new Dialog_SelectHomeDestination(homes, home => LaunchTowardHome(shuttle, home)));
        }

        private static void LaunchTowardHome(Building_PassengerShuttle shuttle, MapParent home)
        {
            string failReason = null;
            if (BetterShuttleLaunchMod.ActiveSettings.AutoLandReturnHomeAtLastDepartureCell
                && LaunchQueueGameComponent.Current != null
                && LaunchQueueGameComponent.Current.TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location)
                && location.IsUsableFor(home)
                && PassengerShuttleLaunchBridge.TryChooseSpecificLandingCell(shuttle, home, location.Cell, location.Rotation, shuttle.LaunchableComp.TryLaunch, out failReason))
            {
                return;
            }

            if (!failReason.NullOrEmpty())
            {
                Messages.Message("BSL_AutoReturnLandingFallback".Translate(failReason), MessageTypeDefOf.RejectInput, false);
            }

            GlobalTargetInfo target = new GlobalTargetInfo(home);
            PassengerShuttleLaunchBridge.TryChooseSpecificWorldTarget(shuttle, target, shuttle.LaunchableComp.TryLaunch);
        }
    }
}
