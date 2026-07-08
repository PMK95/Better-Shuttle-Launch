using System.Collections.Generic;
using BetterShuttleLaunch.Domain;
using RimWorld;
using Verse;

namespace BetterShuttleLaunch.Services
{
    public static class ShuttleDiscoveryService
    {
        public static List<Building_PassengerShuttle> FindPassengerShuttles(Map map)
        {
            List<Building_PassengerShuttle> result = new List<Building_PassengerShuttle>();
            if (map == null)
            {
                return result;
            }

            List<Thing> passengerShuttles = map.listerThings.ThingsInGroup(ThingRequestGroup.PassengerShuttle);
            for (int i = 0; i < passengerShuttles.Count; i++)
            {
                if (passengerShuttles[i] is Building_PassengerShuttle passengerShuttle
                    && ShuttleContext.IsSupportedMapPassengerShuttle(passengerShuttle))
                {
                    result.Add(passengerShuttle);
                }
            }

            return result;
        }
    }
}
