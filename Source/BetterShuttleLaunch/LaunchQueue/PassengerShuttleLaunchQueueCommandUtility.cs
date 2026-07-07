using BetterShuttleLaunch.Shuttles;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public static class PassengerShuttleLaunchQueueCommandUtility
    {
        public static Gizmo CreateLaunchWhenReadyCommand(Building_PassengerShuttle shuttle)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "BSL_LaunchWhenReady".Translate(),
                defaultDesc = "BSL_LaunchWhenReadyDesc".Translate(),
                icon = CompLaunchable.LaunchCommandTex,
                action = () => StartLaunchWhenReadyFlow(shuttle)
            };

            if (!PassengerShuttleLaunchBridge.TryGetLaunchParts(shuttle, out _, out _, out string failReason))
            {
                command.Disable(failReason);
            }

            return command;
        }

        public static bool CanQueueLaunchWhenReady(Building_PassengerShuttle shuttle, out string disabledReason)
        {
            return PassengerShuttleLaunchBridge.TryGetLaunchParts(shuttle, out _, out _, out disabledReason);
        }

        public static Gizmo CreateCaravanLaunchWhenReadyCommand(Caravan caravan)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "BSL_LaunchWhenReady".Translate(),
                defaultDesc = "BSL_LaunchWhenReadyDesc".Translate(),
                icon = CompLaunchable.LaunchCommandTex,
                action = () => StartCaravanLaunchWhenReadyFlow(caravan)
            };

            if (!CanQueueCaravanLaunchWhenReady(caravan, out string disabledReason))
            {
                command.Disable(disabledReason);
            }

            return command;
        }

        public static bool CanQueueCaravanLaunchWhenReady(Caravan caravan, out string disabledReason)
        {
            disabledReason = null;
            if (caravan == null || caravan.Faction != Faction.OfPlayer || caravan.Shuttle == null)
            {
                disabledReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            return true;
        }

        public static Gizmo CreateCancelQueuedLaunchCommand(Building_PassengerShuttle shuttle)
        {
            return new Command_Action
            {
                defaultLabel = "BSL_CancelQueuedLaunch".Translate(),
                defaultDesc = "BSL_CancelQueuedLaunchDesc".Translate(),
                icon = CompTransporter.CancelLoadCommandTex,
                action = () => LaunchQueueGameComponent.Current?.RemoveQueuedLaunch(shuttle, true)
            };
        }

        public static Gizmo CreateCancelCaravanQueuedLaunchCommand(Caravan caravan)
        {
            return new Command_Action
            {
                defaultLabel = "BSL_CancelQueuedLaunch".Translate(),
                defaultDesc = "BSL_CancelQueuedLaunchDesc".Translate(),
                icon = CompTransporter.CancelLoadCommandTex,
                action = () => LaunchQueueGameComponent.Current?.RemoveQueuedLaunch(caravan, true)
            };
        }

        public static void StartLaunchWhenReadyFlow(Building_PassengerShuttle shuttle)
        {
            PassengerShuttleLaunchBridge.OpenLoadDialogThenChooseDestination(shuttle, (destinationTile, arrivalAction) =>
            {
                if (arrivalAction == null)
                {
                    Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }

                LaunchQueueGameComponent.Current?.AddOrReplaceQueuedLaunch(new QueuedPassengerShuttleLaunch(shuttle, destinationTile, arrivalAction));
            });
        }

        public static void StartCaravanLaunchWhenReadyFlow(Caravan caravan)
        {
            if (!CanQueueCaravanLaunchWhenReady(caravan, out string disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Building_PassengerShuttle shuttle = caravan.Shuttle;
            shuttle.LaunchableComp.StartChoosingDestination((destinationTile, arrivalAction) =>
            {
                if (arrivalAction == null)
                {
                    Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }

                LaunchQueueGameComponent.Current?.AddOrReplaceQueuedLaunch(new QueuedPassengerShuttleLaunch(caravan, shuttle, destinationTile, arrivalAction));
            });
        }
    }
}
