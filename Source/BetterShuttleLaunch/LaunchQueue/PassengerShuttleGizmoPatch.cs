using System.Collections.Generic;
using System.Text;
using BetterShuttleLaunch.Commands;
using BetterShuttleLaunch.Settings;
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
            bool isPassengerShuttle = ModsConfig.OdysseyActive && PassengerShuttleFinder.IsSupportedPassengerShuttle(shuttle);

            foreach (Gizmo gizmo in originalGizmos)
            {
                if (isPassengerShuttle && BetterShuttleLaunchMod.ActiveSettings.HideVanillaLaunchCommand && IsVanillaLaunchCommand(gizmo))
                {
                    continue;
                }

                yield return gizmo;
            }

            if (!isPassengerShuttle)
            {
                yield break;
            }

            yield return PassengerShuttleLaunchCommandFactory.CreateForMapShuttle(shuttle);
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
            if (!ModsConfig.OdysseyActive || !PassengerShuttleFinder.IsSupportedPassengerShuttle(shuttle))
            {
                return;
            }

            QueuedPassengerShuttleLaunch queuedLaunch = LaunchQueueGameComponent.Current?.FindQueuedLaunch(shuttle);
            bool hasQueuedLaunch = queuedLaunch != null;
            LaunchReadinessResult readiness = hasQueuedLaunch ? LaunchReadinessEvaluator.EvaluateQueuedPassengerShuttleLaunch(queuedLaunch) : default;
            StringBuilder builder = new StringBuilder();
            if (!__result.NullOrEmpty())
            {
                builder.Append(__result);
            }

            if (BetterShuttleLaunchMod.ActiveSettings.ShowLaunchStatusInInspectPane
                && __instance.CanLaunch() is AcceptanceReport canLaunch
                && !canLaunch.Accepted
                && !canLaunch.Reason.NullOrEmpty())
            {
                AppendUniqueLine(builder, "BSL_LaunchStatusUnavailable".Translate("DisabledCommand".Translate(), canLaunch.Reason), canLaunch.Reason);
            }

            if (hasQueuedLaunch)
            {
                string queuedText = "BSL_LaunchQueued".Translate();
                if (!readiness.StatusText.NullOrEmpty())
                {
                    queuedText += ": " + readiness.StatusText;
                }

                AppendUniqueLine(builder, queuedText, queuedText);
            }

            __result = builder.ToString();
        }

        private static void AppendUniqueLine(StringBuilder builder, string line, string duplicateCheckText)
        {
            if (line.NullOrEmpty() || (!duplicateCheckText.NullOrEmpty() && builder.ToString().Contains(duplicateCheckText)))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(line);
        }
    }
}
