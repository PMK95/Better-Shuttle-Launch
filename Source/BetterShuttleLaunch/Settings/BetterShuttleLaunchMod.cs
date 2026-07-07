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
                "BSL_SettingHideSettlementJumpWhenSingleColony".Translate(),
                ref Settings.HideSettlementJumpCommandWhenSingleColony,
                "BSL_SettingHideSettlementJumpWhenSingleColonyDesc".Translate());
            listing.CheckboxLabeled(
                "BSL_SettingAutoLandReturnHomeAtLastDepartureCell".Translate(),
                ref Settings.AutoLandReturnHomeAtLastDepartureCell,
                "BSL_SettingAutoLandReturnHomeAtLastDepartureCellDesc".Translate());
            listing.End();
        }
    }
}
