using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.ReturnHome;
using BetterShuttleLaunch.Settings;
using BetterShuttleLaunch.Shuttles;
using BetterShuttleLaunch.UI;
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
                if (ShouldHideSettlementJumpCommand(mapParent, gizmo))
                {
                    continue;
                }

                yield return gizmo;
            }

            if (!ShouldShowCommand(mapParent))
            {
                yield break;
            }

            List<Building_PassengerShuttle> shuttles = new List<Building_PassengerShuttle>(PassengerShuttleFinder.FindPassengerShuttles(mapParent.Map));
            bool launchCommandIsLaunchWhenReady = shuttles.Count == 1
                                                   && !PassengerShuttleLaunchBridge.CanStartStateAwareWorldMapLaunch(shuttles[0], out _)
                                                   && PassengerShuttleLaunchQueueCommandUtility.CanQueueLaunchWhenReady(shuttles[0], out _);

            Command_Action command = launchCommandIsLaunchWhenReady ? CreateLaunchWhenReadyCommand(shuttles) : CreateLaunchCommand(shuttles);
            yield return command;

            if (!launchCommandIsLaunchWhenReady)
            {
                yield return CreateLaunchWhenReadyCommand(shuttles);
            }

            if (ShouldShowReturnHomeCommand(mapParent))
            {
                yield return CreateReturnHomeCommand(shuttles);
            }
        }

        private static Command_Action CreateLaunchCommand(List<Building_PassengerShuttle> shuttles)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "BSL_LaunchShuttle".Translate(),
                defaultDesc = "BSL_LaunchShuttleDesc".Translate(),
                icon = CompLaunchable.LaunchCommandTex,
                action = () => LaunchFromWorldMap(shuttles)
            };

            if (shuttles.Count == 0)
            {
                command.Disable("BSL_NoAvailableShuttles".Translate());
            }
            else if (shuttles.Count == 1 && !PassengerShuttleLaunchBridge.CanStartStateAwareWorldMapLaunch(shuttles[0], out string disabledReason))
            {
                command.Disable(disabledReason);
            }

            return command;
        }

        private static Command_Action CreateReturnHomeCommand(List<Building_PassengerShuttle> shuttles)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "BSL_ReturnHome".Translate(),
                defaultDesc = "BSL_ReturnHomeDesc".Translate(),
                icon = CompLaunchable.LaunchCommandTex,
                action = () => ReturnHomeFromWorldMap(shuttles)
            };

            if (shuttles.Count == 0)
            {
                command.Disable("BSL_NoAvailableShuttles".Translate());
            }
            else if (shuttles.Count == 1 && !ReturnHomeCommandUtility.CanStartReturnHome(shuttles[0], out string disabledReason))
            {
                command.Disable(disabledReason);
            }

            return command;
        }

        private static Command_Action CreateLaunchWhenReadyCommand(List<Building_PassengerShuttle> shuttles)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "BSL_LaunchWhenReady".Translate(),
                defaultDesc = "BSL_LaunchWhenReadyDesc".Translate(),
                icon = CompLaunchable.LaunchCommandTex,
                action = () => QueueLaunchFromWorldMap(shuttles)
            };

            if (shuttles.Count == 0)
            {
                command.Disable("BSL_NoAvailableShuttles".Translate());
            }
            else if (shuttles.Count == 1 && !PassengerShuttleLaunchQueueCommandUtility.CanQueueLaunchWhenReady(shuttles[0], out string disabledReason))
            {
                command.Disable(disabledReason);
            }

            return command;
        }

        private static bool ShouldShowCommand(MapParent mapParent)
        {
            return ModsConfig.OdysseyActive
                   && mapParent != null
                   && mapParent.HasMap
                   && mapParent.Faction == Faction.OfPlayer;
        }

        private static bool ShouldShowReturnHomeCommand(MapParent mapParent)
        {
            return mapParent != null
                   && mapParent.HasMap
                   && !mapParent.Map.IsPlayerHome;
        }

        private static bool ShouldHideSettlementJumpCommand(MapParent mapParent, Gizmo gizmo)
        {
            return BetterShuttleLaunchMod.ActiveSettings.HideSettlementJumpCommandWhenSingleColony
                   && mapParent != null
                   && mapParent.Faction == Faction.OfPlayer
                   && mapParent.HasMap
                   && HomeDestinationFinder.FindPlayerHomeMapParents().Count <= 1
                   && gizmo is Command_Action command
                   && command.defaultLabel == "CommandShowMap".Translate();
        }

        private static void LaunchFromWorldMap(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                PassengerShuttleLaunchBridge.StartStateAwareWorldMapLaunch(shuttles[0]);
                return;
            }

            Find.WindowStack.Add(new Dialog_SelectPassengerShuttle(shuttles, PassengerShuttleLaunchBridge.StartStateAwareWorldMapLaunch));
        }

        private static void QueueLaunchFromWorldMap(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow(shuttles[0]);
                return;
            }

            Find.WindowStack.Add(new Dialog_SelectPassengerShuttle(
                shuttles,
                PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow,
                shuttle => PassengerShuttleLaunchQueueCommandUtility.CanQueueLaunchWhenReady(shuttle, out string disabledReason) ? null : disabledReason));
        }

        private static void ReturnHomeFromWorldMap(IReadOnlyList<Building_PassengerShuttle> shuttles)
        {
            if (shuttles.Count == 0)
            {
                Messages.Message("BSL_NoShuttles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (shuttles.Count == 1)
            {
                ReturnHomeCommandUtility.StartReturnHome(shuttles[0]);
                return;
            }

            Find.WindowStack.Add(new Dialog_SelectPassengerShuttle(
                shuttles,
                ReturnHomeCommandUtility.StartReturnHome,
                shuttle => ReturnHomeCommandUtility.CanStartReturnHome(shuttle, out string disabledReason) ? null : disabledReason));
        }
    }
}
