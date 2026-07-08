using System;
using System.Collections.Generic;
using BetterShuttleLaunch.Domain;
using BetterShuttleLaunch.UI;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.RimWorldApi
{
    public static class PassengerShuttleLaunchApi
    {
        public static bool OpenLoadDialogThenRun(ShuttleContext context, Action afterAccepted)
        {
            if (context == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (context.IsCaravan)
            {
                afterAccepted?.Invoke();
                return true;
            }

            if (!ShuttleContext.TryForMapShuttle(context.Shuttle, out ShuttleContext currentContext, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            Find.WindowStack.Add(new Dialog_LoadTransportersThenRunAction(
                currentContext.Map,
                new List<CompTransporter> { currentContext.Transporter },
                afterAccepted));
            return true;
        }

        public static bool TryStartQueuedWorldTargeting(
            ShuttleContext context,
            Action<PlanetTile, string, TransportersArrivalAction> destinationChosen)
        {
            if (!CanStartWorldTargeting(context, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            StopWorldTargeting();
            if (!context.IsCaravan && !TryJumpToWorldTile(context.OriginTile))
            {
                Messages.Message("BSL_DestinationTargetingStartFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            QueuedLaunchTargetingSession.Begin(context, GetWorldTargetLabel);
            Find.WorldTargeter.BeginTargeting(
                target =>
                {
                    if (!CanSelectQueuedWorldTarget(context, target, out string disabledReason))
                    {
                        Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                        StopWorldTargeting();
                        return true;
                    }

                    destinationChosen(
                        target.Tile,
                        GetWorldTargetLabel(target),
                        new TransportersArrivalAction_FormCaravan("MessageTransportPodsArrived"));
                    StopWorldTargeting();
                    return true;
                },
                true,
                CompLaunchable.LaunchCommandTex,
                false,
                null,
                target => GetQueuedTargetingLabel(context, target),
                target => CanSelectQueuedWorldTarget(context, target, out _),
                context.OriginTile,
                true);
            return true;
        }

        public static bool TryStartQueuedLandingCellTargeter(
            ShuttleContext context,
            MapParent destination,
            Action<PlanetTile, TransportersArrivalAction> destinationChosen)
        {
            if (!TryGetTransportersForArrival(context, out List<CompTransporter> transporters, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!CanStartQueuedLandingCellTargeter(context, transporters, destination, out failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (Find.Targeter == null)
            {
                Messages.Message("BSL_DestinationTargetingStartFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            try
            {
                CameraJumper.TryJump(destination.Map.Center, destination.Map, CameraJumper.MovementMode.Pan);
            }
            catch (Exception exception)
            {
                Log.Warning("[Better Shuttle Launch] 예약 발사 착륙 위치 선택을 위해 목적지 맵으로 이동하는 중 오류가 발생했습니다: " + exception);
                Messages.Message("BSL_DestinationTargetingStartFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            TargetingParameters targetingParameters = TargetingParameters.ForCell();
            targetingParameters.validator = target => TryCreateQueuedSpecificLandingAction(
                context,
                destination,
                target.Cell,
                null,
                out _,
                out _,
                out _);

            Find.Targeter.StopTargeting();
            Find.Targeter.BeginTargeting(
                targetingParameters,
                target =>
                {
                    if (!TryCreateQueuedSpecificLandingAction(
                            context,
                            destination,
                            target.Cell,
                            null,
                            out PlanetTile destinationTile,
                            out TransportersArrivalAction arrivalAction,
                            out string selectionFailReason))
                    {
                        Messages.Message(selectionFailReason, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    Find.Targeter.StopTargeting();
                    destinationChosen(destinationTile, arrivalAction);
                },
                null);
            return true;
        }

        public static bool TryCreateQueuedSpecificLandingAction(
            ShuttleContext context,
            MapParent destination,
            IntVec3 cell,
            Rot4? rotation,
            out PlanetTile destinationTile,
            out TransportersArrivalAction arrivalAction,
            out string failReason)
        {
            destinationTile = default;
            arrivalAction = null;
            if (!TryGetTransportersForArrival(context, out List<CompTransporter> transporters, out failReason))
            {
                return false;
            }

            if (!CanStartQueuedLandingCellTargeter(context, transporters, destination, out failReason))
            {
                return false;
            }

            if (!cell.IsValid || !cell.InBounds(destination.Map))
            {
                failReason = "BSL_DestinationInvalid".Translate();
                return false;
            }

            AcceptanceReport canLand = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(
                new LocalTargetInfo(cell),
                destination.Map,
                context.Shuttle.def,
                rotation);
            if (!canLand.Accepted)
            {
                failReason = canLand.Reason;
                return false;
            }

            destinationTile = destination.Tile;
            arrivalAction = rotation.HasValue
                ? new TransportersArrivalAction_LandInSpecificCell(destination, cell, rotation.Value, true)
                : new TransportersArrivalAction_LandInSpecificCell(destination, cell);
            return true;
        }

        public static bool TryLaunchImmediately(QueuedShuttleLaunch queuedLaunch, out string failReason)
        {
            failReason = null;
            if (!TryGetContext(queuedLaunch, out ShuttleContext context, out failReason))
            {
                return false;
            }

            if (context.IsCaravan)
            {
                AcceptanceReport canLaunchCaravan = CaravanShuttleUtility.CanLaunchCaravanShuttle(context.Caravan);
                if (!canLaunchCaravan.Accepted)
                {
                    failReason = canLaunchCaravan.Reason;
                    return false;
                }

                CaravanShuttleUtility.LaunchShuttle(context.Caravan, queuedLaunch.DestinationTile, queuedLaunch.ArrivalAction);
                return true;
            }

            AcceptanceReport canLaunch = context.Launchable.CanLaunch();
            if (!canLaunch.Accepted)
            {
                failReason = canLaunch.Reason;
                return false;
            }

            context.Launchable.TryLaunch(queuedLaunch.DestinationTile, queuedLaunch.ArrivalAction);
            return true;
        }

        public static bool TryGetContext(QueuedShuttleLaunch queuedLaunch, out ShuttleContext context, out string failReason)
        {
            context = null;
            failReason = null;
            if (queuedLaunch == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            return queuedLaunch.Caravan != null
                ? ShuttleContext.TryForCaravan(queuedLaunch.Caravan, out context, out failReason)
                : ShuttleContext.TryForMapShuttle(queuedLaunch.Shuttle, out context, out failReason);
        }

        public static bool TryGetTransportersForArrival(ShuttleContext context, out List<CompTransporter> transporters, out string failReason)
        {
            transporters = null;
            failReason = null;
            if (context == null || context.Transporter == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            if (context.IsCaravan)
            {
                transporters = new List<CompTransporter> { context.Transporter };
                return true;
            }

            transporters = context.Transporter.TransportersInGroup(context.Map);
            if (transporters != null)
            {
                return true;
            }

            failReason = "BSL_LoadingCanceled".Translate();
            return false;
        }

        public static void StopWorldTargeting()
        {
            QueuedLaunchTargetingSession.Clear();
            if (Find.WorldTargeter != null && Find.WorldTargeter.IsTargeting)
            {
                Find.WorldTargeter.StopTargeting();
            }
        }

        public static string GetWorldTargetLabel(GlobalTargetInfo target)
        {
            if (target.HasWorldObject && target.WorldObject != null && !target.WorldObject.Destroyed)
            {
                return target.WorldObject.LabelCap;
            }

            return GetWorldTargetLabel(target.Tile);
        }

        public static string GetWorldTargetLabel(PlanetTile tile)
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

        private static bool CanStartWorldTargeting(ShuttleContext context, out string failReason)
        {
            failReason = null;
            if (Find.WorldTargeter == null || context == null || context.Launchable == null || !context.OriginTile.Valid)
            {
                failReason = "BSL_DestinationTargetingStartFailed".Translate();
                return false;
            }

            return true;
        }

        private static bool TryJumpToWorldTile(PlanetTile originTile)
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

        private static TaggedString GetQueuedTargetingLabel(ShuttleContext context, GlobalTargetInfo target)
        {
            LaunchRangeInfo rangeInfo = LaunchRangeInfo.ForTarget(context, target);
            if (!rangeInfo.CanSelectDestination)
            {
                return rangeInfo.FailureReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : rangeInfo.FailureReason;
            }

            string destinationLabel = GetWorldTargetLabel(target);
            if (rangeInfo.State == LaunchRangeState.NeedsFuel)
            {
                return "BSL_QueuedDestinationFuelWaiting".Translate(destinationLabel, rangeInfo.Distance.ToString(), rangeInfo.CurrentFuelRangeText);
            }

            return "BSL_QueuedDestinationFuelReady".Translate(destinationLabel, rangeInfo.Distance.ToString(), rangeInfo.CurrentFuelRangeText);
        }

        private static bool CanSelectQueuedWorldTarget(ShuttleContext context, GlobalTargetInfo target, out string disabledReason)
        {
            disabledReason = null;
            LaunchRangeInfo rangeInfo = LaunchRangeInfo.ForTarget(context, target);
            if (rangeInfo.CanSelectDestination)
            {
                return true;
            }

            disabledReason = rangeInfo.FailureReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : rangeInfo.FailureReason;
            return false;
        }

        private static bool CanStartQueuedLandingCellTargeter(
            ShuttleContext context,
            List<CompTransporter> transporters,
            MapParent destination,
            out string failReason)
        {
            failReason = null;
            if (context == null
                || context.Launchable == null
                || transporters == null
                || destination == null
                || !destination.Spawned
                || !destination.HasMap
                || destination.Map == null)
            {
                failReason = "BSL_DestinationInvalid".Translate();
                return false;
            }

            LaunchRangeInfo rangeInfo = LaunchRangeInfo.ForTile(context, destination.Tile);
            if (rangeInfo.State != LaunchRangeState.BeyondMaximumRange && rangeInfo.State != LaunchRangeState.InvalidDestination)
            {
                return true;
            }

            failReason = rangeInfo.FailureReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : rangeInfo.FailureReason;
            return false;
        }
    }
}
