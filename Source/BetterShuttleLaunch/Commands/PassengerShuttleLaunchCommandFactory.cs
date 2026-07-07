using System;
using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.ReturnHome;
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
                icon = isQueued
                    ? BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandCancelLaunch, CompTransporter.CancelLoadCommandTex)
                    : BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandLaunchWhenReady, CompLaunchable.LaunchCommandTex)
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
                icon = singleQueued
                    ? BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandCancelLaunch, CompTransporter.CancelLoadCommandTex)
                    : BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandLaunchWhenReady, CompLaunchable.LaunchCommandTex)
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
                icon = isQueued
                    ? BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandCancelLaunch, CompTransporter.CancelLoadCommandTex)
                    : BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandLaunchWhenReady, CompLaunchable.LaunchCommandTex)
            };

            return command;
        }

        private static List<FloatMenuOption> CreateMapShuttleRightClickOptions(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            AddCancelMapLaunchOption(options, shuttles);
            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_LaunchToSettlement".Translate(),
                "BSL_MenuLaunchToSettlementDesc".Translate(),
                () => StartSettlementLaunch(shuttles),
                GetSettlementLaunchDisabledReason(shuttles)));
            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_ReturnToLastDeparture".Translate(),
                "BSL_MenuReturnDesc".Translate(),
                () => StartReturnLaunch(shuttles),
                GetReturnLaunchDisabledReason(shuttles)));
            return options;
        }

        private static List<FloatMenuOption> CreateCaravanRightClickOptions(Caravan caravan)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (LaunchQueueGameComponent.Current?.IsQueued(caravan) == true)
            {
                options.Add(CreateMenuOptionWithTooltip("BSL_CancelQueuedLaunch".Translate(), () => CancelQueuedCaravanLaunch(caravan), "BSL_MenuCancelQueuedLaunchDesc".Translate()));
            }

            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_LaunchToSettlement".Translate(),
                "BSL_MenuLaunchToSettlementDesc".Translate(),
                () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanSettlementLaunchFlow(caravan),
                GetCaravanSettlementLaunchDisabledReason(caravan)));
            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_ReturnToLastDeparture".Translate(),
                "BSL_MenuReturnDesc".Translate(),
                () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanReturnFlow(caravan),
                PassengerShuttleLaunchQueueCommandUtility.CanStartReturnFlow(caravan?.Shuttle) ? null : "BSL_LastDepartureCellUnavailable".Translate()));
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
                options.Add(CreateMenuOptionWithTooltip("BSL_CancelQueuedLaunch".Translate(), () => CancelQueuedMapLaunch(queuedShuttles[0]), "BSL_MenuCancelQueuedLaunchDesc".Translate()));
                return;
            }

            options.Add(CreateMenuOptionWithTooltip("BSL_CancelQueuedLaunch".Translate(), () => SelectMapShuttle(queuedShuttles, CancelQueuedMapLaunch), "BSL_MenuCancelQueuedLaunchDesc".Translate()));
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

            SelectMapShuttle(FindSettlementLaunchTargetShuttles(shuttles), PassengerShuttleLaunchQueueCommandUtility.StartSettlementLaunchFlow);
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

            SelectMapShuttle(FindReturnTargetShuttles(shuttles), PassengerShuttleLaunchQueueCommandUtility.StartReturnFlow);
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

        private static string GetSettlementLaunchDisabledReason(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                return "BSL_NoShuttles".Translate();
            }

            if (FindSettlementLaunchTargetShuttles(shuttles).Count > 0)
            {
                return null;
            }

            return "BSL_NoOtherSettlement".Translate();
        }

        private static string GetReturnLaunchDisabledReason(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                return "BSL_NoShuttles".Translate();
            }

            return FindReturnTargetShuttles(shuttles).Count > 0 ? null : "BSL_LastDepartureCellUnavailable".Translate();
        }

        private static string GetCaravanSettlementLaunchDisabledReason(Caravan caravan)
        {
            if (!PassengerShuttleLaunchQueueCommandUtility.CanQueueCaravanLaunchWhenReady(caravan, out string disabledReason))
            {
                return disabledReason;
            }

            return SettlementDestinationFinder.FindOtherPlayerHomeMapParents(null).Count > 0 ? null : "BSL_NoOtherSettlement".Translate();
        }

        private static List<Building_PassengerShuttle> FindSettlementLaunchTargetShuttles(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> availableShuttles = new List<Building_PassengerShuttle>();
            for (int i = 0; i < shuttles.Count; i++)
            {
                if (PassengerShuttleLaunchQueueCommandUtility.CanStartSettlementLaunchFlow(shuttles[i]))
                {
                    availableShuttles.Add(shuttles[i]);
                }
            }

            return availableShuttles;
        }

        private static List<Building_PassengerShuttle> FindReturnTargetShuttles(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> availableShuttles = new List<Building_PassengerShuttle>();
            for (int i = 0; i < shuttles.Count; i++)
            {
                if (PassengerShuttleLaunchQueueCommandUtility.CanStartReturnFlow(shuttles[i]))
                {
                    availableShuttles.Add(shuttles[i]);
                }
            }

            return availableShuttles;
        }

        private static FloatMenuOption CreateConditionalOptionWithTooltip(string label, string description, Action action, string disabledReason)
        {
            string tooltip = disabledReason.NullOrEmpty()
                ? description
                : "BSL_DisabledReasonTooltip".Translate(description, disabledReason);
            return CreateMenuOptionWithTooltip(label, disabledReason.NullOrEmpty() ? action : null, tooltip);
        }

        private static FloatMenuOption CreateMenuOptionWithTooltip(string label, Action action, string tooltip)
        {
            return new FloatMenuOption(label, action)
            {
                tooltip = tooltip.NullOrEmpty() ? (TipSignal?)null : new TipSignal(tooltip)
            };
        }
    }
}
