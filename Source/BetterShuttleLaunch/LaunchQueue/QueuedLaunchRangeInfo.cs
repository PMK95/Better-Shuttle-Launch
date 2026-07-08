using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public class QueuedLaunchRangeInfo
    {
        public readonly QueuedLaunchRangeState State;
        public readonly PlanetTile OriginTile;
        public readonly PlanetTile DestinationTile;
        public readonly int Distance;
        public readonly int MaximumRange;
        public readonly int CurrentFuelRange;
        public readonly string FailureReason;

        private QueuedLaunchRangeInfo(
            QueuedLaunchRangeState state,
            PlanetTile originTile,
            PlanetTile destinationTile,
            int distance,
            int maximumRange,
            int currentFuelRange,
            string failureReason)
        {
            State = state;
            OriginTile = originTile;
            DestinationTile = destinationTile;
            Distance = distance;
            MaximumRange = maximumRange;
            CurrentFuelRange = currentFuelRange;
            FailureReason = failureReason;
        }

        public bool CanSelectDestination => State == QueuedLaunchRangeState.Ready || State == QueuedLaunchRangeState.NeedsFuel;

        public bool CanLaunchWithCurrentFuel => State == QueuedLaunchRangeState.Ready;

        public string CurrentFuelRangeText => CurrentFuelRange >= 0 ? CurrentFuelRange.ToString() : "BSL_RangeUnlimited".Translate();

        public static QueuedLaunchRangeInfo ForTarget(PlanetTile originTile, CompLaunchable launchable, GlobalTargetInfo target, float? fuelLevelOverride = null)
        {
            if (!target.IsValid || !target.Tile.Valid)
            {
                return Invalid(originTile, default, "BSL_DestinationInvalid".Translate());
            }

            return ForTile(originTile, launchable, target.Tile, fuelLevelOverride);
        }

        public static QueuedLaunchRangeInfo ForTile(PlanetTile originTile, CompLaunchable launchable, PlanetTile destinationTile, float? fuelLevelOverride = null)
        {
            if (!originTile.Valid || !destinationTile.Valid || launchable == null || Find.WorldGrid == null)
            {
                return Invalid(originTile, destinationTile, "BSL_DestinationInvalid".Translate());
            }

            int distance = Find.WorldGrid.TraversalDistanceBetween(originTile, destinationTile, true, int.MaxValue, true);
            int maximumRange = launchable.MaxLaunchDistanceEver(destinationTile.Layer);
            if (maximumRange >= 0 && distance > maximumRange)
            {
                return new QueuedLaunchRangeInfo(
                    QueuedLaunchRangeState.BeyondMaximumRange,
                    originTile,
                    destinationTile,
                    distance,
                    maximumRange,
                    GetCurrentFuelRange(launchable, destinationTile, fuelLevelOverride),
                    "TransportPodDestinationBeyondMaximumRange".Translate());
            }

            int currentFuelRange = GetCurrentFuelRange(launchable, destinationTile, fuelLevelOverride);
            if (currentFuelRange >= 0 && distance > currentFuelRange)
            {
                return new QueuedLaunchRangeInfo(
                    QueuedLaunchRangeState.NeedsFuel,
                    originTile,
                    destinationTile,
                    distance,
                    maximumRange,
                    currentFuelRange,
                    null);
            }

            return new QueuedLaunchRangeInfo(
                QueuedLaunchRangeState.Ready,
                originTile,
                destinationTile,
                distance,
                maximumRange,
                currentFuelRange,
                null);
        }

        private static QueuedLaunchRangeInfo Invalid(PlanetTile originTile, PlanetTile destinationTile, string failureReason)
        {
            return new QueuedLaunchRangeInfo(
                QueuedLaunchRangeState.InvalidDestination,
                originTile,
                destinationTile,
                -1,
                -1,
                -1,
                failureReason);
        }

        private static int GetCurrentFuelRange(CompLaunchable launchable, PlanetTile destinationTile, float? fuelLevelOverride)
        {
            float fuelLevel = fuelLevelOverride ?? launchable.FuelLevel;
            return launchable.MaxLaunchDistanceAtFuelLevel(fuelLevel, destinationTile.Layer);
        }
    }
}
