using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.LaunchQueue
{
    public static class QueuedLaunchRangeTargetingSession
    {
        private static PlanetTile originTile;
        private static CompLaunchable launchable;
        private static Building_PassengerShuttle shuttle;
        private static Func<PlanetTile, string> targetLabelGetter;

        public static bool Active => launchable != null && originTile.Valid;

        public static void Begin(
            PlanetTile origin,
            CompLaunchable launchableComp,
            Building_PassengerShuttle passengerShuttle,
            Func<PlanetTile, string> labelGetter)
        {
            originTile = origin;
            launchable = launchableComp;
            shuttle = passengerShuttle;
            targetLabelGetter = labelGetter;
        }

        public static void Clear()
        {
            originTile = default;
            launchable = null;
            shuttle = null;
            targetLabelGetter = null;
        }

        public static QueuedLaunchRangeInfo GetRangeInfo(PlanetTile targetTile)
        {
            return QueuedLaunchRangeInfo.ForTile(originTile, launchable, targetTile, GetCurrentFuelLevel());
        }

        public static void DrawRangeRingsIfNeeded(WorldTargeter worldTargeter)
        {
            if (!Active)
            {
                return;
            }

            if (worldTargeter == null || !worldTargeter.IsTargeting)
            {
                Clear();
                return;
            }

            DrawRangeRings();
        }

        public static void DrawFuelWarningIfNeeded(WorldTargeter worldTargeter)
        {
            if (!Active)
            {
                return;
            }

            if (worldTargeter == null || !worldTargeter.IsTargeting)
            {
                Clear();
                return;
            }

            PlanetTile targetTile = worldTargeter.ClosestLayerTile;
            if (!targetTile.Valid)
            {
                return;
            }

            QueuedLaunchRangeInfo rangeInfo = GetRangeInfo(targetTile);
            if (rangeInfo.State != QueuedLaunchRangeState.NeedsFuel)
            {
                return;
            }

            string targetLabel = targetLabelGetter != null ? targetLabelGetter(targetTile) : targetTile.ToString();
            string text = "BSL_QueuedDestinationFuelWaiting".Translate(targetLabel, rangeInfo.Distance.ToString(), rangeInfo.CurrentFuelRangeText);
            Widgets.MouseAttachedLabel(text, 0f, 46f, new Color(1f, 0.82f, 0.28f));
        }

        private static void DrawRangeRings()
        {
            QueuedLaunchRangeInfo rangeInfo = QueuedLaunchRangeInfo.ForTile(originTile, launchable, originTile, GetCurrentFuelLevel());
            if (rangeInfo.MaximumRange >= 0)
            {
                GenDraw.DrawWorldRadiusRing(originTile, rangeInfo.MaximumRange, GenDraw.CurTargetingMat);
            }

            if (rangeInfo.CurrentFuelRange < 0 || rangeInfo.CurrentFuelRange == rangeInfo.MaximumRange)
            {
                return;
            }

            GenDraw.DrawWorldRadiusRing(originTile, rangeInfo.CurrentFuelRange, GenDraw.CurTargetingMat);
        }

        private static float? GetCurrentFuelLevel()
        {
            if (shuttle != null && !shuttle.Destroyed)
            {
                return shuttle.FuelLevel;
            }

            return launchable?.FuelLevel;
        }
    }

    [HarmonyPatch(typeof(WorldTargeter), nameof(WorldTargeter.TargeterOnGUI))]
    public static class QueuedLaunchRangeTargeterGuiPatch
    {
        public static void Postfix(WorldTargeter __instance)
        {
            QueuedLaunchRangeTargetingSession.DrawFuelWarningIfNeeded(__instance);
        }
    }

    [HarmonyPatch(typeof(WorldTargeter), nameof(WorldTargeter.TargeterUpdate))]
    public static class QueuedLaunchRangeTargeterUpdatePatch
    {
        public static void Postfix(WorldTargeter __instance)
        {
            QueuedLaunchRangeTargetingSession.DrawRangeRingsIfNeeded(__instance);
        }
    }
}
