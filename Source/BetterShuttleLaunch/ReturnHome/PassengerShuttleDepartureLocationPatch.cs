using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.Shuttles;
using HarmonyLib;
using RimWorld;

namespace BetterShuttleLaunch.ReturnHome
{
    [HarmonyPatch(typeof(CompLaunchable), nameof(CompLaunchable.TryLaunch))]
    public static class PassengerShuttleDepartureLocationPatch
    {
        public static void Prefix(CompLaunchable __instance)
        {
            if (__instance?.parent is Building_PassengerShuttle shuttle && PassengerShuttleFinder.IsSupportedPassengerShuttle(shuttle))
            {
                LaunchQueueGameComponent.Current?.RememberPassengerShuttleDepartureLocation(shuttle);
            }
        }
    }
}
