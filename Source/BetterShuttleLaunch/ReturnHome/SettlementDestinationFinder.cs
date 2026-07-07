using System.Collections.Generic;
using RimWorld.Planet;

namespace BetterShuttleLaunch.ReturnHome
{
    public static class SettlementDestinationFinder
    {
        private static readonly List<MapParent> ResultBuffer = new List<MapParent>();

        public static IReadOnlyList<MapParent> FindOtherPlayerHomeMapParents(MapParent origin)
        {
            ResultBuffer.Clear();
            IReadOnlyList<MapParent> homes = HomeDestinationFinder.FindPlayerHomeMapParents();
            for (int i = 0; i < homes.Count; i++)
            {
                MapParent home = homes[i];
                if (home != null && home != origin)
                {
                    ResultBuffer.Add(home);
                }
            }

            return ResultBuffer;
        }
    }
}
