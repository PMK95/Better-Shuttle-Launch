using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public class QueuedPassengerShuttleLaunch : IExposable
    {
        public Building_PassengerShuttle Shuttle;
        public Caravan Caravan;
        public PlanetTile OriginTile;
        public PlanetTile DestinationTile;
        public string OriginLabel;
        public string DestinationLabel;
        public TransportersArrivalAction ArrivalAction;
        public int CreatedTick;

        public QueuedPassengerShuttleLaunch()
        {
        }

        public QueuedPassengerShuttleLaunch(
            Building_PassengerShuttle shuttle,
            PlanetTile originTile,
            PlanetTile destinationTile,
            string originLabel,
            string destinationLabel,
            TransportersArrivalAction arrivalAction)
        {
            Shuttle = shuttle;
            OriginTile = originTile;
            DestinationTile = destinationTile;
            OriginLabel = originLabel;
            DestinationLabel = destinationLabel;
            ArrivalAction = arrivalAction;
            CreatedTick = Find.TickManager.TicksGame;
        }

        public QueuedPassengerShuttleLaunch(
            Caravan caravan,
            Building_PassengerShuttle shuttle,
            PlanetTile originTile,
            PlanetTile destinationTile,
            string originLabel,
            string destinationLabel,
            TransportersArrivalAction arrivalAction)
        {
            Caravan = caravan;
            Shuttle = shuttle;
            OriginTile = originTile;
            DestinationTile = destinationTile;
            OriginLabel = originLabel;
            DestinationLabel = destinationLabel;
            ArrivalAction = arrivalAction;
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
            Scribe_Deep.Look(ref ArrivalAction, "arrivalAction");
            Scribe_Values.Look(ref CreatedTick, "createdTick", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (OriginLabel.NullOrEmpty())
                {
                    OriginLabel = OriginTile.Valid ? OriginTile.ToString() : "BSL_StatusUnavailable".Translate();
                }

                if (DestinationLabel.NullOrEmpty())
                {
                    DestinationLabel = DestinationTile.Valid ? DestinationTile.ToString() : "BSL_StatusUnavailable".Translate();
                }
            }
        }
    }
}
