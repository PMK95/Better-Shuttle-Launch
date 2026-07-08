using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Domain
{
    public class LastDepartureRecord : IExposable
    {
        public Building_PassengerShuttle Shuttle;
        public MapParent MapParent;
        public IntVec3 Cell;
        public Rot4 Rotation;

        public LastDepartureRecord()
        {
        }

        public LastDepartureRecord(Building_PassengerShuttle shuttle, MapParent mapParent, IntVec3 cell, Rot4 rotation)
        {
            Shuttle = shuttle;
            MapParent = mapParent;
            Cell = cell;
            Rotation = rotation;
        }

        public bool IsUsable => Shuttle != null
                                && MapParent != null
                                && MapParent.Spawned
                                && MapParent.HasMap
                                && Cell.IsValid
                                && Cell.InBounds(MapParent.Map);

        public void ExposeData()
        {
            Scribe_References.Look(ref Shuttle, "shuttle");
            Scribe_References.Look(ref MapParent, "mapParent");
            Scribe_Values.Look(ref Cell, "cell");
            Scribe_Values.Look(ref Rotation, "rotation");
        }
    }
}
