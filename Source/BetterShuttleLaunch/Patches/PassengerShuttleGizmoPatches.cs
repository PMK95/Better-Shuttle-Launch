using System.Collections.Generic;
using System.Text;
using BetterShuttleLaunch.Domain;
using BetterShuttleLaunch.Services;
using BetterShuttleLaunch.Settings;
using BetterShuttleLaunch.UI;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.Patches
{
    [HarmonyPatch(typeof(CompLaunchable), nameof(CompLaunchable.CompGetGizmosExtra))]
    public static class PassengerShuttleCompGizmoPatch
    {
        public static void Postfix(CompLaunchable __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendPassengerShuttleGizmos(__instance, __result);
        }

        private static IEnumerable<Gizmo> AppendPassengerShuttleGizmos(CompLaunchable launchable, IEnumerable<Gizmo> originalGizmos)
        {
            Building_PassengerShuttle shuttle = launchable.parent as Building_PassengerShuttle;
            bool isPassengerShuttle = ModsConfig.OdysseyActive && ShuttleContext.IsSupportedMapPassengerShuttle(shuttle);

            foreach (Gizmo gizmo in originalGizmos)
            {
                if (isPassengerShuttle && BetterShuttleLaunchMod.ActiveSettings.HideVanillaLaunchCommand && IsVanillaLaunchCommand(gizmo))
                {
                    continue;
                }

                yield return gizmo;
            }

            if (isPassengerShuttle)
            {
                yield return ShuttleLaunchCommandFactory.CreateForMapShuttle(shuttle);
            }
        }

        private static bool IsVanillaLaunchCommand(Gizmo gizmo)
        {
            return gizmo is Command command && command.defaultLabel == "CommandLaunchGroup".Translate();
        }
    }

    [HarmonyPatch(typeof(MapParent), nameof(MapParent.GetGizmos))]
    public static class PassengerShuttleMapParentGizmoPatch
    {
        public static void Postfix(MapParent __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendMapParentCommand(__instance, __result);
        }

        private static IEnumerable<Gizmo> AppendMapParentCommand(MapParent mapParent, IEnumerable<Gizmo> originalGizmos)
        {
            foreach (Gizmo gizmo in originalGizmos)
            {
                yield return gizmo;
            }

            if (!ModsConfig.OdysseyActive || mapParent == null || !mapParent.HasMap)
            {
                yield break;
            }

            List<Building_PassengerShuttle> shuttles = ShuttleDiscoveryService.FindPassengerShuttles(mapParent.Map);
            if (shuttles.Count > 0)
            {
                yield return ShuttleLaunchCommandFactory.CreateForMapParent(shuttles);
            }
        }
    }

    [HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
    public static class PassengerShuttleCaravanGizmoPatch
    {
        public static void Postfix(Caravan __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendCaravanCommand(__instance, __result);
        }

        private static IEnumerable<Gizmo> AppendCaravanCommand(Caravan caravan, IEnumerable<Gizmo> originalGizmos)
        {
            bool isPassengerShuttleCaravan = ModsConfig.OdysseyActive && ShuttleContext.TryForCaravan(caravan, out _, out _);
            foreach (Gizmo gizmo in originalGizmos)
            {
                if (isPassengerShuttleCaravan && BetterShuttleLaunchMod.ActiveSettings.HideVanillaLaunchCommand && IsVanillaLaunchCommand(gizmo))
                {
                    continue;
                }

                yield return gizmo;
            }

            if (isPassengerShuttleCaravan)
            {
                yield return ShuttleLaunchCommandFactory.CreateForCaravan(caravan);
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
            if (!ModsConfig.OdysseyActive || !ShuttleContext.IsSupportedMapPassengerShuttle(shuttle))
            {
                return;
            }

            QueuedShuttleLaunch queuedLaunch = ShuttleLaunchQueueGameComponent.Current?.FindQueuedLaunch(shuttle);
            bool hasQueuedLaunch = queuedLaunch != null;
            LaunchReadiness readiness = hasQueuedLaunch ? LaunchReadinessService.Evaluate(queuedLaunch) : default;
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

    [HarmonyPatch(typeof(CompLaunchable), nameof(CompLaunchable.TryLaunch))]
    public static class PassengerShuttleDepartureMemoryPatch
    {
        public static void Prefix(CompLaunchable __instance)
        {
            if (__instance?.parent is Building_PassengerShuttle shuttle && ShuttleContext.IsSupportedMapPassengerShuttle(shuttle))
            {
                ShuttleLaunchQueueGameComponent.Current?.RememberDeparture(shuttle);
            }
        }
    }
}
