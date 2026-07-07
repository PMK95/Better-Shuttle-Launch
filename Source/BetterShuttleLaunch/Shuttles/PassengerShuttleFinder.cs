using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterShuttleLaunch.Shuttles
{
    public static class PassengerShuttleFinder
    {
        private static readonly List<Building_PassengerShuttle> ResultBuffer = new List<Building_PassengerShuttle>();

        public static IReadOnlyList<Building_PassengerShuttle> FindPassengerShuttles(Map map)
        {
            ResultBuffer.Clear();
            if (map == null)
            {
                return ResultBuffer;
            }

            List<Thing> passengerShuttles = map.listerThings.ThingsInGroup(ThingRequestGroup.PassengerShuttle);
            for (int i = 0; i < passengerShuttles.Count; i++)
            {
                if (passengerShuttles[i] is Building_PassengerShuttle passengerShuttle && IsSupportedPassengerShuttle(passengerShuttle))
                {
                    ResultBuffer.Add(passengerShuttle);
                }
            }

            return ResultBuffer;
        }

        public static bool IsSupportedPassengerShuttle(Thing thing)
        {
            Building_PassengerShuttle passengerShuttle = thing as Building_PassengerShuttle;
            if (passengerShuttle == null || passengerShuttle.Destroyed || !passengerShuttle.Spawned)
            {
                return false;
            }

            CompShuttle shuttle = passengerShuttle.ShuttleComp;
            return shuttle != null && shuttle.IsPlayerShuttle && passengerShuttle.LaunchableComp != null && passengerShuttle.TransporterComp != null;
        }
    }
}
