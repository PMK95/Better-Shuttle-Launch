using System;
using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.UI;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Shuttles
{
    public static class PassengerShuttleLaunchBridge
    {
        public static void StartStateAwareWorldMapLaunch(Building_PassengerShuttle shuttle)
        {
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out CompTransporter transporter, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!transporter.LoadingInProgressOrReadyToLaunch)
            {
                OpenVanillaLoadDialog(shuttle);
                return;
            }

            AcceptanceReport canLaunch = launchable.CanLaunch();
            if (!canLaunch.Accepted)
            {
                Messages.Message(canLaunch.Reason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            StartChoosingDestination(shuttle, launchable.TryLaunch);
        }

        public static bool CanStartStateAwareWorldMapLaunch(Building_PassengerShuttle shuttle, out string disabledReason)
        {
            disabledReason = null;
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out CompTransporter transporter, out disabledReason))
            {
                return false;
            }

            if (!transporter.LoadingInProgressOrReadyToLaunch)
            {
                return true;
            }

            AcceptanceReport canLaunch = launchable.CanLaunch();
            if (canLaunch.Accepted)
            {
                return true;
            }

            disabledReason = canLaunch.Reason;
            return false;
        }

        public static void OpenVanillaLoadDialog(Building_PassengerShuttle shuttle)
        {
            if (!TryGetLaunchParts(shuttle, out _, out CompTransporter transporter, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Dialog_LoadTransporters(transporter.Map, new List<CompTransporter> { transporter }));
        }

        public static void OpenLoadDialogThenChooseDestination(Building_PassengerShuttle shuttle, Action<PlanetTile, TransportersArrivalAction> destinationChosen)
        {
            if (!TryGetLaunchParts(shuttle, out _, out CompTransporter transporter, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Dialog_LoadTransportersThenRunAction(
                transporter.Map,
                new List<CompTransporter> { transporter },
                () => StartChoosingDestination(shuttle, destinationChosen)));
        }

        public static void OpenLoadDialogThenRunAction(Building_PassengerShuttle shuttle, Action afterAccepted)
        {
            if (!TryGetLaunchParts(shuttle, out _, out CompTransporter transporter, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Dialog_LoadTransportersThenRunAction(
                transporter.Map,
                new List<CompTransporter> { transporter },
                afterAccepted));
        }

        public static void StartChoosingDestination(Building_PassengerShuttle shuttle, Action<PlanetTile, TransportersArrivalAction> destinationChosen)
        {
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out _, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            launchable.StartChoosingDestination(destinationChosen);
        }

        public static void StartChoosingQueuedDestination(Building_PassengerShuttle shuttle, Action<PlanetTile, string, TransportersArrivalAction> destinationChosen)
        {
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out _, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            PlanetTile originTile = shuttle.Tile;
            BeginQueuedWorldTargeting(
                originTile,
                launchable,
                true,
                shuttle,
                target =>
                {
                    QueueFormCaravanDestination(target, destinationChosen);
                    StopWorldTargeting();
                });
        }

        public static void StartChoosingQueuedDestinationFromCaravan(Caravan caravan, Action<PlanetTile, string, TransportersArrivalAction> destinationChosen)
        {
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            CompLaunchable launchable = shuttle?.LaunchableComp;
            if (caravan == null || caravan.Destroyed || shuttle == null || launchable == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            BeginQueuedWorldTargeting(
                caravan.Tile,
                launchable,
                false,
                shuttle,
                target =>
                {
                    QueueFormCaravanDestination(target, destinationChosen);
                    StopWorldTargeting();
                });
        }

        public static bool TryChooseSpecificLandingCell(
            Building_PassengerShuttle shuttle,
            MapParent destination,
            IntVec3 cell,
            Rot4 rotation,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            out string failReason)
        {
            failReason = null;
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out CompTransporter transporter, out failReason))
            {
                return false;
            }

            List<CompTransporter> transportersInGroup = transporter.TransportersInGroup(shuttle.Map);
            return TryChooseSpecificLandingCell(
                shuttle.Tile,
                transportersInGroup,
                launchable,
                shuttle.def,
                destination,
                cell,
                rotation,
                launchAction,
                null,
                out failReason);
        }

        public static bool CanChooseSpecificLandingCell(
            Building_PassengerShuttle shuttle,
            MapParent destination,
            IntVec3 cell,
            Rot4 rotation,
            out string failReason)
        {
            failReason = null;
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out CompTransporter transporter, out failReason))
            {
                return false;
            }

            return CanUseSpecificLandingCell(
                shuttle.Tile,
                transporter.TransportersInGroup(shuttle.Map),
                launchable,
                shuttle.def,
                destination,
                cell,
                rotation,
                null,
                out failReason);
        }

        public static bool TryChooseSpecificLandingCellFromCaravan(
            Caravan caravan,
            MapParent destination,
            IntVec3 cell,
            Rot4 rotation,
            out string failReason)
        {
            failReason = null;
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            CompLaunchable launchable = shuttle?.LaunchableComp;
            CompTransporter transporter = shuttle?.TransporterComp;
            if (caravan == null || shuttle == null || launchable == null || transporter == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            List<CompTransporter> transporters = new List<CompTransporter> { transporter };
            return TryChooseSpecificLandingCell(
                caravan.Tile,
                transporters,
                launchable,
                shuttle.def,
                destination,
                cell,
                rotation,
                (tile, action) => CaravanShuttleUtility.LaunchShuttle(caravan, tile, action),
                shuttle.FuelLevel,
                out failReason);
        }

        public static bool CanChooseSpecificLandingCellFromCaravan(
            Caravan caravan,
            MapParent destination,
            IntVec3 cell,
            Rot4 rotation,
            out string failReason)
        {
            failReason = null;
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            CompLaunchable launchable = shuttle?.LaunchableComp;
            CompTransporter transporter = shuttle?.TransporterComp;
            if (caravan == null || shuttle == null || launchable == null || transporter == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            return CanUseSpecificLandingCell(
                caravan.Tile,
                new List<CompTransporter> { transporter },
                launchable,
                shuttle.def,
                destination,
                cell,
                rotation,
                shuttle.FuelLevel,
                out failReason);
        }

        public static bool TryChooseSpecificWorldTarget(Building_PassengerShuttle shuttle, GlobalTargetInfo target, Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out CompTransporter transporter, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!target.IsValid || !target.Tile.Valid)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            List<CompTransporter> transportersInGroup = transporter.TransportersInGroup(shuttle.Map);
            if (transportersInGroup == null)
            {
                Messages.Message("BSL_LoadingCanceled".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return CompLaunchable.ChoseWorldTarget(
                target,
                shuttle.Tile,
                transportersInGroup,
                launchable.MaxLaunchDistanceEver(target.Tile.Layer),
                launchAction,
                launchable);
        }

        public static bool StartChoosingSpecificLandingCellForQueuedLaunch(Building_PassengerShuttle shuttle, MapParent destination, Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out CompTransporter transporter, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            List<CompTransporter> transportersInGroup = transporter.TransportersInGroup(shuttle.Map);
            if (transportersInGroup == null)
            {
                Messages.Message("BSL_LoadingCanceled".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!TryStartQueuedLandingCellTargeter(
                shuttle.Tile,
                transportersInGroup,
                launchable,
                shuttle.def,
                destination,
                launchAction,
                out failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        public static bool TryChooseSpecificWorldTargetFromCaravan(Caravan caravan, GlobalTargetInfo target, Action<PlanetTile, TransportersArrivalAction> destinationChosen)
        {
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            CompLaunchable launchable = shuttle?.LaunchableComp;
            CompTransporter transporter = shuttle?.TransporterComp;
            if (caravan == null || shuttle == null || launchable == null || transporter == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!target.IsValid || !target.Tile.Valid)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return CompLaunchable.ChoseWorldTarget(
                target,
                caravan.Tile,
                new List<CompTransporter> { transporter },
                launchable.MaxLaunchDistanceEver(target.Tile.Layer),
                destinationChosen,
                launchable,
                shuttle.FuelLevel);
        }

        public static bool StartChoosingSpecificLandingCellFromCaravanForQueuedLaunch(Caravan caravan, MapParent destination, Action<PlanetTile, TransportersArrivalAction> destinationChosen)
        {
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            CompLaunchable launchable = shuttle?.LaunchableComp;
            CompTransporter transporter = shuttle?.TransporterComp;
            if (caravan == null || shuttle == null || launchable == null || transporter == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!TryStartQueuedLandingCellTargeter(
                caravan.Tile,
                new List<CompTransporter> { transporter },
                launchable,
                shuttle.def,
                destination,
                destinationChosen,
                out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        public static bool TryCreateSpecificLandingActionForQueuedLaunch(
            Building_PassengerShuttle shuttle,
            MapParent destination,
            IntVec3 cell,
            Rot4 rotation,
            out PlanetTile destinationTile,
            out TransportersArrivalAction arrivalAction,
            out string failReason)
        {
            destinationTile = default;
            arrivalAction = null;
            failReason = null;
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out CompTransporter transporter, out failReason))
            {
                return false;
            }

            List<CompTransporter> transportersInGroup = transporter.TransportersInGroup(shuttle.Map);
            if (!CanCreateSpecificLandingActionForQueuedLaunch(
                    shuttle.Tile,
                    transportersInGroup,
                    launchable,
                    shuttle.def,
                    destination,
                    cell,
                    rotation,
                    out failReason))
            {
                return false;
            }

            destinationTile = destination.Tile;
            arrivalAction = new TransportersArrivalAction_LandInSpecificCell(destination, cell, rotation, true);
            return true;
        }

        public static bool TryCreateSpecificLandingActionForCaravanQueuedLaunch(
            Caravan caravan,
            MapParent destination,
            IntVec3 cell,
            Rot4 rotation,
            out PlanetTile destinationTile,
            out TransportersArrivalAction arrivalAction,
            out string failReason)
        {
            destinationTile = default;
            arrivalAction = null;
            failReason = null;
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            CompLaunchable launchable = shuttle?.LaunchableComp;
            CompTransporter transporter = shuttle?.TransporterComp;
            if (caravan == null || shuttle == null || launchable == null || transporter == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            if (!CanCreateSpecificLandingActionForQueuedLaunch(
                    caravan.Tile,
                    new List<CompTransporter> { transporter },
                    launchable,
                    shuttle.def,
                    destination,
                    cell,
                    rotation,
                    out failReason))
            {
                return false;
            }

            destinationTile = destination.Tile;
            arrivalAction = new TransportersArrivalAction_LandInSpecificCell(destination, cell, rotation, true);
            return true;
        }

        public static bool TryLaunchImmediately(Building_PassengerShuttle shuttle, PlanetTile destinationTile, TransportersArrivalAction arrivalAction, out string failReason)
        {
            failReason = null;
            if (!TryGetLaunchParts(shuttle, out CompLaunchable launchable, out _, out failReason))
            {
                return false;
            }

            AcceptanceReport canLaunch = launchable.CanLaunch();
            if (!canLaunch.Accepted)
            {
                failReason = canLaunch.Reason;
                return false;
            }

            launchable.TryLaunch(destinationTile, arrivalAction);
            return true;
        }

        public static bool TryLaunchCaravanImmediately(Caravan caravan, PlanetTile destinationTile, TransportersArrivalAction arrivalAction, out string failReason)
        {
            failReason = null;
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            if (caravan == null || shuttle == null || shuttle.LaunchableComp == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            AcceptanceReport canLaunch = CaravanShuttleUtility.CanLaunchCaravanShuttle(caravan);
            if (!canLaunch.Accepted)
            {
                failReason = canLaunch.Reason;
                return false;
            }

            int distance = Find.WorldGrid.TraversalDistanceBetween(caravan.Tile, destinationTile, true, int.MaxValue, true);
            if (distance > shuttle.LaunchableComp.MaxLaunchDistanceAtFuelLevel(shuttle.FuelLevel, destinationTile.Layer))
            {
                failReason = "TransportPodNotEnoughFuel".Translate();
                return false;
            }

            CaravanShuttleUtility.LaunchShuttle(caravan, destinationTile, arrivalAction);
            return true;
        }

        public static bool TryGetLaunchParts(Building_PassengerShuttle shuttle, out CompLaunchable launchable, out CompTransporter transporter, out string failReason)
        {
            launchable = null;
            transporter = null;
            failReason = null;

            if (!PassengerShuttleFinder.IsSupportedPassengerShuttle(shuttle))
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            launchable = shuttle.LaunchableComp;
            transporter = shuttle.TransporterComp;
            if (launchable == null || transporter == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            return true;
        }

        private static bool TryChooseSpecificLandingCell(
            PlanetTile originTile,
            List<CompTransporter> transporters,
            CompLaunchable launchable,
            ThingDef shuttleDef,
            MapParent destination,
            IntVec3 cell,
            Rot4 rotation,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            float? overrideFuelLevel,
            out string failReason)
        {
            if (!CanUseSpecificLandingCell(
                    originTile,
                    transporters,
                    launchable,
                    shuttleDef,
                    destination,
                    cell,
                    rotation,
                    overrideFuelLevel,
                    out failReason))
            {
                return false;
            }

            launchAction(destination.Tile, new TransportersArrivalAction_LandInSpecificCell(destination, cell, rotation, true));
            return true;
        }

        private static bool CanUseSpecificLandingCell(
            PlanetTile originTile,
            List<CompTransporter> transporters,
            CompLaunchable launchable,
            ThingDef shuttleDef,
            MapParent destination,
            IntVec3 cell,
            Rot4 rotation,
            float? overrideFuelLevel,
            out string failReason)
        {
            failReason = null;
            if (transporters == null || launchable == null || destination == null || !destination.Spawned || !destination.HasMap)
            {
                failReason = "BSL_DestinationInvalid".Translate();
                return false;
            }

            AcceptanceReport canLaunch = launchable.CanLaunch(overrideFuelLevel);
            if (!canLaunch.Accepted)
            {
                failReason = canLaunch.Reason;
                return false;
            }

            if (destination.EnterCooldownBlocksEntering())
            {
                failReason = "MessageEnterCooldownBlocksEntering".Translate(destination.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true, false));
                return false;
            }

            AcceptanceReport canLand = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(new LocalTargetInfo(cell), destination.Map, shuttleDef, rotation);
            if (!canLand.Accepted)
            {
                failReason = canLand.Reason;
                return false;
            }

            int distance = Find.WorldGrid.TraversalDistanceBetween(originTile, destination.Tile, true, int.MaxValue, true);
            float fuelLevel = overrideFuelLevel ?? launchable.FuelLevel;
            if (distance > launchable.MaxLaunchDistanceAtFuelLevel(fuelLevel, destination.Tile.Layer))
            {
                failReason = "TransportPodNotEnoughFuel".Translate();
                return false;
            }

            int maxLaunchDistance = launchable.MaxLaunchDistanceEver(destination.Tile.Layer);
            if (maxLaunchDistance >= 0 && distance > maxLaunchDistance)
            {
                failReason = "TransportPodDestinationBeyondMaximumRange".Translate();
                return false;
            }

            FloatMenuAcceptanceReport stillValid = new TransportersArrivalAction_LandInSpecificCell(destination, cell, rotation, true)
                .StillValid(transporters, destination.Tile);
            if (stillValid.Accepted)
            {
                return true;
            }

            failReason = stillValid.FailReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : stillValid.FailReason;
            return false;
        }

        private static bool CanCreateSpecificLandingActionForQueuedLaunch(
            PlanetTile originTile,
            List<CompTransporter> transporters,
            CompLaunchable launchable,
            ThingDef shuttleDef,
            MapParent destination,
            IntVec3 cell,
            Rot4? rotation,
            out string failReason)
        {
            if (!CanStartQueuedLandingCellTargeter(originTile, transporters, launchable, destination, out failReason))
            {
                return false;
            }

            if (!cell.IsValid || !cell.InBounds(destination.Map))
            {
                failReason = "BSL_DestinationInvalid".Translate();
                return false;
            }

            failReason = null;
            AcceptanceReport canLand = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(new LocalTargetInfo(cell), destination.Map, shuttleDef, rotation);
            if (!canLand.Accepted)
            {
                failReason = canLand.Reason;
                return false;
            }

            return true;
        }

        private static bool CanStartQueuedLandingCellTargeter(
            PlanetTile originTile,
            List<CompTransporter> transporters,
            CompLaunchable launchable,
            MapParent destination,
            out string failReason)
        {
            failReason = null;
            if (transporters == null || launchable == null || destination == null || !destination.Spawned || !destination.HasMap || destination.Map == null)
            {
                failReason = "BSL_DestinationInvalid".Translate();
                return false;
            }

            int maxLaunchDistance = launchable.MaxLaunchDistanceEver(destination.Tile.Layer);
            int distance = Find.WorldGrid.TraversalDistanceBetween(originTile, destination.Tile, true, int.MaxValue, true);
            if (maxLaunchDistance >= 0 && distance > maxLaunchDistance)
            {
                failReason = "TransportPodDestinationBeyondMaximumRange".Translate();
                return false;
            }

            return true;
        }

        private static bool TryStartQueuedLandingCellTargeter(
            PlanetTile originTile,
            List<CompTransporter> transporters,
            CompLaunchable launchable,
            ThingDef shuttleDef,
            MapParent destination,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            out string failReason)
        {
            failReason = null;
            if (launchAction == null)
            {
                failReason = "BSL_DestinationInvalid".Translate();
                return false;
            }

            if (!CanStartQueuedLandingCellTargeter(
                    originTile,
                    transporters,
                    launchable,
                    destination,
                    out failReason))
            {
                return false;
            }

            if (Find.Targeter == null)
            {
                failReason = "BSL_DestinationTargetingStartFailed".Translate();
                return false;
            }

            try
            {
                CameraJumper.TryJump(destination.Map.Center, destination.Map, CameraJumper.MovementMode.Pan);
            }
            catch (Exception exception)
            {
                Log.Warning("[Better Shuttle Launch] 예약 발사 착륙 위치 선택을 위해 목적지 맵으로 이동하는 중 오류가 발생했습니다: " + exception);
                failReason = "BSL_DestinationTargetingStartFailed".Translate();
                return false;
            }

            TargetingParameters targetingParameters = TargetingParameters.ForCell();
            targetingParameters.validator = target => CanCreateSpecificLandingActionForQueuedLaunch(
                originTile,
                transporters,
                launchable,
                shuttleDef,
                destination,
                target.Cell,
                null,
                out _);

            Find.Targeter.StopTargeting();
            Find.Targeter.BeginTargeting(
                targetingParameters,
                target =>
                {
                    if (!CanCreateSpecificLandingActionForQueuedLaunch(
                            originTile,
                            transporters,
                            launchable,
                            shuttleDef,
                            destination,
                            target.Cell,
                            null,
                            out string selectionFailReason))
                    {
                        Messages.Message(selectionFailReason, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    Find.Targeter.StopTargeting();
                    launchAction(destination.Tile, new TransportersArrivalAction_LandInSpecificCell(destination, target.Cell));
                },
                null);
            return true;
        }

        private static void BeginQueuedWorldTargeting(PlanetTile originTile, CompLaunchable launchable, bool jumpToOriginTile, Building_PassengerShuttle shuttle, Action<GlobalTargetInfo> targetChosen)
        {
            if (Find.WorldTargeter == null)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            StopWorldTargeting();
            if (jumpToOriginTile && !TryJumpToWorldTileForQueuedTargeting(originTile))
            {
                Messages.Message("BSL_DestinationTargetingStartFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            QueuedLaunchRangeTargetingSession.Begin(originTile, launchable, shuttle, tile => GetWorldTargetLabel(tile));
            Find.WorldTargeter.BeginTargeting(
                target =>
                {
                    if (!CanSelectQueuedWorldTarget(originTile, launchable, shuttle, target, out string disabledReason))
                    {
                        Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                        StopWorldTargeting();
                        return true;
                    }

                    targetChosen(target);
                    return true;
                },
                true,
                CompLaunchable.LaunchCommandTex,
                false,
                null,
                target => GetQueuedTargetingLabel(originTile, launchable, shuttle, target),
                target => CanSelectQueuedWorldTarget(originTile, launchable, shuttle, target, out _),
                originTile,
                true);
        }

        private static bool TryJumpToWorldTileForQueuedTargeting(PlanetTile originTile)
        {
            if (!originTile.Valid)
            {
                return false;
            }

            try
            {
                CameraJumper.TryJump(originTile, CameraJumper.MovementMode.Pan);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("[Better Shuttle Launch] 예약 발사 목적지 선택을 위해 세계 지도로 이동하는 중 오류가 발생했습니다: " + exception);
                return false;
            }
        }

        private static TaggedString GetQueuedTargetingLabel(PlanetTile originTile, CompLaunchable launchable, Building_PassengerShuttle shuttle, GlobalTargetInfo target)
        {
            QueuedLaunchRangeInfo rangeInfo = QueuedLaunchRangeInfo.ForTarget(originTile, launchable, target, GetCurrentFuelLevel(shuttle, launchable));
            if (!rangeInfo.CanSelectDestination)
            {
                return rangeInfo.FailureReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : rangeInfo.FailureReason;
            }

            string destinationLabel = GetWorldTargetLabel(target);
            if (rangeInfo.State == QueuedLaunchRangeState.NeedsFuel)
            {
                return "BSL_QueuedDestinationFuelWaiting".Translate(destinationLabel, rangeInfo.Distance.ToString(), rangeInfo.CurrentFuelRangeText);
            }

            return "BSL_QueuedDestinationFuelReady".Translate(destinationLabel, rangeInfo.Distance.ToString(), rangeInfo.CurrentFuelRangeText);
        }

        private static bool CanSelectQueuedWorldTarget(PlanetTile originTile, CompLaunchable launchable, Building_PassengerShuttle shuttle, GlobalTargetInfo target, out string disabledReason)
        {
            disabledReason = null;
            QueuedLaunchRangeInfo rangeInfo = QueuedLaunchRangeInfo.ForTarget(originTile, launchable, target, GetCurrentFuelLevel(shuttle, launchable));
            if (rangeInfo.CanSelectDestination)
            {
                return true;
            }

            disabledReason = rangeInfo.FailureReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : rangeInfo.FailureReason;
            return false;
        }

        private static float? GetCurrentFuelLevel(Building_PassengerShuttle shuttle, CompLaunchable launchable)
        {
            if (shuttle != null && !shuttle.Destroyed)
            {
                return shuttle.FuelLevel;
            }

            return launchable?.FuelLevel;
        }

        private static void QueueFormCaravanDestination(GlobalTargetInfo target, Action<PlanetTile, string, TransportersArrivalAction> destinationChosen)
        {
            PlanetTile destinationTile = target.Tile;
            if (!destinationTile.Valid)
            {
                Messages.Message("BSL_DestinationInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            destinationChosen(destinationTile, GetWorldTargetLabel(target), new TransportersArrivalAction_FormCaravan("MessageTransportPodsArrived"));
        }

        private static string GetWorldTargetLabel(GlobalTargetInfo target)
        {
            if (target.HasWorldObject && target.WorldObject != null && !target.WorldObject.Destroyed)
            {
                return target.WorldObject.LabelCap;
            }

            return GetWorldTargetLabel(target.Tile);
        }

        private static string GetWorldTargetLabel(PlanetTile tile)
        {
            if (tile.Valid && Find.WorldObjects != null)
            {
                WorldObject worldObject = Find.WorldObjects.MapParentAt(tile);
                if (worldObject == null)
                {
                    foreach (WorldObject candidate in Find.WorldObjects.ObjectsAt(tile))
                    {
                        if (candidate != null && !candidate.Destroyed)
                        {
                            worldObject = candidate;
                            break;
                        }
                    }
                }

                if (worldObject != null && !worldObject.Destroyed)
                {
                    return worldObject.LabelCap;
                }
            }

            return tile.Valid ? tile.ToString() : "BSL_StatusUnavailable".Translate();
        }

        public static void StopWorldTargeting()
        {
            QueuedLaunchRangeTargetingSession.Clear();
            if (Find.WorldTargeter != null && Find.WorldTargeter.IsTargeting)
            {
                Find.WorldTargeter.StopTargeting();
            }
        }
    }
}
