using Verse;

namespace BetterShuttleLaunch.Settings
{
    public class BetterShuttleLaunchSettings : ModSettings
    {
        public bool HideVanillaLaunchCommand = true;
        public bool ShowLaunchStatusInInspectPane = true;
        public bool ShowTrackerWindow = true;
        public bool ShowTrackerRouteEndpointIcons = true;
        public bool ShowTrackerHoverHelpAndHighlight = true;
        public bool PauseOnShuttleArrival = true;
        public bool FocusOnShuttleArrival = true;
        public float TrackerWindowX = -1f;
        public float TrackerWindowY = 80f;
        public float TrackerWindowWidth = 520f;
        public float TrackerWindowHeight = 300f;
        public bool TrackerWindowMinimized;
        public bool TrackerShowOnlyCurrentMapShuttles;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref HideVanillaLaunchCommand, "hideVanillaLaunchCommand", true);
            Scribe_Values.Look(ref ShowLaunchStatusInInspectPane, "showLaunchStatusInInspectPane", true);
            Scribe_Values.Look(ref ShowTrackerWindow, "showTrackerWindow", true);
            Scribe_Values.Look(ref ShowTrackerRouteEndpointIcons, "showTrackerRouteEndpointIcons", true);
            Scribe_Values.Look(ref ShowTrackerHoverHelpAndHighlight, "showTrackerHoverHelpAndHighlight", true);
            Scribe_Values.Look(ref PauseOnShuttleArrival, "pauseOnShuttleArrival", true);
            Scribe_Values.Look(ref FocusOnShuttleArrival, "focusOnShuttleArrival", true);
            Scribe_Values.Look(ref TrackerWindowX, "trackerWindowX", -1f);
            Scribe_Values.Look(ref TrackerWindowY, "trackerWindowY", 80f);
            Scribe_Values.Look(ref TrackerWindowWidth, "trackerWindowWidth", BetterShuttleLaunchUiConfigDef.ActiveConfig.trackerDefaultWidth);
            Scribe_Values.Look(ref TrackerWindowHeight, "trackerWindowHeight", BetterShuttleLaunchUiConfigDef.ActiveConfig.trackerDefaultHeight);
            Scribe_Values.Look(ref TrackerWindowMinimized, "trackerWindowMinimized", false);
            Scribe_Values.Look(ref TrackerShowOnlyCurrentMapShuttles, "trackerShowOnlyCurrentMapShuttles", false);
        }

        public void ResetToDefaults()
        {
            HideVanillaLaunchCommand = true;
            ShowLaunchStatusInInspectPane = true;
            ShowTrackerWindow = true;
            ShowTrackerRouteEndpointIcons = true;
            ShowTrackerHoverHelpAndHighlight = true;
            PauseOnShuttleArrival = true;
            FocusOnShuttleArrival = true;
            TrackerWindowX = -1f;
            TrackerWindowY = 80f;
            TrackerWindowWidth = BetterShuttleLaunchUiConfigDef.ActiveConfig.trackerDefaultWidth;
            TrackerWindowHeight = BetterShuttleLaunchUiConfigDef.ActiveConfig.trackerDefaultHeight;
            TrackerWindowMinimized = false;
            TrackerShowOnlyCurrentMapShuttles = false;
        }
    }
}
