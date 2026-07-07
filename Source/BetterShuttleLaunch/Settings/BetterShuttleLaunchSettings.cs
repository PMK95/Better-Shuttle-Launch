using Verse;

namespace BetterShuttleLaunch.Settings
{
    public class BetterShuttleLaunchSettings : ModSettings
    {
        public bool HideSettlementJumpCommandWhenSingleColony;
        public bool AutoLandReturnHomeAtLastDepartureCell = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref HideSettlementJumpCommandWhenSingleColony, "hideSettlementJumpCommandWhenSingleColony", false);
            Scribe_Values.Look(ref AutoLandReturnHomeAtLastDepartureCell, "autoLandReturnHomeAtLastDepartureCell", true);
        }
    }
}
