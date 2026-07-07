using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public class TrackedPassengerShuttleFlight : IExposable
    {
        public Building_PassengerShuttle Shuttle;
        public Caravan Caravan;
        public PlanetTile OriginTile;
        public PlanetTile DestinationTile;
        public string OriginLabel;
        public string DestinationLabel;
        public PassengerShuttleFlightState State;
        public string StatusText;
        public int CreatedTick;
        public int LaunchedTick;
        public int ArrivedTick;

        public TrackedPassengerShuttleFlight()
        {
        }

        public TrackedPassengerShuttleFlight(QueuedPassengerShuttleLaunch queuedLaunch)
        {
            Shuttle = queuedLaunch.Shuttle;
            Caravan = queuedLaunch.Caravan;
            OriginTile = queuedLaunch.OriginTile;
            DestinationTile = queuedLaunch.DestinationTile;
            OriginLabel = queuedLaunch.OriginLabel;
            DestinationLabel = queuedLaunch.DestinationLabel;
            State = PassengerShuttleFlightState.Queued;
            StatusText = "BSL_StatusQueued".Translate();
            CreatedTick = Find.TickManager.TicksGame;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref Shuttle, "shuttle");
            Scribe_References.Look(ref Caravan, "caravan");
            Scribe_Values.Look(ref OriginTile, "originTile");
            Scribe_Values.Look(ref DestinationTile, "destinationTile");
            Scribe_Values.Look(ref OriginLabel, "originLabel");
            Scribe_Values.Look(ref DestinationLabel, "destinationLabel");
            Scribe_Values.Look(ref State, "state", PassengerShuttleFlightState.Queued);
            Scribe_Values.Look(ref StatusText, "statusText");
            Scribe_Values.Look(ref CreatedTick, "createdTick", 0);
            Scribe_Values.Look(ref LaunchedTick, "launchedTick", 0);
            Scribe_Values.Look(ref ArrivedTick, "arrivedTick", 0);
        }
    }
}
