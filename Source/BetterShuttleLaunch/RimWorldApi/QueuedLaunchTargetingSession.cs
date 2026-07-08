using System;
using BetterShuttleLaunch.Domain;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.RimWorldApi
{
    public static class QueuedLaunchTargetingSession
    {
        private static ShuttleContext context;
        private static Func<PlanetTile, string> targetLabelGetter;

        public static bool Active => context != null && context.OriginTile.Valid && context.Launchable != null;

        public static void Begin(ShuttleContext shuttleContext, Func<PlanetTile, string> labelGetter)
        {
            context = shuttleContext;
            targetLabelGetter = labelGetter;
        }

        public static void Clear()
        {
            context = null;
            targetLabelGetter = null;
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

            LaunchRangeInfo rangeInfo = LaunchRangeInfo.ForTile(context, context.OriginTile);
            if (rangeInfo.MaximumRange >= 0)
            {
                GenDraw.DrawWorldRadiusRing(context.OriginTile, rangeInfo.MaximumRange, GenDraw.CurTargetingMat);
            }

            if (rangeInfo.CurrentFuelRange >= 0 && rangeInfo.CurrentFuelRange != rangeInfo.MaximumRange)
            {
                GenDraw.DrawWorldRadiusRing(context.OriginTile, rangeInfo.CurrentFuelRange, GenDraw.CurTargetingMat);
            }
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

            LaunchRangeInfo rangeInfo = LaunchRangeInfo.ForTile(context, targetTile);
            if (rangeInfo.State != LaunchRangeState.NeedsFuel)
            {
                return;
            }

            string targetLabel = targetLabelGetter != null ? targetLabelGetter(targetTile) : targetTile.ToString();
            string text = "BSL_QueuedDestinationFuelWaiting".Translate(targetLabel, rangeInfo.Distance.ToString(), rangeInfo.CurrentFuelRangeText);
            Widgets.MouseAttachedLabel(text, 0f, 46f, new Color(1f, 0.82f, 0.28f));
        }
    }

    [HarmonyPatch(typeof(WorldTargeter), nameof(WorldTargeter.TargeterOnGUI))]
    public static class QueuedLaunchTargeterGuiPatch
    {
        public static void Postfix(WorldTargeter __instance)
        {
            QueuedLaunchTargetingSession.DrawFuelWarningIfNeeded(__instance);
        }
    }

    [HarmonyPatch(typeof(WorldTargeter), nameof(WorldTargeter.TargeterUpdate))]
    public static class QueuedLaunchTargeterUpdatePatch
    {
        public static void Postfix(WorldTargeter __instance)
        {
            QueuedLaunchTargetingSession.DrawRangeRingsIfNeeded(__instance);
        }
    }
}
