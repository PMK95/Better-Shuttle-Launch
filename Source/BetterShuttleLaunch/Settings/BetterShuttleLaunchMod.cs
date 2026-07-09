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
                "BSL_SettingShowLaunchStatusInInspectPane".Translate(),
                ref Settings.ShowLaunchStatusInInspectPane,
                "BSL_SettingShowLaunchStatusInInspectPaneDesc".Translate());
            listing.End();
        }
    }
}
