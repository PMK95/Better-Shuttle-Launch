using HarmonyLib;
using Verse;

namespace BetterShuttleLaunch.Bootstrap
{
    [StaticConstructorOnStartup]
    public static class BetterShuttleLaunchBootstrap
    {
        static BetterShuttleLaunchBootstrap()
        {
            new Harmony("bakacandy.BetterShuttleLaunch").PatchAll();
        }
    }
}
