using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Services
{
    public static class SettlementDestinationService
    {
        public static List<MapParent> FindPlayerHomeMapParents()
        {
            List<MapParent> result = new List<MapParent>();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                MapParent parent = map?.Parent;
                if (parent != null && parent.Spawned && parent.HasMap && parent.Faction == Faction.OfPlayer && map.IsPlayerHome)
                {
                    result.Add(parent);
                }
            }

            return result;
        }

        public static List<MapParent> FindOtherPlayerHomeMapParents(MapParent origin)
        {
            List<MapParent> result = FindPlayerHomeMapParents();
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (result[i] == null || result[i] == origin)
                {
                    result.RemoveAt(i);
                }
            }

            return result;
        }
    }
}
