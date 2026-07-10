using HarmonyLib;
using Verse;

namespace BetterShuttleLaunch.Bootstrap
{
    [StaticConstructorOnStartup]
    public static class BetterShuttleLaunchBootstrap
    {
        static BetterShuttleLaunchBootstrap()
        {
            new Harmony("bakacandy.bettershuttlelaunch").PatchAll();
        }
    }
}
