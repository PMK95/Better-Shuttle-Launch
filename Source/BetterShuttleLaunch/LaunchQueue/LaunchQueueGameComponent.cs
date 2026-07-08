using System.Collections.Generic;
using BetterShuttleLaunch.ReturnHome;
using BetterShuttleLaunch.Shuttles;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public class LaunchQueueGameComponent : GameComponent
    {
        private List<QueuedPassengerShuttleLaunch> queuedLaunches = new List<QueuedPassengerShuttleLaunch>();
        private List<Building_PassengerShuttle> departureShuttles = new List<Building_PassengerShuttle>();
        private List<MapParent> departureMapParents = new List<MapParent>();
        private List<IntVec3> departureCells = new List<IntVec3>();
        private List<Rot4> departureRotations = new List<Rot4>();

        public LaunchQueueGameComponent(Game game)
        {
        }

        public static LaunchQueueGameComponent Current => global::Verse.Current.Game?.GetComponent<LaunchQueueGameComponent>();

        public IReadOnlyList<QueuedPassengerShuttleLaunch> QueuedLaunches => queuedLaunches;

        public bool IsQueued(Building_PassengerShuttle shuttle)
        {
            return FindQueuedLaunch(shuttle) != null;
        }

        public QueuedPassengerShuttleLaunch FindQueuedLaunch(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null)
            {
                return null;
            }

            for (int i = 0; i < queuedLaunches.Count; i++)
            {
                if (queuedLaunches[i].Shuttle == shuttle)
                {
                    return queuedLaunches[i];
                }
            }

            return null;
        }

        public bool IsQueued(Caravan caravan)
        {
            return FindQueuedLaunch(caravan) != null;
        }

        public QueuedPassengerShuttleLaunch FindQueuedLaunch(Caravan caravan)
        {
            if (caravan == null)
            {
                return null;
            }

            for (int i = 0; i < queuedLaunches.Count; i++)
            {
                if (queuedLaunches[i].Caravan == caravan)
                {
                    return queuedLaunches[i];
                }
            }

            return null;
        }

        public void AddQueuedLaunchIfNoExistingQueue(QueuedPassengerShuttleLaunch queuedLaunch)
        {
            if (queuedLaunch?.Shuttle == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (FindQueuedLaunch(queuedLaunch.Shuttle) != null || (queuedLaunch.Caravan != null && FindQueuedLaunch(queuedLaunch.Caravan) != null))
            {
                Messages.Message("BSL_QueuedLaunchAlreadyExists".Translate(), queuedLaunch.Shuttle, MessageTypeDefOf.RejectInput, false);
                return;
            }

            queuedLaunches.Add(queuedLaunch);
            LookTargets targets = queuedLaunch.Caravan != null ? new LookTargets(queuedLaunch.Caravan) : new LookTargets(queuedLaunch.Shuttle);
            string destinationLabel = queuedLaunch.DestinationLabel.NullOrEmpty() ? queuedLaunch.DestinationTile.ToString() : queuedLaunch.DestinationLabel;
            Messages.Message("BSL_QueuedForDestination".Translate(destinationLabel), targets, MessageTypeDefOf.TaskCompletion, false);
        }

        public void RemoveQueuedLaunch(Building_PassengerShuttle shuttle, bool showMessage)
        {
            for (int i = queuedLaunches.Count - 1; i >= 0; i--)
            {
                if (queuedLaunches[i].Shuttle == shuttle)
                {
                    queuedLaunches.RemoveAt(i);
                    if (showMessage)
                    {
                        Messages.Message("BSL_QueuedLaunchCanceled".Translate(), shuttle, MessageTypeDefOf.NeutralEvent, false);
                    }
                }
            }
        }

        public void RemoveQueuedLaunch(Caravan caravan, bool showMessage)
        {
            for (int i = queuedLaunches.Count - 1; i >= 0; i--)
            {
                if (queuedLaunches[i].Caravan == caravan)
                {
                    queuedLaunches.RemoveAt(i);
                    if (showMessage)
                    {
                        Messages.Message("BSL_QueuedLaunchCanceled".Translate(), caravan, MessageTypeDefOf.NeutralEvent, false);
                    }
                }
            }
        }

        public void RememberPassengerShuttleDepartureLocation(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null || !shuttle.Spawned || shuttle.Map == null || shuttle.Map.Parent == null)
            {
                return;
            }

            int index = departureShuttles.IndexOf(shuttle);
            if (index < 0)
            {
                departureShuttles.Add(shuttle);
                departureMapParents.Add(shuttle.Map.Parent);
                departureCells.Add(shuttle.Position);
                departureRotations.Add(shuttle.Rotation);
                return;
            }

            departureMapParents[index] = shuttle.Map.Parent;
            departureCells[index] = shuttle.Position;
            departureRotations[index] = shuttle.Rotation;
        }

        public bool TryGetLastDepartureLocation(Building_PassengerShuttle shuttle, out LastDepartureLocation location)
        {
            location = default;
            int index = departureShuttles.IndexOf(shuttle);
            if (index < 0 || index >= departureMapParents.Count || index >= departureCells.Count || index >= departureRotations.Count)
            {
                return false;
            }

            location = new LastDepartureLocation(departureMapParents[index], departureCells[index], departureRotations[index]);
            return location.MapParent != null && location.Cell.IsValid;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref queuedLaunches, "queuedPassengerShuttleLaunches", LookMode.Deep);
            Scribe_Collections.Look(ref departureShuttles, "departureShuttles", LookMode.Reference);
            Scribe_Collections.Look(ref departureMapParents, "departureMapParents", LookMode.Reference);
            Scribe_Collections.Look(ref departureCells, "departureCells", LookMode.Value);
            Scribe_Collections.Look(ref departureRotations, "departureRotations", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                queuedLaunches ??= new List<QueuedPassengerShuttleLaunch>();
                queuedLaunches.RemoveAll(queuedLaunch => queuedLaunch == null);
                departureShuttles ??= new List<Building_PassengerShuttle>();
                departureMapParents ??= new List<MapParent>();
                departureCells ??= new List<IntVec3>();
                departureRotations ??= new List<Rot4>();
                RemoveInvalidDepartureLocations();
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

        private void ProcessQueuedLaunch(QueuedPassengerShuttleLaunch queuedLaunch, int index)
        {
            LaunchReadinessResult readiness = LaunchReadinessEvaluator.EvaluateQueuedPassengerShuttleLaunch(queuedLaunch);
            if (readiness.ShouldCancel)
            {
                CancelQueuedLaunchAt(index, queuedLaunch, readiness.FailureReason);
                return;
            }

            if (!readiness.CanLaunchNow)
            {
                return;
            }

            if (queuedLaunch.Caravan != null)
            {
                if (!PassengerShuttleLaunchBridge.TryLaunchCaravanImmediately(queuedLaunch.Caravan, queuedLaunch.DestinationTile, queuedLaunch.ArrivalAction, out string caravanFailReason))
                {
                    CancelQueuedLaunchAt(index, queuedLaunch, caravanFailReason);
                    return;
                }

                queuedLaunches.RemoveAt(index);
                return;
            }

            if (!PassengerShuttleLaunchBridge.TryLaunchImmediately(queuedLaunch.Shuttle, queuedLaunch.DestinationTile, queuedLaunch.ArrivalAction, out string failReason))
            {
                CancelQueuedLaunchAt(index, queuedLaunch, failReason);
                return;
            }

            queuedLaunches.RemoveAt(index);
        }

        private void CancelQueuedLaunchAt(int index, QueuedPassengerShuttleLaunch queuedLaunch, string reason)
        {
            queuedLaunches.RemoveAt(index);
            string failureReason = reason.NullOrEmpty() ? "BSL_StatusUnavailable".Translate() : reason;
            if (queuedLaunch?.Caravan != null)
            {
                Messages.Message("BSL_AutoLaunchFailed".Translate(failureReason), new LookTargets(queuedLaunch.Caravan), MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            if (queuedLaunch?.Shuttle == null)
            {
                Messages.Message("BSL_AutoLaunchFailed".Translate(failureReason), MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            Messages.Message("BSL_AutoLaunchFailed".Translate(failureReason), new LookTargets(queuedLaunch.Shuttle), MessageTypeDefOf.NegativeEvent, false);
        }

        private void RemoveInvalidDepartureLocations()
        {
            int count = System.Math.Min(
                System.Math.Min(departureShuttles.Count, departureMapParents.Count),
                System.Math.Min(departureCells.Count, departureRotations.Count));
            for (int i = departureShuttles.Count - 1; i >= count; i--)
            {
                departureShuttles.RemoveAt(i);
            }

            for (int i = departureMapParents.Count - 1; i >= count; i--)
            {
                departureMapParents.RemoveAt(i);
            }

            for (int i = departureCells.Count - 1; i >= count; i--)
            {
                departureCells.RemoveAt(i);
            }

            for (int i = departureRotations.Count - 1; i >= count; i--)
            {
                departureRotations.RemoveAt(i);
            }

            for (int i = count - 1; i >= 0; i--)
            {
                if (departureShuttles[i] == null || departureMapParents[i] == null || !departureCells[i].IsValid)
                {
                    departureShuttles.RemoveAt(i);
                    departureMapParents.RemoveAt(i);
                    departureCells.RemoveAt(i);
                    departureRotations.RemoveAt(i);
                }
            }
        }
    }
}
