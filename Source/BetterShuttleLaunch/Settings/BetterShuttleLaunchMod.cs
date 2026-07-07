using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.Settings
{
    public class BetterShuttleLaunchMod : Mod
    {
        public static BetterShuttleLaunchSettings Settings;
        public static BetterShuttleLaunchSettings ActiveSettings => Settings ?? new BetterShuttleLaunchSettings();

        public BetterShuttleLaunchMod(ModContentPack content)
            : base(content)
        {
            Settings = GetSettings<BetterShuttleLaunchSettings>();
        }

        public override string SettingsCategory()
        {
            return "Better Shuttle Launch";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "BSL_SettingHideVanillaLaunchCommand".Translate(),
                ref Settings.HideVanillaLaunchCommand,
                "BSL_SettingHideVanillaLaunchCommandDesc".Translate());
            listing.CheckboxLabeled(
                "BSL_SettingShowLaunchStatusInInspectPane".Translate(),
                ref Settings.ShowLaunchStatusInInspectPane,
                "BSL_SettingShowLaunchStatusInInspectPaneDesc".Translate());
            listing.CheckboxLabeled(
                "BSL_SettingShowTrackerWindow".Translate(),
                ref Settings.ShowTrackerWindow,
                "BSL_SettingShowTrackerWindowDesc".Translate());
            listing.CheckboxLabeled(
                "BSL_SettingShowTrackerRouteEndpointIcons".Translate(),
                ref Settings.ShowTrackerRouteEndpointIcons,
                "BSL_SettingShowTrackerRouteEndpointIconsDesc".Translate());
            listing.CheckboxLabeled(
                "BSL_SettingShowMapPassengerOverlay".Translate(),
                ref Settings.ShowMapPassengerOverlay,
                "BSL_SettingShowMapPassengerOverlayDesc".Translate());
            listing.CheckboxLabeled(
                "BSL_SettingShowTrackerHoverHelpAndHighlight".Translate(),
                ref Settings.ShowTrackerHoverHelpAndHighlight,
                "BSL_SettingShowTrackerHoverHelpAndHighlightDesc".Translate());
            listing.CheckboxLabeled(
                "BSL_SettingPauseOnShuttleArrival".Translate(),
                ref Settings.PauseOnShuttleArrival,
                "BSL_SettingPauseOnShuttleArrivalDesc".Translate());
            listing.CheckboxLabeled(
                "BSL_SettingFocusOnShuttleArrival".Translate(),
                ref Settings.FocusOnShuttleArrival,
                "BSL_SettingFocusOnShuttleArrivalDesc".Translate());
            listing.End();
        }
    }
}
