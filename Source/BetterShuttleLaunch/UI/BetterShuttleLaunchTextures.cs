using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.Settings;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public static class BetterShuttleLaunchTextures
    {
        public static readonly Texture2D CommandLaunchWhenReady = Load(Paths.commandLaunchWhenReady, BetterShuttleLaunchTexturePathConfig.Fallback.commandLaunchWhenReady);
        public static readonly Texture2D CommandLaunchToSettlement = Load(Paths.commandLaunchToSettlement, BetterShuttleLaunchTexturePathConfig.Fallback.commandLaunchToSettlement);
        public static readonly Texture2D CommandReturn = Load(Paths.commandReturn, BetterShuttleLaunchTexturePathConfig.Fallback.commandReturn);
        public static readonly Texture2D CommandCancelLaunch = Load(Paths.commandCancelLaunch, BetterShuttleLaunchTexturePathConfig.Fallback.commandCancelLaunch);
        public static readonly Texture2D CommandOpenTracker = Load(Paths.commandOpenTracker, BetterShuttleLaunchTexturePathConfig.Fallback.commandOpenTracker);

        public static readonly Texture2D TrackerPanelBackground = Load(Paths.trackerPanelBackground, BetterShuttleLaunchTexturePathConfig.Fallback.trackerPanelBackground);
        public static readonly Texture2D TrackerPanelHeader = Load(Paths.trackerPanelHeader, BetterShuttleLaunchTexturePathConfig.Fallback.trackerPanelHeader);
        public static readonly Texture2D TrackerRowBackground = Load(Paths.trackerRowBackground, BetterShuttleLaunchTexturePathConfig.Fallback.trackerRowBackground);
        public static readonly Texture2D TrackerProgressRail = Load(Paths.trackerProgressRail, BetterShuttleLaunchTexturePathConfig.Fallback.trackerProgressRail);
        public static readonly Texture2D TrackerProgressFill = Load(Paths.trackerProgressFill, BetterShuttleLaunchTexturePathConfig.Fallback.trackerProgressFill);
        public static readonly Texture2D TrackerShuttleMarker = Load(Paths.trackerShuttleMarker, BetterShuttleLaunchTexturePathConfig.Fallback.trackerShuttleMarker);
        public static readonly Texture2D TrackerButtonNormal = Load(Paths.trackerButtonNormal, BetterShuttleLaunchTexturePathConfig.Fallback.trackerButtonNormal);
        public static readonly Texture2D TrackerButtonHover = Load(Paths.trackerButtonHover, BetterShuttleLaunchTexturePathConfig.Fallback.trackerButtonHover);
        public static readonly Texture2D TrackerButtonDisabled = Load(Paths.trackerButtonDisabled, BetterShuttleLaunchTexturePathConfig.Fallback.trackerButtonDisabled);
        public static readonly Texture2D TrackerFilterLocal = Load(Paths.trackerFilterLocal, BetterShuttleLaunchTexturePathConfig.Fallback.trackerFilterLocal);
        public static readonly Texture2D TrackerEndpointEmpty = Load(Paths.trackerEndpointEmpty, BetterShuttleLaunchTexturePathConfig.Fallback.trackerEndpointEmpty);
        public static readonly Texture2D TrackerEndpointMap = Load(Paths.trackerEndpointMap, BetterShuttleLaunchTexturePathConfig.Fallback.trackerEndpointMap);
        public static readonly Texture2D TrackerEndpointFaction = Load(Paths.trackerEndpointFaction, BetterShuttleLaunchTexturePathConfig.Fallback.trackerEndpointFaction);

        public static readonly Texture2D StatusIdle = Load(Paths.statusIdle, BetterShuttleLaunchTexturePathConfig.Fallback.statusIdle);
        public static readonly Texture2D StatusLoading = Load(Paths.statusLoading, BetterShuttleLaunchTexturePathConfig.Fallback.statusLoading);
        public static readonly Texture2D StatusWaiting = Load(Paths.statusWaiting, BetterShuttleLaunchTexturePathConfig.Fallback.statusWaiting);
        public static readonly Texture2D StatusReady = Load(Paths.statusReady, BetterShuttleLaunchTexturePathConfig.Fallback.statusReady);
        public static readonly Texture2D StatusInFlight = Load(Paths.statusInFlight, BetterShuttleLaunchTexturePathConfig.Fallback.statusInFlight);
        public static readonly Texture2D StatusArrived = Load(Paths.statusArrived, BetterShuttleLaunchTexturePathConfig.Fallback.statusArrived);
        public static readonly Texture2D StatusFailed = Load(Paths.statusFailed, BetterShuttleLaunchTexturePathConfig.Fallback.statusFailed);

        public static readonly Texture2D BadgeFuel = Load(Paths.badgeFuel, BetterShuttleLaunchTexturePathConfig.Fallback.badgeFuel);
        public static readonly Texture2D BadgeHealth = Load(Paths.badgeHealth, BetterShuttleLaunchTexturePathConfig.Fallback.badgeHealth);
        public static readonly Texture2D BadgeMass = Load(Paths.badgeMass, BetterShuttleLaunchTexturePathConfig.Fallback.badgeMass);

        public static Texture2D GetStatusIcon(PassengerShuttleFlightState state)
        {
            switch (state)
            {
                case PassengerShuttleFlightState.Loading:
                    return StatusLoading;
                case PassengerShuttleFlightState.Waiting:
                    return StatusWaiting;
                case PassengerShuttleFlightState.Ready:
                    return StatusReady;
                case PassengerShuttleFlightState.InFlight:
                    return StatusInFlight;
                case PassengerShuttleFlightState.Arrived:
                    return StatusArrived;
                case PassengerShuttleFlightState.Failed:
                    return StatusFailed;
                default:
                    return StatusIdle;
            }
        }

        public static Texture2D OrFallback(Texture2D customTexture, Texture2D fallbackTexture)
        {
            return customTexture ?? fallbackTexture;
        }

        public static bool DrawIfAvailable(Rect rect, Texture texture, ScaleMode scaleMode = ScaleMode.StretchToFill)
        {
            if (texture == null)
            {
                return false;
            }

            GUI.DrawTexture(rect, texture, scaleMode);
            return true;
        }

        private static BetterShuttleLaunchTexturePathConfig Paths => BetterShuttleLaunchUiConfigDef.ActiveConfig.TexturePaths;

        private static Texture2D Load(string path, string fallbackPath)
        {
            return ContentFinder<Texture2D>.Get(path.NullOrEmpty() ? fallbackPath : path, false);
        }
    }
}
