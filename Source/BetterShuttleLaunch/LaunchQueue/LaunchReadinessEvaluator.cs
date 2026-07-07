using System.Collections.Generic;
using BetterShuttleLaunch.Shuttles;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public static class LaunchReadinessEvaluator
    {
        public static LaunchReadinessResult EvaluateQueuedPassengerShuttleLaunch(QueuedPassengerShuttleLaunch queuedLaunch)
        {
            if (queuedLaunch?.Caravan != null)
            {
                return EvaluateQueuedCaravanPassengerShuttleLaunch(queuedLaunch);
            }

            if (queuedLaunch == null || !PassengerShuttleFinder.IsSupportedPassengerShuttle(queuedLaunch.Shuttle))
            {
                return Cancel("BSL_ShuttleUnavailable".Translate());
            }

            if (queuedLaunch.ArrivalAction == null || !queuedLaunch.DestinationTile.Valid)
            {
                return Cancel("BSL_DestinationInvalid".Translate());
            }

            CompTransporter transporter = queuedLaunch.Shuttle.TransporterComp;
            CompLaunchable launchable = queuedLaunch.Shuttle.LaunchableComp;
            if (transporter == null || launchable == null)
            {
                return Cancel("BSL_ShuttleUnavailable".Translate());
            }

            if (!transporter.LoadingInProgressOrReadyToLaunch)
            {
                return Cancel("BSL_LoadingCanceled".Translate());
            }

            if (transporter.AnyInGroupHasAnythingLeftToLoad)
            {
                return Wait("BSL_WaitingForLoading".Translate());
            }

            AcceptanceReport canLaunch = launchable.CanLaunch();
            if (!canLaunch.Accepted)
            {
                if (TryGetRemainingCooldownTicks(launchable, out int ticksLeft))
                {
                    return Wait("BSL_WaitingForCooldown".Translate(ticksLeft.ToStringTicksToPeriod(true, false, true, true, false)));
                }

                return Wait(canLaunch.Reason.NullOrEmpty() ? "BSL_StatusUnavailable".Translate() : canLaunch.Reason);
            }

            List<CompTransporter> transportersInGroup = transporter.TransportersInGroup(queuedLaunch.Shuttle.Map);
            if (transportersInGroup == null)
            {
                return Cancel("BSL_LoadingCanceled".Translate());
            }

            FloatMenuAcceptanceReport stillValid = queuedLaunch.ArrivalAction.StillValid(transportersInGroup, queuedLaunch.DestinationTile);
            if (!stillValid.Accepted)
            {
                string reason = stillValid.FailReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : stillValid.FailReason;
                return Cancel(reason);
            }

            return new LaunchReadinessResult(true, false, "BSL_StatusReady".Translate(), null);
        }

        private static LaunchReadinessResult EvaluateQueuedCaravanPassengerShuttleLaunch(QueuedPassengerShuttleLaunch queuedLaunch)
        {
            Caravan caravan = queuedLaunch.Caravan;
            Building_PassengerShuttle shuttle = queuedLaunch.Shuttle;
            if (caravan == null
                || caravan.Destroyed
                || caravan.Faction != Faction.OfPlayer
                || caravan.Shuttle == null
                || caravan.Shuttle != shuttle
                || shuttle?.LaunchableComp == null
                || shuttle.TransporterComp == null)
            {
                return Cancel("BSL_ShuttleUnavailable".Translate());
            }

            if (queuedLaunch.ArrivalAction == null || !queuedLaunch.DestinationTile.Valid)
            {
                return Cancel("BSL_DestinationInvalid".Translate());
            }

            AcceptanceReport canLaunch = CaravanShuttleUtility.CanLaunchCaravanShuttle(caravan);
            if (!canLaunch.Accepted)
            {
                if (TryGetRemainingCooldownTicks(shuttle.LaunchableComp, out int ticksLeft))
                {
                    return Wait("BSL_WaitingForCooldown".Translate(ticksLeft.ToStringTicksToPeriod(true, false, true, true, false)));
                }

                return Wait(canLaunch.Reason.NullOrEmpty() ? "BSL_StatusUnavailable".Translate() : canLaunch.Reason);
            }

            int distance = Find.WorldGrid.TraversalDistanceBetween(caravan.Tile, queuedLaunch.DestinationTile, true, int.MaxValue, true);
            int maxLaunchDistance = shuttle.LaunchableComp.MaxLaunchDistanceEver(queuedLaunch.DestinationTile.Layer);
            if (maxLaunchDistance >= 0 && distance > maxLaunchDistance)
            {
                return Cancel("TransportPodDestinationBeyondMaximumRange".Translate());
            }

            if (distance > shuttle.LaunchableComp.MaxLaunchDistanceAtFuelLevel(shuttle.FuelLevel, queuedLaunch.DestinationTile.Layer))
            {
                return Wait("TransportPodNotEnoughFuel".Translate());
            }

            List<CompTransporter> transporters = new List<CompTransporter> { shuttle.TransporterComp };
            FloatMenuAcceptanceReport stillValid = queuedLaunch.ArrivalAction.StillValid(transporters, queuedLaunch.DestinationTile);
            if (!stillValid.Accepted)
            {
                string reason = stillValid.FailReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : stillValid.FailReason;
                return Cancel(reason);
            }

            return new LaunchReadinessResult(true, false, "BSL_StatusReady".Translate(), null);
        }

        private static LaunchReadinessResult Wait(string statusText)
        {
            return new LaunchReadinessResult(false, false, statusText, null);
        }

        private static LaunchReadinessResult Cancel(string reason)
        {
            return new LaunchReadinessResult(false, true, null, reason);
        }

        private static bool TryGetRemainingCooldownTicks(CompLaunchable launchable, out int ticksLeft)
        {
            ticksLeft = launchable.Props.cooldownTicks - Find.TickManager.TicksGame + launchable.lastLaunchTick;
            return launchable.Props.cooldownTicks > 0 && launchable.lastLaunchTick > 0 && ticksLeft > 0;
        }
    }
}
