using BetterShuttleLaunch.LaunchQueue;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public static class BetterShuttleLaunchTextures
    {
        public static readonly Texture2D CommandLaunchWhenReady = Load("UI/Commands/BSL_LaunchWhenReady");
        public static readonly Texture2D CommandLaunchToSettlement = Load("UI/Commands/BSL_LaunchToSettlement");
        public static readonly Texture2D CommandReturn = Load("UI/Commands/BSL_Return");
        public static readonly Texture2D CommandCancelLaunch = Load("UI/Commands/BSL_CancelLaunch");
        public static readonly Texture2D CommandOpenTracker = Load("UI/Commands/BSL_OpenTracker");

        public static readonly Texture2D TrackerPanelBackground = Load("UI/Tracker/Panel_Background");
        public static readonly Texture2D TrackerPanelHeader = Load("UI/Tracker/Panel_Header");
        public static readonly Texture2D TrackerRowBackground = Load("UI/Tracker/Row_Background");
        public static readonly Texture2D TrackerProgressRail = Load("UI/Tracker/Progress_Rail");
        public static readonly Texture2D TrackerProgressFill = Load("UI/Tracker/Progress_Fill");
        public static readonly Texture2D TrackerShuttleMarker = Load("UI/Tracker/Shuttle_Marker");
        public static readonly Texture2D TrackerButtonNormal = Load("UI/Tracker/Button_Normal");
        public static readonly Texture2D TrackerButtonHover = Load("UI/Tracker/Button_Hover");
        public static readonly Texture2D TrackerButtonDisabled = Load("UI/Tracker/Button_Disabled");
        public static readonly Texture2D TrackerFilterLocal = Load("UI/Tracker/Filter_Local");

        public static readonly Texture2D StatusIdle = Load("UI/Status/Idle");
        public static readonly Texture2D StatusLoading = Load("UI/Status/Loading");
        public static readonly Texture2D StatusWaiting = Load("UI/Status/Waiting");
        public static readonly Texture2D StatusReady = Load("UI/Status/Ready");
        public static readonly Texture2D StatusInFlight = Load("UI/Status/InFlight");
        public static readonly Texture2D StatusArrived = Load("UI/Status/Arrived");
        public static readonly Texture2D StatusFailed = Load("UI/Status/Failed");

        public static readonly Texture2D BadgeFuel = Load("UI/Badges/Fuel");
        public static readonly Texture2D BadgeHealth = Load("UI/Badges/Health");
        public static readonly Texture2D BadgeMass = Load("UI/Badges/Mass");
        public static readonly Texture2D BadgePassengers = Load("UI/Badges/Passengers");

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

        private static Texture2D Load(string path)
        {
            return ContentFinder<Texture2D>.Get(path, false);
        }
    }
}
