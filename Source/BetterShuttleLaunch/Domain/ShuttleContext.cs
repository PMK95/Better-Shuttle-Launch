using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Domain
{
    public class ShuttleContext
    {
        private ShuttleContext(Building_PassengerShuttle shuttle, Caravan caravan)
        {
            Shuttle = shuttle;
            Caravan = caravan;
        }

        public Building_PassengerShuttle Shuttle { get; }
        public Caravan Caravan { get; }
        public bool IsCaravan => Caravan != null;
        public CompLaunchable Launchable => Shuttle?.LaunchableComp;
        public CompTransporter Transporter => Shuttle?.TransporterComp;
        public PlanetTile OriginTile => IsCaravan ? Caravan.Tile : Shuttle?.Tile ?? default;
        public float FuelLevel => Shuttle?.FuelLevel ?? Launchable?.FuelLevel ?? 0f;
        public Map Map => IsCaravan ? null : Shuttle?.Map;
        public MapParent MapParent => Map?.Parent;
        public string Label => Caravan?.LabelCap ?? Shuttle?.LabelCap ?? "BSL_StatusUnavailable".Translate();

        public static bool TryForMapShuttle(Building_PassengerShuttle shuttle, out ShuttleContext context, out string failReason)
        {
            context = null;
            failReason = null;
            if (!IsSupportedMapPassengerShuttle(shuttle))
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            context = new ShuttleContext(shuttle, null);
            return true;
        }

        public static bool TryForCaravan(Caravan caravan, out ShuttleContext context, out string failReason)
        {
            context = null;
            failReason = null;
            Building_PassengerShuttle shuttle = caravan?.Shuttle;
            if (caravan == null
                || caravan.Destroyed
                || caravan.Faction != Faction.OfPlayer
                || shuttle == null
                || shuttle.LaunchableComp == null
                || shuttle.TransporterComp == null
                || shuttle.ShuttleComp == null
                || !shuttle.ShuttleComp.IsPlayerShuttle)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            context = new ShuttleContext(shuttle, caravan);
            return true;
        }

        public static bool IsSupportedMapPassengerShuttle(Building_PassengerShuttle shuttle)
        {
            return shuttle != null
                   && !shuttle.Destroyed
                   && shuttle.Spawned
                   && shuttle.ShuttleComp != null
                   && shuttle.ShuttleComp.IsPlayerShuttle
                   && shuttle.LaunchableComp != null
                   && shuttle.TransporterComp != null;
        }
    }
}
