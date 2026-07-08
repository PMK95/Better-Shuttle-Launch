using System;
using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.ReturnHome;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public static class PassengerShuttleLaunchActionMenu
    {
        public static List<FloatMenuOption> CreateForMapShuttle(Building_PassengerShuttle shuttle)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                CreateOption(
                    "BSL_MenuReadyLaunch".Translate(),
                    () => PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow(shuttle),
                    "BSL_ReadyShortcutTooltip".Translate(),
                    shuttle != null && !shuttle.Destroyed ? null : "BSL_ShuttleUnavailable".Translate())
            };

            AddReturnOptions(options, shuttle, null);
            options.Add(CreateOption(
                "BSL_LaunchToSettlement".Translate(),
                () => PassengerShuttleLaunchQueueCommandUtility.StartSettlementLaunchFlow(shuttle),
                "BSL_MenuLaunchToSettlementDesc".Translate(),
                PassengerShuttleLaunchQueueCommandUtility.CanStartSettlementLaunchFlow(shuttle) ? null : "BSL_NoOtherSettlement".Translate()));
            return options;
        }

        public static List<FloatMenuOption> CreateForCaravan(Caravan caravan)
        {
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                CreateOption(
                    "BSL_MenuReadyLaunch".Translate(),
                    () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanLaunchWhenReadyFlow(caravan),
                    "BSL_ReadyShortcutTooltip".Translate(),
                    caravan != null && !caravan.Destroyed && shuttle != null ? null : "BSL_ShuttleUnavailable".Translate())
            };

            AddReturnOptions(options, shuttle, caravan);
            options.Add(CreateOption(
                "BSL_LaunchToSettlement".Translate(),
                () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanSettlementLaunchFlow(caravan),
                "BSL_MenuLaunchToSettlementDesc".Translate(),
                PassengerShuttleLaunchQueueCommandUtility.CanQueueCaravanLaunchWhenReady(caravan, out string disabledReason)
                    && SettlementDestinationFinder.FindOtherPlayerHomeMapParents(null).Count > 0
                        ? null
                        : disabledReason.NullOrEmpty() ? "BSL_NoOtherSettlement".Translate() : disabledReason));
            return options;
        }

        private static void AddReturnOptions(List<FloatMenuOption> options, Building_PassengerShuttle shuttle, Caravan caravan)
        {
            if (PassengerShuttleLaunchQueueCommandUtility.TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location))
            {
                string mapLabel = location.MapParent.LabelCap;
                options.Add(CreateWorldOption(
                    "BSL_MenuReturnChooseLanding".Translate(mapLabel),
                    caravan == null
                        ? (Action)(() => PassengerShuttleLaunchQueueCommandUtility.StartReturnWithLandingSelection(shuttle, location.MapParent))
                        : () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanReturnWithLandingSelection(caravan, location.MapParent),
                    "BSL_ReturnWithLandingSelectionTooltip".Translate(mapLabel),
                    location.MapParent,
                    null));
                options.Add(CreateWorldOption(
                    "BSL_MenuReturnLastCell".Translate(mapLabel),
                    caravan == null
                        ? (Action)(() => PassengerShuttleLaunchQueueCommandUtility.StartReturnToLastDepartureCell(shuttle, location))
                        : () => PassengerShuttleLaunchQueueCommandUtility.StartCaravanReturnToLastDepartureCell(caravan, location),
                    "BSL_ReturnToLastDepartureCellTooltip".Translate(mapLabel),
                    location.MapParent,
                    null));
                return;
            }

            string unavailableLabel = "BSL_StatusUnavailable".Translate();
            options.Add(CreateOption(
                "BSL_MenuReturnChooseLanding".Translate(unavailableLabel),
                null,
                "BSL_ReturnWithLandingSelectionTooltip".Translate(unavailableLabel),
                "BSL_LastDepartureCellUnavailable".Translate()));
            options.Add(CreateOption(
                "BSL_MenuReturnLastCell".Translate(unavailableLabel),
                null,
                "BSL_ReturnToLastDepartureCellTooltip".Translate(unavailableLabel),
                "BSL_LastDepartureCellUnavailable".Translate()));
        }

        private static FloatMenuOption CreateWorldOption(string label, Action action, string tooltip, WorldObject worldObject, string disabledReason)
        {
            return new FloatMenuOption(
                label,
                disabledReason.NullOrEmpty() ? action : null,
                MenuOptionPriority.Default,
                _ =>
                {
                    if (worldObject != null && !worldObject.Destroyed)
                    {
                        TargetHighlighter.Highlight(new GlobalTargetInfo(worldObject), true, true, true);
                    }
                },
                null,
                0f,
                null,
                worldObject)
            {
                tooltip = BuildTooltip(tooltip, disabledReason)
            };
        }

        private static FloatMenuOption CreateOption(string label, Action action, string tooltip, string disabledReason)
        {
            return new FloatMenuOption(label, disabledReason.NullOrEmpty() ? action : null)
            {
                tooltip = BuildTooltip(tooltip, disabledReason)
            };
        }

        private static TipSignal? BuildTooltip(string tooltip, string disabledReason)
        {
            string finalTooltip = disabledReason.NullOrEmpty()
                ? tooltip
                : "BSL_DisabledReasonTooltip".Translate(tooltip, disabledReason);
            return finalTooltip.NullOrEmpty() ? (TipSignal?)null : new TipSignal(finalTooltip);
        }
    }
}
