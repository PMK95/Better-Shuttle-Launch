using System;
using System.Collections.Generic;
using BetterShuttleLaunch.Shuttles;
using BetterShuttleLaunch.ReturnHome;
using BetterShuttleLaunch.UI;
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
            PassengerShuttleLaunchBridge.OpenLoadDialogThenChooseDestination(shuttle, (destinationTile, arrivalAction) =>
            {
                QueueMapShuttleLaunch(shuttle, destinationTile, GetOriginLabel(shuttle), GetDestinationLabel(destinationTile, arrivalAction), arrivalAction);
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
                QueueCaravanLaunch(caravan, shuttle, destinationTile, GetOriginLabel(caravan), GetDestinationLabel(destinationTile, arrivalAction), arrivalAction);
            });
        }

        public static void StartSettlementLaunchFlow(Building_PassengerShuttle shuttle)
        {
            if (!CanQueueLaunchWhenReady(shuttle, out string disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            IReadOnlyList<MapParent> destinations = SettlementDestinationFinder.FindOtherPlayerHomeMapParents(shuttle.Map?.Parent);
            if (destinations.Count == 0)
            {
                Messages.Message("BSL_NoOtherSettlement".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Dialog_SelectHomeDestination(destinations, destination => StartSettlementLaunchToSelectedSettlement(shuttle, destination)));
        }

        public static void StartCaravanSettlementLaunchFlow(Caravan caravan)
        {
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

            Find.WindowStack.Add(new Dialog_SelectHomeDestination(destinations, destination => StartCaravanSettlementLaunchToSelectedSettlement(caravan, destination)));
        }

        public static void StartReturnFlow(Building_PassengerShuttle shuttle)
        {
            if (!TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Dialog_SelectReturnLaunchMode(
                () => StartReturnWithLandingSelection(shuttle, location.MapParent),
                () => StartReturnToLastDepartureCell(shuttle, location)));
        }

        public static void StartCaravanReturnFlow(Caravan caravan)
        {
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            if (!TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location))
            {
                Messages.Message("BSL_LastDepartureCellUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Dialog_SelectReturnLaunchMode(
                () => StartCaravanReturnWithLandingSelection(caravan, location.MapParent),
                () => StartCaravanReturnToLastDepartureCell(caravan, location)));
        }

        public static void QueueMapShuttleLaunch(
            Building_PassengerShuttle shuttle,
            PlanetTile destinationTile,
            string originLabel,
            string destinationLabel,
            TransportersArrivalAction arrivalAction)
        {
            if (arrivalAction == null || !destinationTile.Valid)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            PlanetTile originTile = shuttle?.Tile ?? default;
            LaunchQueueGameComponent.Current?.AddOrReplaceQueuedLaunch(new QueuedPassengerShuttleLaunch(
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
            if (arrivalAction == null || !destinationTile.Valid)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            LaunchQueueGameComponent.Current?.AddOrReplaceQueuedLaunch(new QueuedPassengerShuttleLaunch(
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
            PassengerShuttleLaunchBridge.OpenLoadDialogThenRunAction(shuttle, () =>
            {
                if (destination == null)
                {
                    Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }

                PassengerShuttleLaunchBridge.TryChooseSpecificWorldTarget(
                    shuttle,
                    new GlobalTargetInfo(destination),
                    (tile, action) => QueueMapShuttleLaunch(shuttle, tile, GetOriginLabel(shuttle), destination.LabelCap, action));
            });
        }

        private static void StartCaravanSettlementLaunchToSelectedSettlement(Caravan caravan, MapParent destination)
        {
            if (destination == null)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            PassengerShuttleLaunchBridge.TryChooseSpecificWorldTargetFromCaravan(
                caravan,
                new GlobalTargetInfo(destination),
                (tile, action) => QueueCaravanLaunch(caravan, shuttle, tile, GetOriginLabel(caravan), destination.LabelCap, action));
        }

        private static void StartReturnWithLandingSelection(Building_PassengerShuttle shuttle, MapParent destination)
        {
            if (destination == null)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            PassengerShuttleLaunchBridge.OpenLoadDialogThenRunAction(shuttle, () =>
            {
                PassengerShuttleLaunchBridge.TryChooseSpecificWorldTarget(
                    shuttle,
                    new GlobalTargetInfo(destination),
                    (tile, action) => QueueMapShuttleLaunch(shuttle, tile, GetOriginLabel(shuttle), destination.LabelCap, action));
            });
        }

        private static void StartCaravanReturnWithLandingSelection(Caravan caravan, MapParent destination)
        {
            if (destination == null)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            PassengerShuttleLaunchBridge.TryChooseSpecificWorldTargetFromCaravan(
                caravan,
                new GlobalTargetInfo(destination),
                (tile, action) => QueueCaravanLaunch(caravan, shuttle, tile, GetOriginLabel(caravan), destination.LabelCap, action));
        }

        private static void StartReturnToLastDepartureCell(Building_PassengerShuttle shuttle, LastDepartureLocation location)
        {
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

        private static void StartCaravanReturnToLastDepartureCell(Caravan caravan, LastDepartureLocation location)
        {
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

        private static bool TryGetLastDepartureLocation(Building_PassengerShuttle shuttle, out LastDepartureLocation location)
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
    }
}
