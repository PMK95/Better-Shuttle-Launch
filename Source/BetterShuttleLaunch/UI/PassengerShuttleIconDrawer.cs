using RimWorld;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public static class PassengerShuttleIconDrawer
    {
        public static void Draw(Rect rect, Building_PassengerShuttle shuttle)
        {
            Texture icon = TryGetThingTexture(shuttle) ?? shuttle?.def?.uiIcon ?? CompLaunchable.LaunchCommandTex;
            if (icon == null)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = shuttle?.DrawColor ?? Color.white;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            GUI.color = oldColor;
        }

        private static Texture TryGetThingTexture(Building_PassengerShuttle shuttle)
        {
            if (shuttle?.Graphic == null)
            {
                return null;
            }

            Material material = shuttle.Graphic.MatAt(shuttle.Rotation, shuttle);
            return material?.mainTexture;
        }
    }
}
