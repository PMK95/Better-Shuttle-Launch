using System;
using System.Collections.Generic;
using BetterShuttleLaunch.Shuttles;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Dialog_SelectPassengerShuttle : Window
    {
        private readonly List<Building_PassengerShuttle> shuttles;
        private readonly Action<Building_PassengerShuttle> selectAction;
        private readonly Func<Building_PassengerShuttle, string> getDisabledReason;
        private Vector2 scrollPosition;

        public Dialog_SelectPassengerShuttle(IReadOnlyList<Building_PassengerShuttle> shuttles, Action<Building_PassengerShuttle> selectAction)
            : this(shuttles, selectAction, GetStateAwareLaunchDisabledReason)
        {
        }

        public Dialog_SelectPassengerShuttle(
            IReadOnlyList<Building_PassengerShuttle> shuttles,
            Action<Building_PassengerShuttle> selectAction,
            Func<Building_PassengerShuttle, string> getDisabledReason)
        {
            this.shuttles = new List<Building_PassengerShuttle>(shuttles);
            this.selectAction = selectAction;
            this.getDisabledReason = getDisabledReason;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(560f, 420f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "BSL_SelectShuttle".Translate());
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(0f, 45f, inRect.width, inRect.height - 45f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, shuttles.Count * 42f);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            for (int i = 0; i < shuttles.Count; i++)
            {
                DrawShuttleRow(new Rect(0f, i * 42f, viewRect.width, 36f), shuttles[i]);
            }

            Widgets.EndScrollView();
        }

        private void DrawShuttleRow(Rect rect, Building_PassengerShuttle shuttle)
        {
            bool available = PassengerShuttleLaunchBridge.TryGetLaunchParts(shuttle, out CompLaunchable launchable, out CompTransporter transporter, out string failReason);
            string status = GetStatusText(available, launchable, transporter, failReason);
            string disabledReason = getDisabledReason?.Invoke(shuttle);
            bool canSelect = disabledReason.NullOrEmpty();

            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.Label(new Rect(rect.x, rect.y + 6f, rect.width - 130f, 24f), shuttle?.LabelCap ?? "BSL_StatusUnavailable".Translate());
            Widgets.Label(new Rect(rect.x + rect.width - 245f, rect.y + 6f, 120f, 24f), status);

            Rect buttonRect = new Rect(rect.x + rect.width - 110f, rect.y + 4f, 100f, 28f);
            if (canSelect)
            {
                if (Widgets.ButtonText(buttonRect, "BSL_Select".Translate()))
                {
                    Close();
                    selectAction(shuttle);
                }
            }
            else
            {
                Widgets.DrawHighlight(buttonRect);
                TooltipHandler.TipRegion(buttonRect, disabledReason.NullOrEmpty() ? failReason : disabledReason);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(buttonRect, "BSL_Select".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private static string GetStatusText(bool available, CompLaunchable launchable, CompTransporter transporter, string failReason)
        {
            if (!available)
            {
                return failReason.NullOrEmpty() ? "BSL_StatusUnavailable".Translate() : failReason;
            }

            if (!transporter.LoadingInProgressOrReadyToLaunch)
            {
                return "BSL_StatusIdle".Translate();
            }

            if (transporter.AnyInGroupHasAnythingLeftToLoad)
            {
                return "BSL_StatusLoading".Translate();
            }

            AcceptanceReport canLaunch = launchable.CanLaunch();
            return canLaunch.Accepted ? "BSL_StatusReady".Translate() : canLaunch.Reason;
        }

        private static string GetStateAwareLaunchDisabledReason(Building_PassengerShuttle shuttle)
        {
            return PassengerShuttleLaunchBridge.CanStartStateAwareWorldMapLaunch(shuttle, out string disabledReason) ? null : disabledReason;
        }
    }
}
