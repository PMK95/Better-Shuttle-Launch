using System;
using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.UI;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Commands
{
    public static class PassengerShuttleLaunchCommandFactory
    {
        public static Gizmo CreateForMapShuttle(Building_PassengerShuttle shuttle)
        {
            bool isQueued = LaunchQueueGameComponent.Current?.IsQueued(shuttle) == true;
            Command_PassengerShuttleLaunchMenu command = new Command_PassengerShuttleLaunchMenu(
                isQueued ? () => CancelQueuedMapLaunch(shuttle) : () => PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow(shuttle),
                () => CreateMapShuttleRightClickOptions(new List<Building_PassengerShuttle> { shuttle }))
            {
                defaultLabel = isQueued ? "BSL_CancelQueuedLaunch".Translate() : "BSL_LaunchWhenReady".Translate(),
                defaultDesc = isQueued ? "BSL_CancelQueuedLaunchDesc".Translate() : "BSL_LaunchWhenReadyDesc".Translate(),
                icon = isQueued ? CompTransporter.CancelLoadCommandTex : CompLaunchable.LaunchCommandTex
            };

            return command;
        }

        public static Gizmo CreateForMapParent(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> shuttleList = new List<Building_PassengerShuttle>(shuttles);
            bool singleQueued = shuttleList.Count == 1 && LaunchQueueGameComponent.Current?.IsQueued(shuttleList[0]) == true;
            Command_PassengerShuttleLaunchMenu command = new Command_PassengerShuttleLaunchMenu(
                singleQueued ? () => CancelQueuedMapLaunch(shuttleList[0]) : () => StartQueuedMapLaunch(shuttleList),
                () => CreateMapShuttleRightClickOptions(shuttleList))
            {
                defaultLabel = singleQueued ? "BSL_CancelQueuedLaunch".Translate() : "BSL_LaunchWhenReady".Translate(),
                defaultDesc = singleQueued ? "BSL_CancelQueuedLaunchDesc".Translate() : "BSL_LaunchWhenReadyDesc".Translate(),
                icon = singleQueued ? CompTransporter.CancelLoadCommandTex : CompLaunchable.LaunchCommandTex
            };

            if (shuttleList.Count == 0)
            {
                command.Disable("BSL_NoAvailableShuttles".Translate());
            }

            return command;
        }

        public static Gizmo CreateForCaravan(Caravan caravan)
        {
            bool isQueued = LaunchQueueGameComponent.Current?.IsQueued(caravan) == true;
            Command_PassengerShuttleLaunchMenu command = new Command_PassengerShuttleLaunchMenu(
                isQueued ? () => CancelQueuedCaravanLaunch(caravan) : () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanLaunchWhenReadyFlow(caravan),
                () => CreateCaravanRightClickOptions(caravan))
            {
                defaultLabel = isQueued ? "BSL_CancelQueuedLaunch".Translate() : "BSL_LaunchWhenReady".Translate(),
                defaultDesc = isQueued ? "BSL_CancelQueuedLaunchDesc".Translate() : "BSL_LaunchWhenReadyDesc".Translate(),
                icon = isQueued ? CompTransporter.CancelLoadCommandTex : CompLaunchable.LaunchCommandTex
            };

            return command;
        }

        private static List<FloatMenuOption> CreateMapShuttleRightClickOptions(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            AddCancelMapLaunchOption(options, shuttles);
            options.Add(new FloatMenuOption("BSL_LaunchToSettlement".Translate(), () => StartSettlementLaunch(shuttles)));
            options.Add(new FloatMenuOption("BSL_ReturnToLastDeparture".Translate(), () => StartReturnLaunch(shuttles)));
            return options;
        }

        private static List<FloatMenuOption> CreateCaravanRightClickOptions(Caravan caravan)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (LaunchQueueGameComponent.Current?.IsQueued(caravan) == true)
            {
                options.Add(new FloatMenuOption("BSL_CancelQueuedLaunch".Translate(), () => CancelQueuedCaravanLaunch(caravan)));
            }

            options.Add(new FloatMenuOption("BSL_LaunchToSettlement".Translate(), () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanSettlementLaunchFlow(caravan)));
            options.Add(new FloatMenuOption("BSL_ReturnToLastDeparture".Translate(), () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanReturnFlow(caravan)));
            return options;
        }

        private static void AddCancelMapLaunchOption(List<FloatMenuOption> options, IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> queuedShuttles = FindQueuedMapShuttles(shuttles);
            if (queuedShuttles.Count == 0)
            {
                return;
            }

            if (queuedShuttles.Count == 1)
            {
                options.Add(new FloatMenuOption("BSL_CancelQueuedLaunch".Translate(), () => CancelQueuedMapLaunch(queuedShuttles[0])));
                return;
            }

            options.Add(new FloatMenuOption("BSL_CancelQueuedLaunch".Translate(), () => SelectMapShuttle(queuedShuttles, CancelQueuedMapLaunch)));
        }

        private static void StartQueuedMapLaunch(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow(shuttles[0]);
                return;
            }

            SelectMapShuttle(shuttles, PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow);
        }

        private static void StartSettlementLaunch(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                PassengerShuttleLaunchQueueCommandUtility.StartSettlementLaunchFlow(shuttles[0]);
                return;
            }

            SelectMapShuttle(shuttles, PassengerShuttleLaunchQueueCommandUtility.StartSettlementLaunchFlow);
        }

        private static void StartReturnLaunch(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                PassengerShuttleLaunchQueueCommandUtility.StartReturnFlow(shuttles[0]);
                return;
            }

            SelectMapShuttle(shuttles, PassengerShuttleLaunchQueueCommandUtility.StartReturnFlow);
        }

        private static void SelectMapShuttle(IReadOnlyList<Building_PassengerShuttle> shuttles, Action<Building_PassengerShuttle> selectAction)
        {
            Find.WindowStack.Add(new Dialog_SelectPassengerShuttle(shuttles, selectAction, _ => null));
        }

        private static void CancelQueuedMapLaunch(Building_PassengerShuttle shuttle)
        {
            LaunchQueueGameComponent.Current?.RemoveQueuedLaunch(shuttle, true);
        }

        private static void CancelQueuedCaravanLaunch(Caravan caravan)
        {
            LaunchQueueGameComponent.Current?.RemoveQueuedLaunch(caravan, true);
        }

        private static List<Building_PassengerShuttle> FindQueuedMapShuttles(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> queuedShuttles = new List<Building_PassengerShuttle>();
            for (int i = 0; i < shuttles.Count; i++)
            {
                if (LaunchQueueGameComponent.Current?.IsQueued(shuttles[i]) == true)
                {
                    queuedShuttles.Add(shuttles[i]);
                }
            }

            return queuedShuttles;
        }
    }
}
