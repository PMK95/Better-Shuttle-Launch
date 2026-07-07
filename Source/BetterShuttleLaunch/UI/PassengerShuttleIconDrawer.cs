using RimWorld;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public static class PassengerShuttleIconDrawer
    {
        public static void Draw(Rect rect, Building_PassengerShuttle shuttle)
        {
            Texture icon = shuttle?.def?.uiIcon ?? CompLaunchable.LaunchCommandTex;
            if (icon == null)
            {
                return;
            }

            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }
    }
}
