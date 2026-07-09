using BetterShuttleLaunch.Domain;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.RimWorldApi
{
    public static class QueuedLaunchTargetingSession
    {
        private static ShuttleContext context;
        private static PlanetLayer cachedLayer;
        private static PlanetTile cachedOrigin = PlanetTile.Invalid;
        private static PlanetTile cachedClosest = PlanetTile.Invalid;

        public static bool Active => context != null && context.OriginTile.Valid && context.Launchable != null;

        public static void Begin(ShuttleContext shuttleContext)
        {
            context = shuttleContext;
            ClearLayerRangeCache();
        }

        public static void Clear()
        {
            context = null;
            ClearLayerRangeCache();
        }

        public static void DrawVanillaStyleRangeRingsForActiveTargeting()
        {
            if (!Active)
            {
                return;
            }

            PlanetTile rangeCenter = GetRangeRingCenterTileForSelectedLayer(context.OriginTile);
            if (!rangeCenter.Valid)
            {
                return;
            }

            int maximumRange = context.Launchable.MaxLaunchDistanceEver(rangeCenter.Layer);
            GenDraw.DrawWorldRadiusRing(rangeCenter, maximumRange, CompPilotConsole.GetThrusterRadiusMat(rangeCenter));

            if (context.Launchable.Refuelable != null)
            {
                int currentFuelRange = context.Launchable.MaxLaunchDistanceAtFuelLevel(context.FuelLevel, rangeCenter.Layer);
                if (currentFuelRange >= 0)
                {
                    GenDraw.DrawWorldRadiusRing(rangeCenter, currentFuelRange, CompPilotConsole.GetFuelRadiusMat(rangeCenter));
                }
            }
        }

        private static PlanetTile GetRangeRingCenterTileForSelectedLayer(PlanetTile originTile)
        {
            if (!originTile.Valid)
            {
                return PlanetTile.Invalid;
            }

            PlanetLayer selectedLayer = PlanetLayer.Selected;
            if (selectedLayer == null || originTile.Layer == selectedLayer)
            {
                return originTile;
            }

            if (cachedLayer != selectedLayer || cachedOrigin != originTile || !cachedClosest.Valid)
            {
                cachedLayer = selectedLayer;
                cachedOrigin = originTile;
                cachedClosest = selectedLayer.GetClosestTile_NewTemp(originTile);
            }

            return cachedClosest;
        }

        private static void ClearLayerRangeCache()
        {
            cachedLayer = null;
            cachedOrigin = PlanetTile.Invalid;
            cachedClosest = PlanetTile.Invalid;
        }
    }
}
