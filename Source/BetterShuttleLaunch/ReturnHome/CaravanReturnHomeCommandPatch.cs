using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.ReturnHome;
using BetterShuttleLaunch.Settings;
using BetterShuttleLaunch.Shuttles;
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
                yield return gizmo;
            }

            if (!ModsConfig.OdysseyActive || caravan == null || caravan.Faction != Faction.OfPlayer || caravan.Shuttle == null)
            {
                yield break;
            }

            yield return CreateCaravanReturnHomeCommand(caravan);

            LaunchQueueGameComponent queue = LaunchQueueGameComponent.Current;
            if (queue != null && queue.IsQueued(caravan))
            {
                yield return PassengerShuttleLaunchQueueCommandUtility.CreateCancelCaravanQueuedLaunchCommand(caravan);
            }
            else
            {
                yield return PassengerShuttleLaunchQueueCommandUtility.CreateCaravanLaunchWhenReadyCommand(caravan);
            }
        }

        private static Gizmo CreateCaravanReturnHomeCommand(Caravan caravan)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "BSL_ReturnHome".Translate(),
                defaultDesc = "BSL_ReturnHomeDesc".Translate(),
                icon = CompLaunchable.LaunchCommandTex,
                action = () => StartCaravanReturnHome(caravan)
            };

            AcceptanceReport canLaunch = CaravanShuttleUtility.CanLaunchCaravanShuttle(caravan);
            if (!canLaunch.Accepted)
            {
                command.Disable(canLaunch.Reason);
            }

            return command;
        }

        private static void StartCaravanReturnHome(Caravan caravan)
        {
            IReadOnlyList<MapParent> homes = HomeDestinationFinder.FindPlayerHomeMapParents();
            if (homes.Count == 0)
            {
                Messages.Message("BSL_NoValidHome".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (homes.Count == 1)
            {
                LaunchCaravanTowardHome(caravan, homes[0]);
                return;
            }

            Find.WindowStack.Add(new UI.Dialog_SelectHomeDestination(homes, home => LaunchCaravanTowardHome(caravan, home)));
        }

        private static void LaunchCaravanTowardHome(Caravan caravan, MapParent home)
        {
            Building_PassengerShuttle shuttle = caravan.Shuttle;
            CompLaunchable launchable = shuttle?.LaunchableComp;
            CompTransporter transporter = shuttle?.TransporterComp;
            if (launchable == null || transporter == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            string failReason = null;
            if (BetterShuttleLaunchMod.ActiveSettings.AutoLandReturnHomeAtLastDepartureCell
                && LaunchQueueGameComponent.Current != null
                && LaunchQueueGameComponent.Current.TryGetLastDepartureLocation(shuttle, out LastDepartureLocation location)
                && location.IsUsableFor(home)
                && PassengerShuttleLaunchBridge.TryChooseSpecificLandingCellFromCaravan(caravan, home, location.Cell, location.Rotation, out failReason))
            {
                return;
            }

            if (!failReason.NullOrEmpty())
            {
                Messages.Message("BSL_AutoReturnLandingFallback".Translate(failReason), MessageTypeDefOf.RejectInput, false);
            }

            List<CompTransporter> transporters = new List<CompTransporter> { transporter };
            GlobalTargetInfo target = new GlobalTargetInfo(home);
            CompLaunchable.ChoseWorldTarget(
                target,
                caravan.Tile,
                transporters,
                launchable.MaxLaunchDistanceEver(target.Tile.Layer),
                (tile, action) => CaravanShuttleUtility.LaunchShuttle(caravan, tile, action),
                launchable,
                shuttle.FuelLevel);
        }
    }
}
