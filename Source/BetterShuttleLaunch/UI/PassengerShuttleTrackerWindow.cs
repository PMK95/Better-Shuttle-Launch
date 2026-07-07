using System.Collections.Generic;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.Settings;
using BetterShuttleLaunch.Shuttles;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class PassengerShuttleTrackerWindow : Window
    {
        private const float ExpandedWidth = 430f;
        private const float ExpandedHeight = 260f;
        private const float MinimizedHeight = 38f;
        private Vector2 scrollPosition;
        private bool dragging;

        public PassengerShuttleTrackerWindow()
        {
            doWindowBackground = true;
            doCloseX = false;
            draggable = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            closeOnClickedOutside = false;
            closeOnCancel = false;
            forcePause = false;

            BetterShuttleLaunchSettings settings = BetterShuttleLaunchMod.ActiveSettings;
            float x = settings.TrackerWindowX < 0f ? Verse.UI.screenWidth - ExpandedWidth - 18f : settings.TrackerWindowX;
            float y = settings.TrackerWindowY;
            windowRect = new Rect(x, y, ExpandedWidth, settings.TrackerWindowMinimized ? MinimizedHeight : ExpandedHeight);
        }

        public override Vector2 InitialSize => new Vector2(ExpandedWidth, BetterShuttleLaunchMod.ActiveSettings.TrackerWindowMinimized ? MinimizedHeight : ExpandedHeight);

        public override void DoWindowContents(Rect inRect)
        {
            Rect headerRect = new Rect(0f, 0f, inRect.width, 30f);
            HandleRightMouseDrag(headerRect);
            Widgets.Label(new Rect(headerRect.x, headerRect.y + 4f, headerRect.width - 36f, 24f), "BSL_ShuttleTracker".Translate());

            Rect minimizeRect = new Rect(headerRect.xMax - 28f, headerRect.y + 2f, 26f, 26f);
            if (Widgets.ButtonText(minimizeRect, BetterShuttleLaunchMod.ActiveSettings.TrackerWindowMinimized ? "+" : "-"))
            {
                ToggleMinimized();
            }

            if (BetterShuttleLaunchMod.ActiveSettings.TrackerWindowMinimized)
            {
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                Widgets.Label(new Rect(0f, 38f, inRect.width, 26f), "BSL_StatusUnavailable".Translate());
                return;
            }

            List<PassengerShuttleTrackerRow> rows = BuildRows(map);
            if (rows.Count == 0)
            {
                Widgets.Label(new Rect(0f, 38f, inRect.width, 26f), "BSL_NoShuttles".Translate());
                return;
            }

            Rect outRect = new Rect(0f, 36f, inRect.width, inRect.height - 36f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, rows.Count * 66f);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            for (int i = 0; i < rows.Count; i++)
            {
                DrawRow(new Rect(0f, i * 66f, viewRect.width, 60f), rows[i]);
            }

            Widgets.EndScrollView();
        }

        private static List<PassengerShuttleTrackerRow> BuildRows(Map map)
        {
            List<PassengerShuttleTrackerRow> rows = new List<PassengerShuttleTrackerRow>();
            LaunchQueueGameComponent queue = LaunchQueueGameComponent.Current;
            foreach (Building_PassengerShuttle shuttle in PassengerShuttleFinder.FindPassengerShuttles(map))
            {
                rows.Add(new PassengerShuttleTrackerRow(shuttle, queue?.FindQueuedLaunch(shuttle), queue?.FindTrackedFlight(shuttle)));
            }

            if (queue == null || map.Parent == null)
            {
                return rows;
            }

            for (int i = 0; i < queue.TrackedFlights.Count; i++)
            {
                TrackedPassengerShuttleFlight trackedFlight = queue.TrackedFlights[i];
                if (trackedFlight?.Shuttle == null || rows.Exists(row => row.Shuttle == trackedFlight.Shuttle))
                {
                    continue;
                }

                if (trackedFlight.OriginTile == map.Parent.Tile || trackedFlight.DestinationTile == map.Parent.Tile)
                {
                    rows.Add(new PassengerShuttleTrackerRow(trackedFlight.Shuttle, queue.FindQueuedLaunch(trackedFlight.Shuttle), trackedFlight));
                }
            }

            return rows;
        }

        private void DrawRow(Rect rect, PassengerShuttleTrackerRow row)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            PassengerShuttleIconDrawer.Draw(new Rect(rect.x + 4f, rect.y + 6f, 42f, 42f), row.Shuttle);

            Rect labelRect = new Rect(rect.x + 54f, rect.y + 4f, 145f, 22f);
            Widgets.Label(labelRect, row.Shuttle?.LabelCap ?? "BSL_StatusUnavailable".Translate());

            PassengerShuttleFlightState state = GetRowState(row, out string statusText);
            DrawStatusIcon(new Rect(rect.x + 54f, rect.y + 31f, 14f, 14f), state);
            Widgets.Label(new Rect(rect.x + 74f, rect.y + 27f, 125f, 22f), statusText);

            Rect routeRect = new Rect(rect.x + 200f, rect.y + 5f, rect.width - 292f, 48f);
            DrawRoute(routeRect, row, state);

            if (row.QueuedLaunch == null && state != PassengerShuttleFlightState.InFlight)
            {
                Rect buttonRect = new Rect(rect.xMax - 84f, rect.y + 16f, 78f, 28f);
                if (Widgets.ButtonText(buttonRect, "BSL_ReadyShortcut".Translate()))
                {
                    PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow(row.Shuttle);
                }
            }
        }

        private static PassengerShuttleFlightState GetRowState(PassengerShuttleTrackerRow row, out string statusText)
        {
            if (row.QueuedLaunch != null)
            {
                LaunchReadinessResult readiness = LaunchReadinessEvaluator.EvaluateQueuedPassengerShuttleLaunch(row.QueuedLaunch);
                statusText = readiness.StatusText.NullOrEmpty() ? "BSL_StatusQueued".Translate() : readiness.StatusText;
                if (readiness.CanLaunchNow)
                {
                    return PassengerShuttleFlightState.Ready;
                }

                if (row.Shuttle?.TransporterComp != null && row.Shuttle.TransporterComp.AnyInGroupHasAnythingLeftToLoad)
                {
                    return PassengerShuttleFlightState.Loading;
                }

                return PassengerShuttleFlightState.Waiting;
            }

            if (row.TrackedFlight != null)
            {
                statusText = row.TrackedFlight.StatusText.NullOrEmpty() ? "BSL_StatusQueued".Translate() : row.TrackedFlight.StatusText;
                return row.TrackedFlight.State;
            }

            statusText = "BSL_StatusIdle".Translate();
            return PassengerShuttleFlightState.Queued;
        }

        private static void DrawRoute(Rect rect, PassengerShuttleTrackerRow row, PassengerShuttleFlightState state)
        {
            string origin = row.QueuedLaunch?.OriginLabel ?? row.TrackedFlight?.OriginLabel ?? "BSL_StatusIdle".Translate();
            string destination = row.QueuedLaunch?.DestinationLabel ?? row.TrackedFlight?.DestinationLabel ?? "BSL_StatusIdle".Translate();
            Widgets.Label(new Rect(rect.x, rect.y, rect.width * 0.45f, 20f), origin);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.45f, 20f), destination);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect lineRect = new Rect(rect.x + 6f, rect.y + 30f, rect.width - 12f, 2f);
            DrawSolidRect(lineRect, new Color(0.45f, 0.48f, 0.50f, 1f));

            float progress = GetRouteProgress(row, state);
            Rect iconRect = new Rect(lineRect.x + (lineRect.width - 24f) * progress, rect.y + 19f, 24f, 24f);
            PassengerShuttleIconDrawer.Draw(iconRect, row.Shuttle);
            if (state == PassengerShuttleFlightState.Arrived && Find.TickManager.TicksGame % 40 < 20)
            {
                Widgets.DrawBox(iconRect.ExpandedBy(2f), 2);
            }
        }

        private static float GetRouteProgress(PassengerShuttleTrackerRow row, PassengerShuttleFlightState state)
        {
            if (state == PassengerShuttleFlightState.Arrived)
            {
                return 1f;
            }

            if (state == PassengerShuttleFlightState.InFlight && row.TrackedFlight != null && row.TrackedFlight.LaunchedTick > 0)
            {
                return Mathf.Clamp01((Find.TickManager.TicksGame - row.TrackedFlight.LaunchedTick) / 2500f);
            }

            if (state == PassengerShuttleFlightState.Ready)
            {
                return 0.35f;
            }

            if (state == PassengerShuttleFlightState.Loading)
            {
                return 0.18f;
            }

            return 0.08f;
        }

        private static void DrawStatusIcon(Rect rect, PassengerShuttleFlightState state)
        {
            Color color = GetStatusColor(state);
            if (state == PassengerShuttleFlightState.Arrived && Find.TickManager.TicksGame % 40 < 20)
            {
                color = Color.white;
            }

            DrawSolidRect(rect, color);
            Widgets.DrawBox(rect, 1);
        }

        private static Color GetStatusColor(PassengerShuttleFlightState state)
        {
            switch (state)
            {
                case PassengerShuttleFlightState.Loading:
                    return new Color(0.95f, 0.72f, 0.25f, 1f);
                case PassengerShuttleFlightState.Waiting:
                    return new Color(0.42f, 0.68f, 1f, 1f);
                case PassengerShuttleFlightState.Ready:
                    return new Color(0.35f, 0.9f, 0.35f, 1f);
                case PassengerShuttleFlightState.InFlight:
                    return new Color(0.25f, 0.9f, 0.95f, 1f);
                case PassengerShuttleFlightState.Arrived:
                    return new Color(0.65f, 1f, 0.35f, 1f);
                case PassengerShuttleFlightState.Failed:
                    return new Color(1f, 0.25f, 0.22f, 1f);
                default:
                    return new Color(0.55f, 0.55f, 0.55f, 1f);
            }
        }

        private void ToggleMinimized()
        {
            BetterShuttleLaunchSettings settings = BetterShuttleLaunchMod.Settings;
            settings.TrackerWindowMinimized = !settings.TrackerWindowMinimized;
            windowRect.height = settings.TrackerWindowMinimized ? MinimizedHeight : ExpandedHeight;
        }

        private void HandleRightMouseDrag(Rect headerRect)
        {
            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 1 && headerRect.Contains(ev.mousePosition))
            {
                dragging = true;
                ev.Use();
            }

            if (dragging && ev.type == EventType.MouseDrag && ev.button == 1)
            {
                windowRect.position += ev.delta;
                BetterShuttleLaunchMod.Settings.TrackerWindowX = windowRect.x;
                BetterShuttleLaunchMod.Settings.TrackerWindowY = windowRect.y;
                ev.Use();
            }

            if (dragging && ev.rawType == EventType.MouseUp && ev.button == 1)
            {
                dragging = false;
                ev.Use();
            }
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = oldColor;
        }

        private readonly struct PassengerShuttleTrackerRow
        {
            public readonly Building_PassengerShuttle Shuttle;
            public readonly QueuedPassengerShuttleLaunch QueuedLaunch;
            public readonly TrackedPassengerShuttleFlight TrackedFlight;

            public PassengerShuttleTrackerRow(Building_PassengerShuttle shuttle, QueuedPassengerShuttleLaunch queuedLaunch, TrackedPassengerShuttleFlight trackedFlight)
            {
                Shuttle = shuttle;
                QueuedLaunch = queuedLaunch;
                TrackedFlight = trackedFlight;
            }
        }
    }

    [HarmonyPatch(typeof(MapInterface), "MapInterfaceOnGUI_BeforeMainTabs")]
    public static class PassengerShuttleTrackerWindowPatch
    {
        private static PassengerShuttleTrackerWindow trackerWindow;

        public static void Postfix()
        {
            if (!ModsConfig.OdysseyActive || Find.CurrentMap == null || !ShouldShowForCurrentMap(Find.CurrentMap))
            {
                return;
            }

            if (trackerWindow == null || !Find.WindowStack.Windows.Contains(trackerWindow))
            {
                trackerWindow = new PassengerShuttleTrackerWindow();
                Find.WindowStack.Add(trackerWindow);
            }
        }

        private static bool ShouldShowForCurrentMap(Map map)
        {
            if (map.IsPlayerHome)
            {
                return true;
            }

            foreach (Building_PassengerShuttle shuttle in PassengerShuttleFinder.FindPassengerShuttles(map))
            {
                if (shuttle != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
