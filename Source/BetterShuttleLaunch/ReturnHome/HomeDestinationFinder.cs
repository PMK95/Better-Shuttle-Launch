using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.ReturnHome
{
    public static class HomeDestinationFinder
    {
        private static readonly List<MapParent> ResultBuffer = new List<MapParent>();

        public static IReadOnlyList<MapParent> FindPlayerHomeMapParents()
        {
            ResultBuffer.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                MapParent parent = map?.Parent;
                if (parent != null && parent.Spawned && parent.HasMap && parent.Faction == Faction.OfPlayer && map.IsPlayerHome)
                {
                    ResultBuffer.Add(parent);
                }
            }

            return ResultBuffer;
        }
    }
}
