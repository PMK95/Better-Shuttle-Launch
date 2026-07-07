using System.Collections.Generic;
using BetterShuttleLaunch.Commands;
using BetterShuttleLaunch.Shuttles;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.WorldMap
{
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.GetGizmos))]
    public static class WorldMapPassengerShuttleCommandPatch
    {
        public static void Postfix(MapParent __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendLaunchShuttleCommand(__instance, __result);
        }

        private static IEnumerable<Gizmo> AppendLaunchShuttleCommand(MapParent mapParent, IEnumerable<Gizmo> originalGizmos)
        {
            foreach (Gizmo gizmo in originalGizmos)
            {
                yield return gizmo;
            }

            if (!ShouldShowCommand(mapParent))
            {
                yield break;
            }

            List<Building_PassengerShuttle> shuttles = new List<Building_PassengerShuttle>(PassengerShuttleFinder.FindPassengerShuttles(mapParent.Map));
            if (shuttles.Count == 0)
            {
                yield break;
            }

            yield return PassengerShuttleLaunchCommandFactory.CreateForMapParent(shuttles);
        }

        private static bool ShouldShowCommand(MapParent mapParent)
        {
            return ModsConfig.OdysseyActive
                   && mapParent != null
                   && mapParent.HasMap;
        }
    }
}
