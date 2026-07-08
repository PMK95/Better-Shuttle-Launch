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
            Command_PassengerShuttleLaunchMenu command = new Command_PassengerShuttleLaunchMenu(
                () => PassengerShuttleTrackerActionMenu.CreateForMapShuttle(shuttle))
            {
                defaultLabel = "BSL_LaunchWhenReady".Translate(),
                defaultDesc = "BSL_LaunchWhenReadyDesc".Translate(),
                icon = BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandLaunchWhenReady, CompLaunchable.LaunchCommandTex)
            };

            return command;
        }

        public static Gizmo CreateForMapParent(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> shuttleList = new List<Building_PassengerShuttle>(shuttles);
            Command_PassengerShuttleLaunchMenu command = new Command_PassengerShuttleLaunchMenu(
                () => CreateMapShuttleMenuOptions(shuttleList))
            {
                defaultLabel = "BSL_LaunchWhenReady".Translate(),
                defaultDesc = "BSL_LaunchWhenReadyDesc".Translate(),
                icon = BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandLaunchWhenReady, CompLaunchable.LaunchCommandTex)
            };

            if (shuttleList.Count == 0)
            {
                command.Disable("BSL_NoAvailableShuttles".Translate());
            }

            return command;
        }

        public static Gizmo CreateForCaravan(Caravan caravan)
        {
            Command_PassengerShuttleLaunchMenu command = new Command_PassengerShuttleLaunchMenu(
                () => PassengerShuttleTrackerActionMenu.CreateForCaravan(caravan))
            {
                defaultLabel = "BSL_LaunchWhenReady".Translate(),
                defaultDesc = "BSL_LaunchWhenReadyDesc".Translate(),
                icon = BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandLaunchWhenReady, CompLaunchable.LaunchCommandTex)
            };

            return command;
        }

        private static List<FloatMenuOption> CreateMapShuttleMenuOptions(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 1)
            {
                return PassengerShuttleTrackerActionMenu.CreateForMapShuttle(shuttles[0]);
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_MenuReadyLaunch".Translate(),
                "BSL_ReadyShortcutTooltip".Translate(),
                () => StartQueuedMapLaunch(shuttles),
                shuttles.Count > 0 ? null : "BSL_NoShuttles".Translate()));
            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_MenuReturnChooseLanding".Translate(GetReturnDestinationLabel(shuttles)),
                "BSL_ReturnWithLandingSelectionTooltip".Translate(GetReturnDestinationLabel(shuttles)),
                () => StartReturnWithLandingSelection(shuttles),
                GetReturnLaunchDisabledReason(shuttles)));
            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_MenuReturnLastCell".Translate(GetReturnDestinationLabel(shuttles)),
                "BSL_ReturnToLastDepartureCellTooltip".Translate(GetReturnDestinationLabel(shuttles)),
                () => StartReturnToLastDepartureCell(shuttles),
                GetReturnLaunchDisabledReason(shuttles)));
            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_LaunchToSettlement".Translate(),
                "BSL_MenuLaunchToSettlementDesc".Translate(),
                () => StartSettlementLaunch(shuttles),
                GetSettlementLaunchDisabledReason(shuttles)));
            return options;
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

        private static void StartReturnWithLandingSelection(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                StartReturnWithLandingSelection(shuttles[0]);
                return;
            }

            SelectMapShuttle(FindReturnTargetShuttles(shuttles), StartReturnWithLandingSelection);
        }

        private static void StartReturnToLastDepartureCell(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                StartReturnToLastDepartureCell(shuttles[0]);
                return;
            }

            SelectMapShuttle(FindReturnTargetShuttles(shuttles), StartReturnToLastDepartureCell);
        }

        private static void StartReturnWithLandingSelection(Building_PassengerShuttle shuttle)
        {
            if (!PassengerShuttleLaunchQueueCommandUtility.TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            PassengerShuttleLaunchQueueCommandUtility.StartReturnWithLandingSelection(shuttle, location.MapParent);
        }

        private static void StartReturnToLastDepartureCell(Building_PassengerShuttle shuttle)
        {
            if (!PassengerShuttleLaunchQueueCommandUtility.TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            PassengerShuttleLaunchQueueCommandUtility.StartReturnToLastDepartureCell(shuttle, location);
        }

        private static void SelectMapShuttle(IReadOnlyList<Building_PassengerShuttle> shuttles, Action<Building_PassengerShuttle> selectAction)
        {
            Find.WindowStack.Add(new Dialog_SelectPassengerShuttle(shuttles, selectAction, _ => null));
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

        private static string GetReturnDestinationLabel(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (FindReturnTargetShuttles(shuttles).Count == 1
                && PassengerShuttleLaunchQueueCommandUtility.TryGetLastDepartureLocation(FindReturnTargetShuttles(shuttles)[0], out LastDepartureLocation location))
            {
                return location.MapParent.LabelCap;
            }

            return "BSL_SelectShuttle".Translate();
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
