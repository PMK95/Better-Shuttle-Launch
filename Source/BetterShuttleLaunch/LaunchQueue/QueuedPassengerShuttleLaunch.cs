using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public class QueuedPassengerShuttleLaunch : IExposable
    {
        public Building_PassengerShuttle Shuttle;
        public Caravan Caravan;
        public PlanetTile DestinationTile;
        public TransportersArrivalAction ArrivalAction;
        public int CreatedTick;

        public QueuedPassengerShuttleLaunch()
        {
        }

        public QueuedPassengerShuttleLaunch(Building_PassengerShuttle shuttle, PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            Shuttle = shuttle;
            DestinationTile = destinationTile;
            ArrivalAction = arrivalAction;
            CreatedTick = Find.TickManager.TicksGame;
        }

        public QueuedPassengerShuttleLaunch(Caravan caravan, Building_PassengerShuttle shuttle, PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            Caravan = caravan;
            Shuttle = shuttle;
            DestinationTile = destinationTile;
            ArrivalAction = arrivalAction;
            CreatedTick = Find.TickManager.TicksGame;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref Shuttle, "shuttle");
            Scribe_References.Look(ref Caravan, "caravan");
            Scribe_Values.Look(ref DestinationTile, "destinationTile");
            Scribe_Deep.Look(ref ArrivalAction, "arrivalAction");
            Scribe_Values.Look(ref CreatedTick, "createdTick", 0);
        }
    }
}
