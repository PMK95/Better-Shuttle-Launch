using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Dialog_SelectHomeDestination : Window
    {
        private readonly List<MapParent> homes;
        private readonly Action<MapParent> selectAction;
        private Vector2 scrollPosition;

        public Dialog_SelectHomeDestination(IReadOnlyList<MapParent> homes, Action<MapParent> selectAction)
        {
            this.homes = new List<MapParent>(homes);
            this.selectAction = selectAction;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(520f, 380f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "BSL_SelectSettlement".Translate());
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(0f, 45f, inRect.width, inRect.height - 45f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, homes.Count * 38f);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            for (int i = 0; i < homes.Count; i++)
            {
                DrawHomeRow(new Rect(0f, i * 38f, viewRect.width, 32f), homes[i]);
            }

            Widgets.EndScrollView();
        }

        private void DrawHomeRow(Rect rect, MapParent home)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.Label(new Rect(rect.x, rect.y + 5f, rect.width - 120f, 24f), home.LabelCap);

            Rect buttonRect = new Rect(rect.x + rect.width - 110f, rect.y + 2f, 100f, 28f);
            if (Widgets.ButtonText(buttonRect, "BSL_Select".Translate()))
            {
                Close();
                selectAction(home);
            }
        }
    }
}
