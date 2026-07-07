using System.Collections.Generic;
using BetterShuttleLaunch.Commands;
using BetterShuttleLaunch.Settings;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.ReturnHome
{
    [HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
    public static class CaravanReturnHomeCommandPatch
    {
        public static void Postfix(Caravan __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendReturnHomeCommand(__instance, __result);
        }

        private static IEnumerable<Gizmo> AppendReturnHomeCommand(Caravan caravan, IEnumerable<Gizmo> originalGizmos)
        {
            foreach (Gizmo gizmo in originalGizmos)
            {
                if (ShouldHideVanillaLaunchCommand(caravan, gizmo))
                {
                    continue;
                }

                yield return gizmo;
            }

            if (!ModsConfig.OdysseyActive || caravan == null || caravan.Faction != Faction.OfPlayer || caravan.Shuttle == null)
            {
                yield break;
            }

            yield return PassengerShuttleLaunchCommandFactory.CreateForCaravan(caravan);
        }

        private static bool ShouldHideVanillaLaunchCommand(Caravan caravan, Gizmo gizmo)
        {
            return ModsConfig.OdysseyActive
                   && BetterShuttleLaunchMod.ActiveSettings.HideVanillaLaunchCommand
                   && caravan != null
                   && caravan.Faction == Faction.OfPlayer
                   && caravan.Shuttle != null
                   && gizmo is Command command
                   && command.defaultLabel == "CommandLaunchGroup".Translate();
        }
    }
}
