using System.Collections.Generic;
using BetterShuttleLaunch.ReturnHome;
using BetterShuttleLaunch.Settings;
using BetterShuttleLaunch.Shuttles;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public class LaunchQueueGameComponent : GameComponent
    {
        private List<QueuedPassengerShuttleLaunch> queuedLaunches = new List<QueuedPassengerShuttleLaunch>();
        private List<TrackedPassengerShuttleFlight> trackedFlights = new List<TrackedPassengerShuttleFlight>();
        private List<Building_PassengerShuttle> departureShuttles = new List<Building_PassengerShuttle>();
        private List<MapParent> departureMapParents = new List<MapParent>();
        private List<IntVec3> departureCells = new List<IntVec3>();
        private List<Rot4> departureRotations = new List<Rot4>();

        public LaunchQueueGameComponent(Game game)
        {
        }

        public static LaunchQueueGameComponent Current => global::Verse.Current.Game?.GetComponent<LaunchQueueGameComponent>();

        public IReadOnlyList<QueuedPassengerShuttleLaunch> QueuedLaunches => queuedLaunches;

        public IReadOnlyList<TrackedPassengerShuttleFlight> TrackedFlights => trackedFlights;

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

        public TrackedPassengerShuttleFlight FindTrackedFlight(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null)
            {
                return null;
            }

            for (int i = 0; i < trackedFlights.Count; i++)
            {
                if (trackedFlights[i].Shuttle == shuttle)
                {
                    return trackedFlights[i];
                }
            }

            return null;
        }

        public void AddOrReplaceQueuedLaunch(QueuedPassengerShuttleLaunch queuedLaunch)
        {
            if (queuedLaunch?.Shuttle == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (queuedLaunch.Caravan != null)
            {
                RemoveQueuedLaunch(queuedLaunch.Shuttle, false);
                RemoveQueuedLaunch(queuedLaunch.Caravan, false);
            }
            else
            {
                RemoveQueuedLaunch(queuedLaunch.Shuttle, false);
            }

            queuedLaunches.Add(queuedLaunch);
            AddOrReplaceTrackedFlight(queuedLaunch);
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
                    RemoveTrackedFlight(shuttle);
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
                    Building_PassengerShuttle shuttle = queuedLaunches[i].Shuttle;
                    queuedLaunches.RemoveAt(i);
                    RemoveTrackedFlight(shuttle);
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
            Scribe_Collections.Look(ref trackedFlights, "trackedPassengerShuttleFlights", LookMode.Deep);
            Scribe_Collections.Look(ref departureShuttles, "departureShuttles", LookMode.Reference);
            Scribe_Collections.Look(ref departureMapParents, "departureMapParents", LookMode.Reference);
            Scribe_Collections.Look(ref departureCells, "departureCells", LookMode.Value);
            Scribe_Collections.Look(ref departureRotations, "departureRotations", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                queuedLaunches ??= new List<QueuedPassengerShuttleLaunch>();
                queuedLaunches.RemoveAll(queuedLaunch => queuedLaunch == null);
                trackedFlights ??= new List<TrackedPassengerShuttleFlight>();
                trackedFlights.RemoveAll(trackedFlight => trackedFlight == null || trackedFlight.Shuttle == null);
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

            UpdateTrackedFlights();
        }

        private void ProcessQueuedLaunch(QueuedPassengerShuttleLaunch queuedLaunch, int index)
        {
            LaunchReadinessResult readiness = LaunchReadinessEvaluator.EvaluateQueuedPassengerShuttleLaunch(queuedLaunch);
            UpdateTrackedFlightFromQueuedLaunch(queuedLaunch, readiness);
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

                MarkTrackedFlightInFlight(queuedLaunch);
                queuedLaunches.RemoveAt(index);
                return;
            }

            if (!PassengerShuttleLaunchBridge.TryLaunchImmediately(queuedLaunch.Shuttle, queuedLaunch.DestinationTile, queuedLaunch.ArrivalAction, out string failReason))
            {
                CancelQueuedLaunchAt(index, queuedLaunch, failReason);
                return;
            }

            MarkTrackedFlightInFlight(queuedLaunch);
            queuedLaunches.RemoveAt(index);
        }

        private void CancelQueuedLaunchAt(int index, QueuedPassengerShuttleLaunch queuedLaunch, string reason)
        {
            queuedLaunches.RemoveAt(index);
            MarkTrackedFlightFailed(queuedLaunch, reason);
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

        private void AddOrReplaceTrackedFlight(QueuedPassengerShuttleLaunch queuedLaunch)
        {
            TrackedPassengerShuttleFlight trackedFlight = FindTrackedFlight(queuedLaunch.Shuttle);
            if (trackedFlight == null)
            {
                trackedFlights.Add(new TrackedPassengerShuttleFlight(queuedLaunch));
                return;
            }

            trackedFlight.Caravan = queuedLaunch.Caravan;
            trackedFlight.OriginTile = queuedLaunch.OriginTile;
            trackedFlight.DestinationTile = queuedLaunch.DestinationTile;
            trackedFlight.OriginLabel = queuedLaunch.OriginLabel;
            trackedFlight.DestinationLabel = queuedLaunch.DestinationLabel;
            trackedFlight.State = PassengerShuttleFlightState.Queued;
            trackedFlight.StatusText = "BSL_StatusQueued".Translate();
            trackedFlight.CreatedTick = Find.TickManager.TicksGame;
            trackedFlight.LaunchedTick = 0;
            trackedFlight.ArrivedTick = 0;
        }

        private void RemoveTrackedFlight(Building_PassengerShuttle shuttle)
        {
            for (int i = trackedFlights.Count - 1; i >= 0; i--)
            {
                if (trackedFlights[i].Shuttle == shuttle)
                {
                    trackedFlights.RemoveAt(i);
                }
            }
        }

        private void UpdateTrackedFlightFromQueuedLaunch(QueuedPassengerShuttleLaunch queuedLaunch, LaunchReadinessResult readiness)
        {
            TrackedPassengerShuttleFlight trackedFlight = FindTrackedFlight(queuedLaunch.Shuttle);
            if (trackedFlight == null)
            {
                return;
            }

            trackedFlight.StatusText = readiness.StatusText.NullOrEmpty() ? "BSL_StatusQueued".Translate() : readiness.StatusText;
            if (readiness.CanLaunchNow)
            {
                trackedFlight.State = PassengerShuttleFlightState.Ready;
                return;
            }

            if (queuedLaunch.Shuttle?.TransporterComp != null && queuedLaunch.Shuttle.TransporterComp.AnyInGroupHasAnythingLeftToLoad)
            {
                trackedFlight.State = PassengerShuttleFlightState.Loading;
                return;
            }

            trackedFlight.State = PassengerShuttleFlightState.Waiting;
        }

        private void MarkTrackedFlightInFlight(QueuedPassengerShuttleLaunch queuedLaunch)
        {
            TrackedPassengerShuttleFlight trackedFlight = FindTrackedFlight(queuedLaunch.Shuttle);
            if (trackedFlight == null)
            {
                trackedFlights.Add(new TrackedPassengerShuttleFlight(queuedLaunch));
                trackedFlight = trackedFlights[trackedFlights.Count - 1];
            }

            trackedFlight.State = PassengerShuttleFlightState.InFlight;
            trackedFlight.StatusText = "BSL_StatusInFlight".Translate();
            trackedFlight.LaunchedTick = Find.TickManager.TicksGame;
        }

        private void MarkTrackedFlightFailed(QueuedPassengerShuttleLaunch queuedLaunch, string reason)
        {
            if (queuedLaunch?.Shuttle == null)
            {
                return;
            }

            TrackedPassengerShuttleFlight trackedFlight = FindTrackedFlight(queuedLaunch.Shuttle);
            if (trackedFlight == null)
            {
                trackedFlights.Add(new TrackedPassengerShuttleFlight(queuedLaunch));
                trackedFlight = trackedFlights[trackedFlights.Count - 1];
            }

            trackedFlight.State = PassengerShuttleFlightState.Failed;
            trackedFlight.StatusText = reason.NullOrEmpty() ? "BSL_StatusUnavailable".Translate() : reason;
            trackedFlight.ArrivedTick = Find.TickManager.TicksGame;
        }

        private void UpdateTrackedFlights()
        {
            for (int i = trackedFlights.Count - 1; i >= 0; i--)
            {
                TrackedPassengerShuttleFlight trackedFlight = trackedFlights[i];
                if (trackedFlight?.Shuttle == null || trackedFlight.Shuttle.Destroyed)
                {
                    trackedFlights.RemoveAt(i);
                    continue;
                }

                if (trackedFlight.State == PassengerShuttleFlightState.InFlight && HasTrackedFlightArrived(trackedFlight))
                {
                    trackedFlight.State = PassengerShuttleFlightState.Arrived;
                    trackedFlight.StatusText = "BSL_StatusArrived".Translate();
                    trackedFlight.ArrivedTick = Find.TickManager.TicksGame;
                    ApplyArrivalOptionsOnce(trackedFlight);
                    continue;
                }

                if ((trackedFlight.State == PassengerShuttleFlightState.Arrived || trackedFlight.State == PassengerShuttleFlightState.Failed)
                    && trackedFlight.ArrivedTick > 0
                    && Find.TickManager.TicksGame - trackedFlight.ArrivedTick > 3600)
                {
                    trackedFlights.RemoveAt(i);
                }
            }
        }

        private static bool HasTrackedFlightArrived(TrackedPassengerShuttleFlight trackedFlight)
        {
            MapParent mapParent = trackedFlight.Shuttle.Map?.Parent;
            return trackedFlight.DestinationTile.Valid
                   && trackedFlight.Shuttle.Spawned
                   && mapParent != null
                   && mapParent.Tile == trackedFlight.DestinationTile;
        }

        private static void ApplyArrivalOptionsOnce(TrackedPassengerShuttleFlight trackedFlight)
        {
            BetterShuttleLaunchSettings settings = BetterShuttleLaunchMod.ActiveSettings;
            if (settings.PauseOnShuttleArrival)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            }

            if (settings.FocusOnShuttleArrival)
            {
                JumpToArrivedShuttleOrDestination(trackedFlight);
            }
        }

        private static void JumpToArrivedShuttleOrDestination(TrackedPassengerShuttleFlight trackedFlight)
        {
            Building_PassengerShuttle shuttle = trackedFlight?.Shuttle;
            if (shuttle != null && !shuttle.Destroyed && shuttle.Spawned)
            {
                CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(shuttle), CameraJumper.MovementMode.Pan);
                return;
            }

            PlanetTile destinationTile = trackedFlight?.DestinationTile ?? default;
            if (!destinationTile.Valid || Find.WorldObjects == null)
            {
                return;
            }

            MapParent mapParent = Find.WorldObjects.MapParentAt(destinationTile);
            if (mapParent != null && !mapParent.Destroyed && mapParent.HasMap)
            {
                CameraJumper.TryJump(mapParent.Map.Center, mapParent.Map, CameraJumper.MovementMode.Pan);
                return;
            }

            WorldObject worldObject = FindWorldObjectAtTile(destinationTile);
            if (worldObject != null && !worldObject.Destroyed)
            {
                CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(worldObject), CameraJumper.MovementMode.Pan);
                return;
            }

            CameraJumper.TryJump(destinationTile, CameraJumper.MovementMode.Pan);
            Find.WorldSelector.ClearSelection();
            Find.WorldSelector.SelectedTile = destinationTile;
        }

        private static WorldObject FindWorldObjectAtTile(PlanetTile tile)
        {
            if (!tile.Valid || Find.WorldObjects == null)
            {
                return null;
            }

            foreach (WorldObject worldObject in Find.WorldObjects.ObjectsAt(tile))
            {
                if (worldObject != null && !worldObject.Destroyed)
                {
                    return worldObject;
                }
            }

            return null;
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
