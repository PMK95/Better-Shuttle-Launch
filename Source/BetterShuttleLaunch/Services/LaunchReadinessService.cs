using System.Collections.Generic;
using BetterShuttleLaunch.Domain;
using BetterShuttleLaunch.RimWorldApi;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Services
{
    public static class LaunchReadinessService
    {
        public static LaunchReadiness Evaluate(QueuedShuttleLaunch queuedLaunch)
        {
            if (!PassengerShuttleLaunchApi.TryGetContext(queuedLaunch, out ShuttleContext context, out string failReason))
            {
                return Cancel(failReason);
            }

            if (queuedLaunch.ArrivalAction == null || !queuedLaunch.DestinationTile.Valid)
            {
                return Cancel("BSL_DestinationInvalid".Translate());
            }

            if (!context.IsCaravan)
            {
                CompTransporter transporter = context.Transporter;
                if (transporter == null || !transporter.LoadingInProgressOrReadyToLaunch)
                {
                    return Cancel("BSL_LoadingCanceled".Translate());
                }

                if (transporter.AnyInGroupHasAnythingLeftToLoad)
                {
                    return Wait("BSL_WaitingForLoading".Translate());
                }

                AcceptanceReport canLaunch = context.Launchable.CanLaunch();
                if (!canLaunch.Accepted)
                {
                    return WaitForLaunchRequirement(context.Launchable, canLaunch);
                }
            }
            else
            {
                AcceptanceReport canLaunch = CaravanShuttleUtility.CanLaunchCaravanShuttle(context.Caravan);
                if (!canLaunch.Accepted)
                {
                    return WaitForLaunchRequirement(context.Launchable, canLaunch);
                }
            }

            LaunchRangeInfo rangeInfo = LaunchRangeInfo.ForTile(context, queuedLaunch.DestinationTile);
            if (!rangeInfo.CanSelectDestination)
            {
                return Cancel(rangeInfo.FailureReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : rangeInfo.FailureReason);
            }

            if (!rangeInfo.CanLaunchWithCurrentFuel)
            {
                return Wait("BSL_WaitingForFuel".Translate(rangeInfo.Distance.ToString(), rangeInfo.CurrentFuelRangeText));
            }

            if (!PassengerShuttleLaunchApi.TryGetTransportersForArrival(context, out List<CompTransporter> transporters, out failReason))
            {
                return Cancel(failReason);
            }

            FloatMenuAcceptanceReport stillValid = queuedLaunch.ArrivalAction.StillValid(transporters, queuedLaunch.DestinationTile);
            if (!stillValid.Accepted)
            {
                return Cancel(stillValid.FailReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : stillValid.FailReason);
            }

            if (queuedLaunch.ArrivalAction is TransportersArrivalAction_TransportShip transportShipArrivalAction
                && transportShipArrivalAction.mapParent is Settlement settlement)
            {
                if (transportShipArrivalAction.transportShip == null || transportShipArrivalAction.transportShip.Disposed)
                {
                    return Cancel("BSL_DestinationInvalid".Translate());
                }

                FloatMenuAcceptanceReport canAttack = TransportersArrivalAction_AttackSettlement.CanAttack(transporters, settlement);
                if (!canAttack.Accepted)
                {
                    return Cancel(canAttack.FailReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : canAttack.FailReason);
                }
            }

            return new LaunchReadiness(true, false, "BSL_StatusReady".Translate(), null);
        }

        private static LaunchReadiness WaitForLaunchRequirement(CompLaunchable launchable, AcceptanceReport canLaunch)
        {
            if (TryGetRemainingCooldownTicks(launchable, out int ticksLeft))
            {
                return Wait("BSL_WaitingForCooldown".Translate(ticksLeft.ToStringTicksToPeriod(true, false, true, true, false)));
            }

            return Wait(canLaunch.Reason.NullOrEmpty() ? "BSL_StatusUnavailable".Translate() : canLaunch.Reason);
        }

        private static LaunchReadiness Wait(string statusText)
        {
            return new LaunchReadiness(false, false, statusText, null);
        }

        private static LaunchReadiness Cancel(string reason)
        {
            return new LaunchReadiness(false, true, null, reason);
        }

        private static bool TryGetRemainingCooldownTicks(CompLaunchable launchable, out int ticksLeft)
        {
            ticksLeft = launchable.Props.cooldownTicks - Find.TickManager.TicksGame + launchable.lastLaunchTick;
            return launchable.Props.cooldownTicks > 0 && launchable.lastLaunchTick > 0 && ticksLeft > 0;
        }
    }
}
