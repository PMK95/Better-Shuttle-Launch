using System;
using System.Collections.Generic;
using BetterShuttleLaunch.Shuttles;
using BetterShuttleLaunch.ReturnHome;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public static class PassengerShuttleLaunchQueueCommandUtility
    {
        public static bool CanQueueLaunchWhenReady(Building_PassengerShuttle shuttle, out string disabledReason)
        {
            return PassengerShuttleLaunchBridge.TryGetLaunchParts(shuttle, out _, out _, out disabledReason);
        }

        public static bool HasQueuedLaunch(Building_PassengerShuttle shuttle)
        {
            return LaunchQueueGameComponent.Current?.IsQueued(shuttle) == true;
        }

        public static bool HasQueuedLaunch(Caravan caravan)
        {
            return LaunchQueueGameComponent.Current?.IsQueued(caravan) == true;
        }

        public static void CancelQueuedLaunch(Building_PassengerShuttle shuttle)
        {
            if (!HasQueuedLaunch(shuttle))
            {
                Messages.Message("BSL_NoQueuedLaunch".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            LaunchQueueGameComponent.Current?.RemoveQueuedLaunch(shuttle, true);
        }

        public static void CancelQueuedLaunch(Caravan caravan)
        {
            if (!HasQueuedLaunch(caravan))
            {
                Messages.Message("BSL_NoQueuedLaunch".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            LaunchQueueGameComponent.Current?.RemoveQueuedLaunch(caravan, true);
        }

        public static bool CanQueueCaravanLaunchWhenReady(Caravan caravan, out string disabledReason)
        {
            disabledReason = null;
            if (caravan == null || caravan.Faction != Faction.OfPlayer || caravan.Shuttle == null || caravan.Shuttle.LaunchableComp == null)
            {
                disabledReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            return true;
        }

        public static void StartLaunchWhenReadyFlow(Building_PassengerShuttle shuttle)
        {
            if (RejectNewMapQueuedLaunchWhenAlreadyQueued(shuttle, false))
            {
                return;
            }

            PassengerShuttleLaunchBridge.OpenLoadDialogThenRunAction(shuttle, () =>
            {
                PassengerShuttleLaunchBridge.StartChoosingQueuedDestination(shuttle, (destinationTile, destinationLabel, arrivalAction) =>
                {
                    QueueMapShuttleLaunch(shuttle, destinationTile, GetOriginLabel(shuttle), destinationLabel, arrivalAction);
                });
            });
        }

        public static void StartCaravanLaunchWhenReadyFlow(Caravan caravan)
        {
            if (RejectNewCaravanQueuedLaunchWhenAlreadyQueued(caravan, false))
            {
                return;
            }

            if (!CanQueueCaravanLaunchWhenReady(caravan, out string disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Building_PassengerShuttle shuttle = caravan.Shuttle;
            PassengerShuttleLaunchBridge.StartChoosingQueuedDestinationFromCaravan(caravan, (destinationTile, destinationLabel, arrivalAction) =>
            {
                QueueCaravanLaunch(caravan, shuttle, destinationTile, GetOriginLabel(caravan), destinationLabel, arrivalAction);
            });
        }

        public static void StartSettlementLaunchFlow(Building_PassengerShuttle shuttle)
        {
            if (RejectNewMapQueuedLaunchWhenAlreadyQueued(shuttle, false))
            {
                return;
            }

            if (!CanStartSettlementLaunchFlow(shuttle))
            {
                Messages.Message("BSL_NoOtherSettlement".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            IReadOnlyList<MapParent> destinations = SettlementDestinationFinder.FindOtherPlayerHomeMapParents(shuttle.Map?.Parent);
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < destinations.Count; i++)
            {
                MapParent destination = destinations[i];
                options.Add(CreateWorldTargetMenuOption(
                    destination.LabelCap,
                    () => StartSettlementLaunchToSelectedSettlement(shuttle, destination),
                    "BSL_SelectSettlementDestinationTooltip".Translate(destination.LabelCap),
                    destination));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public static void StartCaravanSettlementLaunchFlow(Caravan caravan)
        {
            if (RejectNewCaravanQueuedLaunchWhenAlreadyQueued(caravan, false))
            {
                return;
            }

            if (!CanQueueCaravanLaunchWhenReady(caravan, out string disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            IReadOnlyList<MapParent> destinations = SettlementDestinationFinder.FindOtherPlayerHomeMapParents(null);
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
                    () => StartCaravanSettlementLaunchToSelectedSettlement(caravan, destination),
                    "BSL_SelectSettlementDestinationTooltip".Translate(destination.LabelCap),
                    destination));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public static void StartReturnFlow(Building_PassengerShuttle shuttle)
        {
            if (RejectNewMapQueuedLaunchWhenAlreadyQueued(shuttle, false))
            {
                return;
            }

            if (!TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                CreateWorldTargetMenuOption(
                    "BSL_ReturnWithLandingSelection".Translate(),
                    () => StartReturnWithLandingSelection(shuttle, location.MapParent),
                    "BSL_ReturnWithLandingSelectionTooltip".Translate(location.MapParent.LabelCap),
                    location.MapParent),
                CreateWorldTargetMenuOption(
                    "BSL_ReturnToLastDepartureCell".Translate(),
                    () => StartReturnToLastDepartureCell(shuttle, location),
                    "BSL_ReturnToLastDepartureCellTooltip".Translate(location.MapParent.LabelCap),
                    location.MapParent)
            }));
        }

        public static void StartCaravanReturnFlow(Caravan caravan)
        {
            if (RejectNewCaravanQueuedLaunchWhenAlreadyQueued(caravan, false))
            {
                return;
            }

            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            if (!TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                CreateWorldTargetMenuOption(
                    "BSL_ReturnWithLandingSelection".Translate(),
                    () => StartCaravanReturnWithLandingSelection(caravan, location.MapParent),
                    "BSL_ReturnWithLandingSelectionTooltip".Translate(location.MapParent.LabelCap),
                    location.MapParent),
                CreateWorldTargetMenuOption(
                    "BSL_ReturnToLastDepartureCell".Translate(),
                    () => StartCaravanReturnToLastDepartureCell(caravan, location),
                    "BSL_ReturnToLastDepartureCellTooltip".Translate(location.MapParent.LabelCap),
                    location.MapParent)
            }));
        }

        public static bool CanStartSettlementLaunchFlow(Building_PassengerShuttle shuttle)
        {
            return CanQueueLaunchWhenReady(shuttle, out _)
                   && !HasQueuedLaunch(shuttle)
                   && SettlementDestinationFinder.FindOtherPlayerHomeMapParents(shuttle.Map?.Parent).Count > 0;
        }

        public static bool CanStartReturnFlow(Building_PassengerShuttle shuttle)
        {
            return !HasQueuedLaunch(shuttle) && TryGetLastDepartureLocation(shuttle, out _);
        }

        public static void QueueMapShuttleLaunch(
            Building_PassengerShuttle shuttle,
            PlanetTile destinationTile,
            string originLabel,
            string destinationLabel,
            TransportersArrivalAction arrivalAction)
        {
            if (RejectNewMapQueuedLaunchWhenAlreadyQueued(shuttle, true))
            {
                return;
            }

            if (arrivalAction == null || !destinationTile.Valid)
            {
                PassengerShuttleLaunchBridge.StopWorldTargeting();
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            PlanetTile originTile = shuttle?.Tile ?? default;
            LaunchQueueGameComponent.Current?.AddQueuedLaunchIfNoExistingQueue(new QueuedPassengerShuttleLaunch(
                shuttle,
                originTile,
                destinationTile,
                originLabel,
                destinationLabel,
                arrivalAction));
        }

        public static void QueueCaravanLaunch(
            Caravan caravan,
            Building_PassengerShuttle shuttle,
            PlanetTile destinationTile,
            string originLabel,
            string destinationLabel,
            TransportersArrivalAction arrivalAction)
        {
            if (RejectNewCaravanQueuedLaunchWhenAlreadyQueued(caravan, true))
            {
                return;
            }

            if (arrivalAction == null || !destinationTile.Valid)
            {
                PassengerShuttleLaunchBridge.StopWorldTargeting();
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            LaunchQueueGameComponent.Current?.AddQueuedLaunchIfNoExistingQueue(new QueuedPassengerShuttleLaunch(
                caravan,
                shuttle,
                caravan?.Tile ?? default,
                destinationTile,
                originLabel,
                destinationLabel,
                arrivalAction));
        }

        private static void StartSettlementLaunchToSelectedSettlement(Building_PassengerShuttle shuttle, MapParent destination)
        {
            if (RejectNewMapQueuedLaunchWhenAlreadyQueued(shuttle, false))
            {
                return;
            }

            PassengerShuttleLaunchBridge.OpenLoadDialogThenRunAction(shuttle, () =>
            {
                if (destination == null)
                {
                    Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }

                PassengerShuttleLaunchBridge.StartChoosingSpecificLandingCellForQueuedLaunch(
                    shuttle,
                    destination,
                    (tile, action) => QueueMapShuttleLaunch(shuttle, tile, GetOriginLabel(shuttle), destination.LabelCap, action));
            });
        }

        private static void StartCaravanSettlementLaunchToSelectedSettlement(Caravan caravan, MapParent destination)
        {
            if (RejectNewCaravanQueuedLaunchWhenAlreadyQueued(caravan, false))
            {
                return;
            }

            if (destination == null)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            PassengerShuttleLaunchBridge.StartChoosingSpecificLandingCellFromCaravanForQueuedLaunch(
                caravan,
                destination,
                (tile, action) => QueueCaravanLaunch(caravan, shuttle, tile, GetOriginLabel(caravan), destination.LabelCap, action));
        }

        public static void StartReturnWithLandingSelection(Building_PassengerShuttle shuttle, MapParent destination)
        {
            if (RejectNewMapQueuedLaunchWhenAlreadyQueued(shuttle, false))
            {
                return;
            }

            if (destination == null)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            PassengerShuttleLaunchBridge.OpenLoadDialogThenRunAction(shuttle, () =>
            {
                PassengerShuttleLaunchBridge.StartChoosingSpecificLandingCellForQueuedLaunch(
                    shuttle,
                    destination,
                    (tile, action) => QueueMapShuttleLaunch(shuttle, tile, GetOriginLabel(shuttle), destination.LabelCap, action));
            });
        }

        public static void StartCaravanReturnWithLandingSelection(Caravan caravan, MapParent destination)
        {
            if (RejectNewCaravanQueuedLaunchWhenAlreadyQueued(caravan, false))
            {
                return;
            }

            if (destination == null)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            PassengerShuttleLaunchBridge.StartChoosingSpecificLandingCellFromCaravanForQueuedLaunch(
                caravan,
                destination,
                (tile, action) => QueueCaravanLaunch(caravan, shuttle, tile, GetOriginLabel(caravan), destination.LabelCap, action));
        }

        public static void StartReturnToLastDepartureCell(Building_PassengerShuttle shuttle, LastDepartureLocation location)
        {
            if (RejectNewMapQueuedLaunchWhenAlreadyQueued(shuttle, false))
            {
                return;
            }

            PassengerShuttleLaunchBridge.OpenLoadDialogThenRunAction(shuttle, () =>
            {
                if (!PassengerShuttleLaunchBridge.TryCreateSpecificLandingActionForQueuedLaunch(
                        shuttle,
                        location.MapParent,
                        location.Cell,
                        location.Rotation,
                        out PlanetTile tile,
                        out TransportersArrivalAction action,
                        out string failReason))
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                QueueMapShuttleLaunch(shuttle, tile, GetOriginLabel(shuttle), location.MapParent.LabelCap, action);
            });
        }

        public static void StartCaravanReturnToLastDepartureCell(Caravan caravan, LastDepartureLocation location)
        {
            if (RejectNewCaravanQueuedLaunchWhenAlreadyQueued(caravan, false))
            {
                return;
            }

            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            if (!PassengerShuttleLaunchBridge.TryCreateSpecificLandingActionForCaravanQueuedLaunch(
                    caravan,
                    location.MapParent,
                    location.Cell,
                    location.Rotation,
                    out PlanetTile tile,
                    out TransportersArrivalAction action,
                    out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            QueueCaravanLaunch(caravan, shuttle, tile, GetOriginLabel(caravan), location.MapParent.LabelCap, action);
        }

        public static bool TryGetLastDepartureLocation(Building_PassengerShuttle shuttle, out LastDepartureLocation location)
        {
            location = default;
            return LaunchQueueGameComponent.Current != null
                   && LaunchQueueGameComponent.Current.TryGetLastDepartureLocation(shuttle, out location)
                   && location.MapParent != null
                   && location.MapParent.Spawned
                   && location.MapParent.HasMap;
        }

        private static string GetOriginLabel(Building_PassengerShuttle shuttle)
        {
            return shuttle?.Map?.Parent?.LabelCap ?? shuttle?.Tile.ToString() ?? "BSL_StatusUnavailable".Translate();
        }

        private static string GetOriginLabel(Caravan caravan)
        {
            return caravan?.LabelCap ?? caravan?.Tile.ToString() ?? "BSL_StatusUnavailable".Translate();
        }

        private static string GetDestinationLabel(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            return destinationTile.ToString();
        }

        private static bool RejectNewMapQueuedLaunchWhenAlreadyQueued(Building_PassengerShuttle shuttle, bool stopWorldTargeting)
        {
            if (!HasQueuedLaunch(shuttle))
            {
                return false;
            }

            if (stopWorldTargeting)
            {
                PassengerShuttleLaunchBridge.StopWorldTargeting();
            }

            Messages.Message("BSL_QueuedLaunchAlreadyExists".Translate(), shuttle, MessageTypeDefOf.RejectInput, false);
            return true;
        }

        private static bool RejectNewCaravanQueuedLaunchWhenAlreadyQueued(Caravan caravan, bool stopWorldTargeting)
        {
            if (!HasQueuedLaunch(caravan))
            {
                return false;
            }

            if (stopWorldTargeting)
            {
                PassengerShuttleLaunchBridge.StopWorldTargeting();
            }

            Messages.Message("BSL_QueuedLaunchAlreadyExists".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
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
