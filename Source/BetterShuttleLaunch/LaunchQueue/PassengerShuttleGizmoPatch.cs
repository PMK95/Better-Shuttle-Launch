using System.Collections.Generic;
using System.Text;
using BetterShuttleLaunch.ReturnHome;
using BetterShuttleLaunch.Shuttles;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    [HarmonyPatch(typeof(CompLaunchable), nameof(CompLaunchable.CompGetGizmosExtra))]
    public static class PassengerShuttleGizmoPatch
    {
        public static void Postfix(CompLaunchable __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendPassengerShuttleGizmos(__instance, __result);
        }

        private static IEnumerable<Gizmo> AppendPassengerShuttleGizmos(CompLaunchable launchable, IEnumerable<Gizmo> originalGizmos)
        {
            Building_PassengerShuttle shuttle = launchable.parent as Building_PassengerShuttle;
            bool replaceVanillaLaunchWithQueuedLaunch = ModsConfig.OdysseyActive
                                                         && PassengerShuttleFinder.IsSupportedPassengerShuttle(shuttle)
                                                         && shuttle.TransporterComp.LoadingInProgressOrReadyToLaunch
                                                         && !launchable.CanLaunch().Accepted
                                                         && PassengerShuttleLaunchQueueCommandUtility.CanQueueLaunchWhenReady(shuttle, out _);

            foreach (Gizmo gizmo in originalGizmos)
            {
                if (replaceVanillaLaunchWithQueuedLaunch && IsVanillaLaunchCommand(gizmo))
                {
                    continue;
                }

                yield return gizmo;
            }

            if (!ModsConfig.OdysseyActive || !PassengerShuttleFinder.IsSupportedPassengerShuttle(shuttle))
            {
                yield break;
            }

            yield return ReturnHomeCommandUtility.CreateReturnHomeCommand(shuttle);

            LaunchQueueGameComponent queue = LaunchQueueGameComponent.Current;
            if (queue != null && queue.IsQueued(shuttle))
            {
                yield return PassengerShuttleLaunchQueueCommandUtility.CreateCancelQueuedLaunchCommand(shuttle);
            }
            else
            {
                yield return PassengerShuttleLaunchQueueCommandUtility.CreateLaunchWhenReadyCommand(shuttle);
            }
        }

        private static bool IsVanillaLaunchCommand(Gizmo gizmo)
        {
            return gizmo is Command command && command.defaultLabel == "CommandLaunchGroup".Translate();
        }
    }

    [HarmonyPatch(typeof(CompLaunchable), nameof(CompLaunchable.CompInspectStringExtra))]
    public static class PassengerShuttleInspectStringPatch
    {
        public static void Postfix(CompLaunchable __instance, ref string __result)
        {
            Building_PassengerShuttle shuttle = __instance.parent as Building_PassengerShuttle;
            QueuedPassengerShuttleLaunch queuedLaunch = LaunchQueueGameComponent.Current?.FindQueuedLaunch(shuttle);
            if (queuedLaunch == null)
            {
                return;
            }

            LaunchReadinessResult readiness = LaunchReadinessEvaluator.EvaluateQueuedPassengerShuttleLaunch(queuedLaunch);
            StringBuilder builder = new StringBuilder();
            if (!__result.NullOrEmpty())
            {
                builder.AppendLine(__result);
            }

            builder.Append("BSL_LaunchQueued".Translate());
            if (!readiness.StatusText.NullOrEmpty())
            {
                builder.Append(": ");
                builder.Append(readiness.StatusText);
            }

            __result = builder.ToString();
        }
    }
}
