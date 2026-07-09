using System;
using System.Collections.Generic;
using BetterShuttleLaunch.Domain;
using BetterShuttleLaunch.Services;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public static class ShuttleLaunchCommandFactory
    {
        public static Gizmo CreateForMapShuttle(Building_PassengerShuttle shuttle)
        {
            if (!ShuttleContext.TryForMapShuttle(shuttle, out ShuttleContext context, out string failReason))
            {
                return CreateDisabledCommand(failReason);
            }

            return CreateForSingleContext(context);
        }

        public static Gizmo CreateForCaravan(Caravan caravan)
        {
            if (!ShuttleContext.TryForCaravan(caravan, out ShuttleContext context, out string failReason))
            {
                return CreateDisabledCommand(failReason);
            }

            return CreateForSingleContext(context);
        }

        public static Gizmo CreateForMapParent(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<ShuttleContext> contexts = CreateContexts(shuttles);
            if (contexts.Count == 0)
            {
                return CreateDisabledCommand("BSL_NoAvailableShuttles".Translate());
            }

            if (contexts.Count == 1)
            {
                return CreateForSingleContext(contexts[0]);
            }

            bool allQueued = FindUnqueuedContexts(contexts).Count == 0;
            Command_ShuttleLaunch command = new Command_ShuttleLaunch(() => CreateOptionsForContexts(contexts))
            {
                defaultLabel = allQueued ? "BSL_CancelQueuedLaunch".Translate() : "BSL_LaunchWhenReady".Translate(),
                defaultDesc = allQueued ? "BSL_CancelQueuedLaunchDesc".Translate() : "BSL_LaunchWhenReadyDesc".Translate(),
                icon = allQueued
                    ? BetterShuttleLaunchTextures.GetCancelQueuedLaunchIcon()
                    : BetterShuttleLaunchTextures.GetLaunchWhenReadyIcon()
            };
            return command;
        }

        private static Gizmo CreateForSingleContext(ShuttleContext context)
        {
            bool hasQueuedLaunch = ShuttleLaunchService.HasQueuedLaunch(context);
            Command_ShuttleLaunch command = new Command_ShuttleLaunch(() => CreateOptionsForContext(context))
            {
                defaultLabel = hasQueuedLaunch ? "BSL_CancelQueuedLaunch".Translate() : "BSL_LaunchWhenReady".Translate(),
                defaultDesc = hasQueuedLaunch ? "BSL_CancelQueuedLaunchDesc".Translate() : "BSL_LaunchWhenReadyDesc".Translate(),
                icon = hasQueuedLaunch
                    ? BetterShuttleLaunchTextures.GetCancelQueuedLaunchIcon()
                    : BetterShuttleLaunchTextures.GetLaunchWhenReadyIcon()
            };
            return command;
        }

        private static List<FloatMenuOption> CreateOptionsForContext(ShuttleContext context)
        {
            if (ShuttleLaunchService.HasQueuedLaunch(context))
            {
                return new List<FloatMenuOption>
                {
                    CreateOption(
                        "BSL_CancelQueuedLaunch".Translate(),
                        () => ShuttleLaunchService.CancelQueuedLaunch(context),
                        "BSL_CancelQueuedLaunchDesc".Translate(),
                        null)
                };
            }

            string returnLabel = GetReturnDestinationLabel(context);
            return new List<FloatMenuOption>
            {
                CreateOption(
                    "BSL_MenuReadyLaunch".Translate(),
                    () => ShuttleLaunchService.StartReadyLaunch(context),
                    "BSL_ReadyShortcutTooltip".Translate(),
                    null),
                CreateOption(
                    "BSL_MenuReturnChooseLanding".Translate(returnLabel),
                    () => ShuttleLaunchService.StartReturnWithLandingSelection(context),
                    "BSL_ReturnWithLandingSelectionTooltip".Translate(returnLabel),
                    ShuttleLaunchService.CanStartReturn(context) ? null : "BSL_LastDepartureCellUnavailable".Translate()),
                CreateOption(
                    "BSL_MenuReturnLastCell".Translate(returnLabel),
                    () => ShuttleLaunchService.StartReturnToLastDepartureCell(context),
                    "BSL_ReturnToLastDepartureCellTooltip".Translate(returnLabel),
                    ShuttleLaunchService.CanStartReturn(context) ? null : "BSL_LastDepartureCellUnavailable".Translate()),
                CreateOption(
                    "BSL_LaunchToSettlement".Translate(),
                    () => ShuttleLaunchService.StartLaunchToSettlement(context),
                    "BSL_MenuLaunchToSettlementDesc".Translate(),
                    ShuttleLaunchService.CanStartLaunchToSettlement(context) ? null : "BSL_NoOtherSettlement".Translate())
            };
        }

        private static List<FloatMenuOption> CreateOptionsForContexts(IReadOnlyList<ShuttleContext> contexts)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            List<ShuttleContext> queuedContexts = FindQueuedContexts(contexts);
            List<ShuttleContext> unqueuedContexts = FindUnqueuedContexts(contexts);
            if (queuedContexts.Count > 0)
            {
                options.Add(CreateOption(
                    "BSL_CancelQueuedLaunch".Translate(),
                    () => SelectContextAndRun(queuedContexts, ShuttleLaunchService.CancelQueuedLaunch),
                    "BSL_CancelQueuedLaunchDesc".Translate(),
                    null));
            }

            options.Add(CreateOption(
                "BSL_MenuReadyLaunch".Translate(),
                () => StartReadyLaunchForContexts(unqueuedContexts),
                "BSL_ReadyShortcutTooltip".Translate(),
                unqueuedContexts.Count > 0 ? null : "BSL_QueuedLaunchAlreadyExists".Translate()));
            options.Add(CreateOption(
                "BSL_MenuReturnChooseLanding".Translate(GetReturnDestinationLabel(contexts)),
                () => SelectContextAndRun(FindReturnContexts(contexts), ShuttleLaunchService.StartReturnWithLandingSelection),
                "BSL_ReturnWithLandingSelectionTooltip".Translate(GetReturnDestinationLabel(contexts)),
                FindReturnContexts(contexts).Count > 0 ? null : "BSL_LastDepartureCellUnavailable".Translate()));
            options.Add(CreateOption(
                "BSL_MenuReturnLastCell".Translate(GetReturnDestinationLabel(contexts)),
                () => SelectContextAndRun(FindReturnContexts(contexts), ShuttleLaunchService.StartReturnToLastDepartureCell),
                "BSL_ReturnToLastDepartureCellTooltip".Translate(GetReturnDestinationLabel(contexts)),
                FindReturnContexts(contexts).Count > 0 ? null : "BSL_LastDepartureCellUnavailable".Translate()));
            options.Add(CreateOption(
                "BSL_LaunchToSettlement".Translate(),
                () => SelectContextAndRun(FindSettlementContexts(contexts), ShuttleLaunchService.StartLaunchToSettlement),
                "BSL_MenuLaunchToSettlementDesc".Translate(),
                FindSettlementContexts(contexts).Count > 0 ? null : "BSL_NoOtherSettlement".Translate()));
            return options;
        }

        private static void StartReadyLaunchForContexts(IReadOnlyList<ShuttleContext> contexts)
        {
            if (contexts.Count == 0)
            {
                Messages.Message("BSL_QueuedLaunchAlreadyExists".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            SelectContextAndRun(contexts, ShuttleLaunchService.StartReadyLaunch);
        }

        private static void SelectContextAndRun(IReadOnlyList<ShuttleContext> contexts, Action<ShuttleContext> action)
        {
            if (contexts.Count == 0)
            {
                Messages.Message("BSL_NoAvailableShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (contexts.Count == 1)
            {
                action(contexts[0]);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < contexts.Count; i++)
            {
                ShuttleContext context = contexts[i];
                options.Add(new FloatMenuOption(context.Label, () => action(context)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static FloatMenuOption CreateOption(string label, Action action, string tooltip, string disabledReason)
        {
            string finalTooltip = disabledReason.NullOrEmpty()
                ? tooltip
                : "BSL_DisabledReasonTooltip".Translate(tooltip, disabledReason);
            return new FloatMenuOption(label, disabledReason.NullOrEmpty() ? action : null)
            {
                tooltip = finalTooltip.NullOrEmpty() ? (TipSignal?)null : new TipSignal(finalTooltip)
            };
        }

        private static string GetReturnDestinationLabel(ShuttleContext context)
        {
            return ShuttleLaunchService.TryGetLastDepartureRecord(context, out LastDepartureRecord record)
                ? record.MapParent.LabelCap
                : "BSL_StatusUnavailable".Translate();
        }

        private static string GetReturnDestinationLabel(IReadOnlyList<ShuttleContext> contexts)
        {
            List<ShuttleContext> returnContexts = FindReturnContexts(contexts);
            return returnContexts.Count == 1 && ShuttleLaunchService.TryGetLastDepartureRecord(returnContexts[0], out LastDepartureRecord record)
                ? record.MapParent.LabelCap
                : "BSL_SelectShuttle".Translate();
        }

        private static List<ShuttleContext> CreateContexts(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            List<ShuttleContext> contexts = new List<ShuttleContext>();
            for (int i = 0; i < shuttles.Count; i++)
            {
                if (ShuttleContext.TryForMapShuttle(shuttles[i], out ShuttleContext context, out _))
                {
                    contexts.Add(context);
                }
            }

            return contexts;
        }

        private static List<ShuttleContext> FindQueuedContexts(IReadOnlyList<ShuttleContext> contexts)
        {
            List<ShuttleContext> result = new List<ShuttleContext>();
            for (int i = 0; i < contexts.Count; i++)
            {
                if (ShuttleLaunchService.HasQueuedLaunch(contexts[i]))
                {
                    result.Add(contexts[i]);
                }
            }

            return result;
        }

        private static List<ShuttleContext> FindUnqueuedContexts(IReadOnlyList<ShuttleContext> contexts)
        {
            List<ShuttleContext> result = new List<ShuttleContext>();
            for (int i = 0; i < contexts.Count; i++)
            {
                if (!ShuttleLaunchService.HasQueuedLaunch(contexts[i]))
                {
                    result.Add(contexts[i]);
                }
            }

            return result;
        }

        private static List<ShuttleContext> FindReturnContexts(IReadOnlyList<ShuttleContext> contexts)
        {
            List<ShuttleContext> result = new List<ShuttleContext>();
            for (int i = 0; i < contexts.Count; i++)
            {
                if (ShuttleLaunchService.CanStartReturn(contexts[i]))
                {
                    result.Add(contexts[i]);
                }
            }

            return result;
        }

        private static List<ShuttleContext> FindSettlementContexts(IReadOnlyList<ShuttleContext> contexts)
        {
            List<ShuttleContext> result = new List<ShuttleContext>();
            for (int i = 0; i < contexts.Count; i++)
            {
                if (ShuttleLaunchService.CanStartLaunchToSettlement(contexts[i]))
                {
                    result.Add(contexts[i]);
                }
            }

            return result;
        }

        private static Gizmo CreateDisabledCommand(string disabledReason)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "BSL_LaunchWhenReady".Translate(),
                defaultDesc = "BSL_LaunchWhenReadyDesc".Translate(),
                icon = BetterShuttleLaunchTextures.GetLaunchWhenReadyIcon()
            };
            command.Disable(disabledReason);
            return command;
        }
    }
}
