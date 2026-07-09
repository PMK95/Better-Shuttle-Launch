using System;
using System.Collections.Generic;
using BetterShuttleLaunch.Domain;
using BetterShuttleLaunch.RimWorldApi;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Services
{
    public static class ShuttleLaunchService
    {
        public static bool HasQueuedLaunch(ShuttleContext context)
        {
            return ShuttleLaunchQueueGameComponent.Current?.HasQueuedLaunch(context) == true;
        }

        public static void StartReadyLaunch(ShuttleContext context)
        {
            if (RejectNewLaunchWhenQueued(context))
            {
                return;
            }

            RunAfterLoadingIfNeeded(context, currentContext =>
            {
                PassengerShuttleLaunchApi.TryStartQueuedWorldTargeting(
                    currentContext,
                    (destinationTile, destinationLabel, arrivalAction) => QueueLaunch(currentContext, destinationTile, destinationLabel, arrivalAction));
            });
        }

        public static void StartLaunchToSettlement(ShuttleContext context)
        {
            if (RejectNewLaunchWhenQueued(context))
            {
                return;
            }

            List<MapParent> destinations = SettlementDestinationService.FindOtherPlayerHomeMapParents(context?.MapParent);
            if (destinations.Count == 0)
            {
                Messages.Message("BSL_NoOtherSettlement".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < destinations.Count; i++)
            {
                MapParent destination = destinations[i];
                options.Add(CreateWorldTargetMenuOption(
                    destination.LabelCap,
                    () => StartLaunchToSpecificSettlement(context, destination),
                    "BSL_SelectSettlementDestinationTooltip".Translate(destination.LabelCap),
                    destination));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public static void StartReturnWithLandingSelection(ShuttleContext context)
        {
            if (RejectNewLaunchWhenQueued(context))
            {
                return;
            }

            if (!TryGetLastDepartureRecord(context, out LastDepartureRecord record))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            RunAfterLoadingIfNeeded(context, currentContext =>
            {
                PassengerShuttleLaunchApi.TryStartQueuedLandingCellTargeter(
                    currentContext,
                    record.MapParent,
                    (tile, action) => QueueLaunch(currentContext, tile, record.MapParent.LabelCap, action));
            });
        }

        public static void StartReturnToLastDepartureCell(ShuttleContext context)
        {
            if (RejectNewLaunchWhenQueued(context))
            {
                return;
            }

            if (!TryGetLastDepartureRecord(context, out LastDepartureRecord record))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            RunAfterLoadingIfNeeded(context, currentContext =>
            {
                if (!PassengerShuttleLaunchApi.TryCreateQueuedSpecificLandingAction(
                        currentContext,
                        record.MapParent,
                        record.Cell,
                        record.Rotation,
                        out PlanetTile destinationTile,
                        out TransportersArrivalAction arrivalAction,
                        out string failReason))
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                QueueLaunch(currentContext, destinationTile, record.MapParent.LabelCap, arrivalAction);
            });
        }

        public static void CancelQueuedLaunch(ShuttleContext context)
        {
            if (!HasQueuedLaunch(context))
            {
                Messages.Message("BSL_NoQueuedLaunch".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            ShuttleLaunchQueueGameComponent.Current?.CancelQueuedLaunch(context, true);
        }

        public static bool CanStartLaunchToSettlement(ShuttleContext context)
        {
            return context != null
                   && !HasQueuedLaunch(context)
                   && SettlementDestinationService.FindOtherPlayerHomeMapParents(context.MapParent).Count > 0;
        }

        public static bool CanStartReturn(ShuttleContext context)
        {
            return context != null && !HasQueuedLaunch(context) && TryGetLastDepartureRecord(context, out _);
        }

        public static bool TryGetLastDepartureRecord(ShuttleContext context, out LastDepartureRecord record)
        {
            record = null;
            return context?.Shuttle != null
                   && ShuttleLaunchQueueGameComponent.Current != null
                   && ShuttleLaunchQueueGameComponent.Current.TryGetLastDepartureRecord(context.Shuttle, out record);
        }

        public static string GetOriginLabel(ShuttleContext context)
        {
            if (context == null)
            {
                return "BSL_StatusUnavailable".Translate();
            }

            if (context.IsCaravan)
            {
                return context.Caravan.LabelCap;
            }

            return context.MapParent?.LabelCap ?? context.OriginTile.ToString();
        }

        private static void StartLaunchToSpecificSettlement(ShuttleContext context, MapParent destination)
        {
            if (RejectNewLaunchWhenQueued(context))
            {
                return;
            }

            if (destination == null)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            RunAfterLoadingIfNeeded(context, currentContext =>
            {
                PassengerShuttleLaunchApi.TryStartQueuedLandingCellTargeter(
                    currentContext,
                    destination,
                    (tile, action) => QueueLaunch(currentContext, tile, destination.LabelCap, action));
            });
        }

        private static void QueueLaunch(
            ShuttleContext context,
            PlanetTile destinationTile,
            string destinationLabel,
            TransportersArrivalAction arrivalAction)
        {
            if (RejectNewLaunchWhenQueued(context))
            {
                return;
            }

            if (arrivalAction == null || !destinationTile.Valid)
            {
                PassengerShuttleLaunchApi.StopWorldTargeting();
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            ShuttleLaunchQueueGameComponent.Current?.TryAddQueuedLaunch(new QueuedShuttleLaunch(
                context,
                destinationTile,
                GetOriginLabel(context),
                destinationLabel,
                arrivalAction));
        }

        private static void RunAfterLoadingIfNeeded(ShuttleContext context, Action<ShuttleContext> action)
        {
            if (context == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (context.IsCaravan)
            {
                action(context);
                return;
            }

            PassengerShuttleLaunchApi.OpenLoadDialogThenRun(context, pawnsExpectedAfterLoading =>
            {
                if (!ShuttleContext.TryForMapShuttle(context.Shuttle, out ShuttleContext currentContext, out string failReason))
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                action(currentContext.WithPawnsExpectedAfterLoading(pawnsExpectedAfterLoading));
            });
        }

        private static bool RejectNewLaunchWhenQueued(ShuttleContext context)
        {
            if (!HasQueuedLaunch(context))
            {
                return false;
            }

            Messages.Message("BSL_QueuedLaunchAlreadyExists".Translate(), context?.Shuttle, MessageTypeDefOf.RejectInput, false);
            return true;
        }

        private static FloatMenuOption CreateWorldTargetMenuOption(string label, Action action, string tooltip, WorldObject worldObject)
        {
            return new FloatMenuOption(
                label,
                action,
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
                tooltip = tooltip.NullOrEmpty() ? (TipSignal?)null : new TipSignal(tooltip)
            };
        }
    }
}
