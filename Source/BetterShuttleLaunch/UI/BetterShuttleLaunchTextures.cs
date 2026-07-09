using RimWorld;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public static class BetterShuttleLaunchTextures
    {
        public static readonly Texture2D CommandLaunchWhenReady = Load("UI/Commands/BSL_LaunchWhenReady");
        public static readonly Texture2D CommandCancelLaunch = Load("UI/Commands/BSL_CancelLaunch");

        public static Texture2D GetLaunchWhenReadyIcon()
        {
            return CommandLaunchWhenReady ?? CompLaunchable.LaunchCommandTex;
        }

        public static Texture2D GetCancelQueuedLaunchIcon()
        {
            return CommandCancelLaunch ?? TexCommand.ClearPrioritizedWork ?? TexCommand.RemoveRoutePlannerWaypoint ?? CompLaunchable.LaunchCommandTex;
        }

        private static Texture2D Load(string path)
        {
            return ContentFinder<Texture2D>.Get(path, false);
        }
    }
}
