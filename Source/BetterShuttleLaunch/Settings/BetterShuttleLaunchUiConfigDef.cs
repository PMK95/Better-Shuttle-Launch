using Verse;

namespace BetterShuttleLaunch.Settings
{
    public class BetterShuttleLaunchUiConfigDef : Def
    {
        private const string DefaultConfigDefName = "BSL_DefaultUiConfig";
        private static readonly BetterShuttleLaunchUiConfigDef fallbackConfig = new BetterShuttleLaunchUiConfigDef();

        public float trackerDefaultWidth = 520f;
        public float trackerDefaultHeight = 300f;
        public float trackerMinWidth = 460f;
        public float trackerMinHeight = 180f;
        public float trackerMaxWidth = 720f;
        public float trackerMaxHeight = 600f;
        public float trackerMinimizedHeight = 38f;
        public float trackerRowStrideHeight = 66f;
        public float trackerRowDrawHeight = 60f;
        public int estimatedShuttleTravelTicksPerTile = 120;
        public BetterShuttleLaunchTexturePathConfig texturePaths = new BetterShuttleLaunchTexturePathConfig();

        public static BetterShuttleLaunchUiConfigDef ActiveConfig
        {
            get
            {
                try
                {
                    return DefDatabase<BetterShuttleLaunchUiConfigDef>.GetNamedSilentFail(DefaultConfigDefName) ?? fallbackConfig;
                }
                catch
                {
                    return fallbackConfig;
                }
            }
        }

        public BetterShuttleLaunchTexturePathConfig TexturePaths => texturePaths ?? BetterShuttleLaunchTexturePathConfig.Fallback;
    }

    public class BetterShuttleLaunchTexturePathConfig
    {
        public static readonly BetterShuttleLaunchTexturePathConfig Fallback = new BetterShuttleLaunchTexturePathConfig();

        public string commandLaunchWhenReady = "UI/Commands/BSL_LaunchWhenReady";
        public string commandLaunchToSettlement = "UI/Commands/BSL_LaunchToSettlement";
        public string commandReturn = "UI/Commands/BSL_Return";
        public string commandCancelLaunch = "UI/Commands/BSL_CancelLaunch";
        public string commandOpenTracker = "UI/Commands/BSL_OpenTracker";
        public string trackerPanelBackground = "UI/Tracker/Panel_Background";
        public string trackerPanelHeader = "UI/Tracker/Panel_Header";
        public string trackerRowBackground = "UI/Tracker/Row_Background";
        public string trackerProgressRail = "UI/Tracker/Progress_Rail";
        public string trackerProgressFill = "UI/Tracker/Progress_Fill";
        public string trackerShuttleMarker = "UI/Tracker/Shuttle_Marker";
        public string trackerButtonNormal = "UI/Tracker/Button_Normal";
        public string trackerButtonHover = "UI/Tracker/Button_Hover";
        public string trackerButtonDisabled = "UI/Tracker/Button_Disabled";
        public string trackerFilterLocal = "UI/Tracker/Filter_Local";
        public string trackerEndpointEmpty = "UI/Tracker/Endpoint_Empty";
        public string trackerEndpointMap = "UI/Tracker/Endpoint_Map";
        public string trackerEndpointFaction = "UI/Tracker/Endpoint_Faction";
        public string statusIdle = "UI/Status/Idle";
        public string statusLoading = "UI/Status/Loading";
        public string statusWaiting = "UI/Status/Waiting";
        public string statusReady = "UI/Status/Ready";
        public string statusInFlight = "UI/Status/InFlight";
        public string statusArrived = "UI/Status/Arrived";
        public string statusFailed = "UI/Status/Failed";
        public string badgeFuel = "UI/Badges/Fuel";
        public string badgeHealth = "UI/Badges/Health";
        public string badgeMass = "UI/Badges/Mass";
    }
}
