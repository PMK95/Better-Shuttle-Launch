using Verse;

namespace BetterShuttleLaunch.Settings
{
    public class BetterShuttleLaunchSettings : ModSettings
    {
        public bool ShowLaunchStatusInInspectPane = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ShowLaunchStatusInInspectPane, "showLaunchStatusInInspectPane", true);
        }
    }
}
