using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Domain
{
    public class LaunchRangeInfo
    {
        public readonly LaunchRangeState State;
        public readonly PlanetTile OriginTile;
        public readonly PlanetTile DestinationTile;
        public readonly int Distance;
        public readonly int MaximumRange;
        public readonly int CurrentFuelRange;
        public readonly string FailureReason;

        private LaunchRangeInfo(
            LaunchRangeState state,
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

        public bool CanSelectDestination => State == LaunchRangeState.Ready || State == LaunchRangeState.NeedsFuel;

        public bool CanLaunchWithCurrentFuel => State == LaunchRangeState.Ready;

        public string CurrentFuelRangeText => CurrentFuelRange >= 0 ? CurrentFuelRange.ToString() : "BSL_RangeUnlimited".Translate();

        public static LaunchRangeInfo ForTarget(ShuttleContext context, GlobalTargetInfo target)
        {
            if (!target.IsValid || !target.Tile.Valid)
            {
                return Invalid(context?.OriginTile ?? default, default, "BSL_DestinationInvalid".Translate());
            }

            return ForTile(context, target.Tile);
        }

        public static LaunchRangeInfo ForTile(ShuttleContext context, PlanetTile destinationTile)
        {
            if (context == null || !context.OriginTile.Valid || !destinationTile.Valid || context.Launchable == null || Find.WorldGrid == null)
            {
                return Invalid(context?.OriginTile ?? default, destinationTile, "BSL_DestinationInvalid".Translate());
            }

            int distance = Find.WorldGrid.TraversalDistanceBetween(context.OriginTile, destinationTile, true, int.MaxValue, true);
            int maximumRange = context.Launchable.MaxLaunchDistanceEver(destinationTile.Layer);
            int currentFuelRange = context.Launchable.MaxLaunchDistanceAtFuelLevel(context.FuelLevel, destinationTile.Layer);
            if (maximumRange >= 0 && distance > maximumRange)
            {
                return new LaunchRangeInfo(
                    LaunchRangeState.BeyondMaximumRange,
                    context.OriginTile,
                    destinationTile,
                    distance,
                    maximumRange,
                    currentFuelRange,
                    "TransportPodDestinationBeyondMaximumRange".Translate());
            }

            if (currentFuelRange >= 0 && distance > currentFuelRange)
            {
                return new LaunchRangeInfo(
                    LaunchRangeState.NeedsFuel,
                    context.OriginTile,
                    destinationTile,
                    distance,
                    maximumRange,
                    currentFuelRange,
                    null);
            }

            return new LaunchRangeInfo(
                LaunchRangeState.Ready,
                context.OriginTile,
                destinationTile,
                distance,
                maximumRange,
                currentFuelRange,
                null);
        }

        private static LaunchRangeInfo Invalid(PlanetTile originTile, PlanetTile destinationTile, string failureReason)
        {
            return new LaunchRangeInfo(
                LaunchRangeState.InvalidDestination,
                originTile,
                destinationTile,
                -1,
                -1,
                -1,
                failureReason);
        }
    }
}
