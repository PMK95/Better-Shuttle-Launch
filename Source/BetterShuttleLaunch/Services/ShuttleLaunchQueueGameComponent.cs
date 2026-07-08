using System.Collections.Generic;
using BetterShuttleLaunch.Domain;
using BetterShuttleLaunch.RimWorldApi;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Services
{
    public class ShuttleLaunchQueueGameComponent : GameComponent
    {
        private List<QueuedShuttleLaunch> queuedLaunches = new List<QueuedShuttleLaunch>();
        private List<LastDepartureRecord> departureRecords = new List<LastDepartureRecord>();

        public ShuttleLaunchQueueGameComponent(Game game)
        {
        }

        public static ShuttleLaunchQueueGameComponent Current => global::Verse.Current.Game?.GetComponent<ShuttleLaunchQueueGameComponent>();

        public IReadOnlyList<QueuedShuttleLaunch> QueuedLaunches => queuedLaunches;

        public bool HasQueuedLaunch(ShuttleContext context)
        {
            return FindQueuedLaunch(context) != null;
        }

        public QueuedShuttleLaunch FindQueuedLaunch(ShuttleContext context)
        {
            if (context == null)
            {
                return null;
            }

            return context.IsCaravan ? FindQueuedLaunch(context.Caravan) : FindQueuedLaunch(context.Shuttle);
        }

        public QueuedShuttleLaunch FindQueuedLaunch(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null)
            {
                return null;
            }

            for (int i = 0; i < queuedLaunches.Count; i++)
            {
                if (queuedLaunches[i]?.Shuttle == shuttle)
                {
                    return queuedLaunches[i];
                }
            }

            return null;
        }

        public QueuedShuttleLaunch FindQueuedLaunch(Caravan caravan)
        {
            if (caravan == null)
            {
                return null;
            }

            for (int i = 0; i < queuedLaunches.Count; i++)
            {
                if (queuedLaunches[i]?.Caravan == caravan)
                {
                    return queuedLaunches[i];
                }
            }

            return null;
        }

        public bool TryAddQueuedLaunch(QueuedShuttleLaunch queuedLaunch)
        {
            if (queuedLaunch?.Shuttle == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (FindQueuedLaunch(queuedLaunch.Shuttle) != null || (queuedLaunch.Caravan != null && FindQueuedLaunch(queuedLaunch.Caravan) != null))
            {
                Messages.Message("BSL_QueuedLaunchAlreadyExists".Translate(), queuedLaunch.Shuttle, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            queuedLaunches.Add(queuedLaunch);
            LookTargets targets = queuedLaunch.Caravan != null ? new LookTargets(queuedLaunch.Caravan) : new LookTargets(queuedLaunch.Shuttle);
            string destinationLabel = queuedLaunch.DestinationLabel.NullOrEmpty() ? queuedLaunch.DestinationTile.ToString() : queuedLaunch.DestinationLabel;
            Messages.Message("BSL_QueuedForDestination".Translate(destinationLabel), targets, MessageTypeDefOf.TaskCompletion, false);
            return true;
        }

        public void CancelQueuedLaunch(ShuttleContext context, bool showMessage)
        {
            if (context == null)
            {
                return;
            }

            if (context.IsCaravan)
            {
                CancelQueuedLaunch(context.Caravan, showMessage);
                return;
            }

            CancelQueuedLaunch(context.Shuttle, showMessage);
        }

        public void CancelQueuedLaunch(Building_PassengerShuttle shuttle, bool showMessage)
        {
            for (int i = queuedLaunches.Count - 1; i >= 0; i--)
            {
                if (queuedLaunches[i]?.Shuttle == shuttle)
                {
                    queuedLaunches.RemoveAt(i);
                    if (showMessage)
                    {
                        Messages.Message("BSL_QueuedLaunchCanceled".Translate(), shuttle, MessageTypeDefOf.NeutralEvent, false);
                    }
                }
            }
        }

        public void CancelQueuedLaunch(Caravan caravan, bool showMessage)
        {
            for (int i = queuedLaunches.Count - 1; i >= 0; i--)
            {
                if (queuedLaunches[i]?.Caravan == caravan)
                {
                    queuedLaunches.RemoveAt(i);
                    if (showMessage)
                    {
                        Messages.Message("BSL_QueuedLaunchCanceled".Translate(), caravan, MessageTypeDefOf.NeutralEvent, false);
                    }
                }
            }
        }

        public void RememberDeparture(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null || !shuttle.Spawned || shuttle.Map?.Parent == null)
            {
                return;
            }

            LastDepartureRecord record = FindDepartureRecord(shuttle);
            if (record == null)
            {
                departureRecords.Add(new LastDepartureRecord(shuttle, shuttle.Map.Parent, shuttle.Position, shuttle.Rotation));
                return;
            }

            record.MapParent = shuttle.Map.Parent;
            record.Cell = shuttle.Position;
            record.Rotation = shuttle.Rotation;
        }

        public bool TryGetLastDepartureRecord(Building_PassengerShuttle shuttle, out LastDepartureRecord record)
        {
            record = FindDepartureRecord(shuttle);
            return record != null && record.IsUsable;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref queuedLaunches, "bslQueuedShuttleLaunches", LookMode.Deep);
            Scribe_Collections.Look(ref departureRecords, "bslLastDepartureRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                queuedLaunches ??= new List<QueuedShuttleLaunch>();
                departureRecords ??= new List<LastDepartureRecord>();
                queuedLaunches.RemoveAll(queuedLaunch => queuedLaunch == null);
                RemoveInvalidDepartureRecords();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 60 != 0)
            {
                return;
            }

            for (int i = queuedLaunches.Count - 1; i >= 0; i--)
            {
                ProcessQueuedLaunch(queuedLaunches[i], i);
            }
        }

        private LastDepartureRecord FindDepartureRecord(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null)
            {
                return null;
            }

            for (int i = 0; i < departureRecords.Count; i++)
            {
                if (departureRecords[i]?.Shuttle == shuttle)
                {
                    return departureRecords[i];
                }
            }

            return null;
        }

        private void ProcessQueuedLaunch(QueuedShuttleLaunch queuedLaunch, int index)
        {
            LaunchReadiness readiness = LaunchReadinessService.Evaluate(queuedLaunch);
            if (readiness.ShouldCancel)
            {
                CancelQueuedLaunchAt(index, queuedLaunch, readiness.FailureReason);
                return;
            }

            if (!readiness.CanLaunchNow)
            {
                return;
            }

            if (!PassengerShuttleLaunchApi.TryLaunchImmediately(queuedLaunch, out string failReason))
            {
                CancelQueuedLaunchAt(index, queuedLaunch, failReason);
                return;
            }

            queuedLaunches.RemoveAt(index);
        }

        private void CancelQueuedLaunchAt(int index, QueuedShuttleLaunch queuedLaunch, string reason)
        {
            queuedLaunches.RemoveAt(index);
            string failureReason = reason.NullOrEmpty() ? "BSL_StatusUnavailable".Translate() : reason;
            if (queuedLaunch?.Caravan != null)
            {
                Messages.Message("BSL_AutoLaunchFailed".Translate(failureReason), new LookTargets(queuedLaunch.Caravan), MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            if (queuedLaunch?.Shuttle != null)
            {
                Messages.Message("BSL_AutoLaunchFailed".Translate(failureReason), new LookTargets(queuedLaunch.Shuttle), MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            Messages.Message("BSL_AutoLaunchFailed".Translate(failureReason), MessageTypeDefOf.NegativeEvent, false);
        }

        private void RemoveInvalidDepartureRecords()
        {
            departureRecords.RemoveAll(record => record == null || record.Shuttle == null || record.MapParent == null || !record.Cell.IsValid);
        }
    }
}
