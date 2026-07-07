using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.ReturnHome
{
    public struct LastDepartureLocation
    {
        public MapParent MapParent;
        public IntVec3 Cell;
        public Rot4 Rotation;

        public LastDepartureLocation(MapParent mapParent, IntVec3 cell, Rot4 rotation)
        {
            MapParent = mapParent;
            Cell = cell;
            Rotation = rotation;
        }

        public bool IsUsableFor(MapParent mapParent)
        {
            return mapParent != null
                   && MapParent == mapParent
                   && mapParent.Spawned
                   && mapParent.HasMap
                   && Cell.IsValid
                   && Cell.InBounds(mapParent.Map);
        }
    }
}
