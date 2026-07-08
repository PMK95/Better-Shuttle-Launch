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
            if (PassengerShuttleLaunchQueueCommandUtility.HasQueuedLaunch(shuttle))
            {
                return CreateCancelQueuedMapShuttleCommand(shuttle);
            }

            Command_PassengerShuttleLaunchMenu command = new Command_PassengerShuttleLaunchMenu(
                () => PassengerShuttleLaunchActionMenu.CreateForMapShuttle(shuttle))
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
            if (shuttleList.Count == 1 && PassengerShuttleLaunchQueueCommandUtility.HasQueuedLaunch(shuttleList[0]))
            {
                return CreateCancelQueuedMapShuttleCommand(shuttleList[0]);
            }

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
            if (PassengerShuttleLaunchQueueCommandUtility.HasQueuedLaunch(caravan))
            {
                return CreateCancelQueuedCaravanCommand(caravan);
            }

            Command_PassengerShuttleLaunchMenu command = new Command_PassengerShuttleLaunchMenu(
                () => PassengerShuttleLaunchActionMenu.CreateForCaravan(caravan))
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
                return PassengerShuttleLaunchActionMenu.CreateForMapShuttle(shuttles[0]);
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            List<Building_PassengerShuttle> queuedShuttles = FindQueuedShuttles(shuttles);
            if (queuedShuttles.Count > 0)
            {
                options.Add(CreateMenuOptionWithTooltip(
                    "BSL_CancelQueuedLaunch".Translate(),
                    () => CancelQueuedMapLaunch(queuedShuttles),
                    "BSL_CancelQueuedLaunchDesc".Translate()));
            }

            options.Add(CreateConditionalOptionWithTooltip(
                "BSL_MenuReadyLaunch".Translate(),
                "BSL_ReadyShortcutTooltip".Translate(),
                () => StartQueuedMapLaunch(shuttles),
                GetReadyLaunchDisabledReason(shuttles)));
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
            List<Building_PassengerShuttle> availableShuttles = FindUnqueuedShuttles(shuttles);
            if (availableShuttles.Count == 0)
            {
                string failReason = shuttles.Count == 0 ? "BSL_NoShuttles".Translate() : "BSL_QueuedLaunchAlreadyExists".Translate();
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (availableShuttles.Count == 1)
            {
                PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow(availableShuttles[0]);
                return;
            }

            SelectMapShuttle(availableShuttles, PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow);
        }

        private static void StartSettlementLaunch(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> availableShuttles = FindSettlementLaunchTargetShuttles(shuttles);
            if (availableShuttles.Count == 0)
            {
                string failReason = shuttles.Count == 0 ? "BSL_NoShuttles".Translate() : GetSettlementLaunchDisabledReason(shuttles);
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (availableShuttles.Count == 1)
            {
                PassengerShuttleLaunchQueueCommandUtility.StartSettlementLaunchFlow(availableShuttles[0]);
                return;
            }

            SelectMapShuttle(availableShuttles, PassengerShuttleLaunchQueueCommandUtility.StartSettlementLaunchFlow);
        }

        private static void StartReturnWithLandingSelection(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> availableShuttles = FindReturnTargetShuttles(shuttles);
            if (availableShuttles.Count == 0)
            {
                string failReason = shuttles.Count == 0 ? "BSL_NoShuttles".Translate() : GetReturnLaunchDisabledReason(shuttles);
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (availableShuttles.Count == 1)
            {
                StartReturnWithLandingSelection(availableShuttles[0]);
                return;
            }

            SelectMapShuttle(availableShuttles, StartReturnWithLandingSelection);
        }

        private static void StartReturnToLastDepartureCell(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> availableShuttles = FindReturnTargetShuttles(shuttles);
            if (availableShuttles.Count == 0)
            {
                string failReason = shuttles.Count == 0 ? "BSL_NoShuttles".Translate() : GetReturnLaunchDisabledReason(shuttles);
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (availableShuttles.Count == 1)
            {
                StartReturnToLastDepartureCell(availableShuttles[0]);
                return;
            }

            SelectMapShuttle(availableShuttles, StartReturnToLastDepartureCell);
        }

        private static void CancelQueuedMapLaunch(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoQueuedLaunch".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                PassengerShuttleLaunchQueueCommandUtility.CancelQueuedLaunch(shuttles[0]);
                return;
            }

            SelectMapShuttle(shuttles, PassengerShuttleLaunchQueueCommandUtility.CancelQueuedLaunch);
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

            if (FindUnqueuedShuttles(shuttles).Count == 0)
            {
                return "BSL_QueuedLaunchAlreadyExists".Translate();
            }

            return "BSL_NoOtherSettlement".Translate();
        }

        private static string GetReturnLaunchDisabledReason(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                return "BSL_NoShuttles".Translate();
            }

            if (FindReturnTargetShuttles(shuttles).Count > 0)
            {
                return null;
            }

            return FindUnqueuedShuttles(shuttles).Count == 0
                ? "BSL_QueuedLaunchAlreadyExists".Translate()
                : "BSL_LastDepartureCellUnavailable".Translate();
        }

        private static string GetReadyLaunchDisabledReason(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                return "BSL_NoShuttles".Translate();
            }

            return FindUnqueuedShuttles(shuttles).Count > 0 ? null : "BSL_QueuedLaunchAlreadyExists".Translate();
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

        private static List<Building_PassengerShuttle> FindUnqueuedShuttles(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> availableShuttles = new List<Building_PassengerShuttle>();
            for (int i = 0; i < shuttles.Count; i++)
            {
                if (!PassengerShuttleLaunchQueueCommandUtility.HasQueuedLaunch(shuttles[i]))
                {
                    availableShuttles.Add(shuttles[i]);
                }
            }

            return availableShuttles;
        }

        private static List<Building_PassengerShuttle> FindQueuedShuttles(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<Building_PassengerShuttle> queuedShuttles = new List<Building_PassengerShuttle>();
            for (int i = 0; i < shuttles.Count; i++)
            {
                if (PassengerShuttleLaunchQueueCommandUtility.HasQueuedLaunch(shuttles[i]))
                {
                    queuedShuttles.Add(shuttles[i]);
                }
            }

            return queuedShuttles;
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

        private static Gizmo CreateCancelQueuedMapShuttleCommand(Building_PassengerShuttle shuttle)
        {
            return new Command_Action
            {
                defaultLabel = "BSL_CancelQueuedLaunch".Translate(),
                defaultDesc = "BSL_CancelQueuedLaunchDesc".Translate(),
                icon = BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandCancelLaunch, CompLaunchable.LaunchCommandTex),
                action = () => PassengerShuttleLaunchQueueCommandUtility.CancelQueuedLaunch(shuttle)
            };
        }

        private static Gizmo CreateCancelQueuedCaravanCommand(Caravan caravan)
        {
            return new Command_Action
            {
                defaultLabel = "BSL_CancelQueuedLaunch".Translate(),
                defaultDesc = "BSL_CancelQueuedLaunchDesc".Translate(),
                icon = BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandCancelLaunch, CompLaunchable.LaunchCommandTex),
                action = () => PassengerShuttleLaunchQueueCommandUtility.CancelQueuedLaunch(caravan)
            };
        }
    }
}
