using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using BetterShuttleLaunch.LaunchQueue;
using BetterShuttleLaunch.Settings;
using BetterShuttleLaunch.Shuttles;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class PassengerShuttleTrackerWindow : Window
    {
        private const float ExpandedWidth = 430f;
        private const float ExpandedHeight = 260f;
        private const float MinimizedHeight = 38f;
        private const int EstimatedShuttleTravelTicksPerTile = 120;
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
            BetterShuttleLaunchTextures.DrawIfAvailable(inRect, BetterShuttleLaunchTextures.TrackerPanelBackground);
            Rect headerRect = new Rect(0f, 0f, inRect.width, 30f);
            BetterShuttleLaunchTextures.DrawIfAvailable(headerRect, BetterShuttleLaunchTextures.TrackerPanelHeader);
            HandleRightMouseDrag(headerRect);
            if (BetterShuttleLaunchTextures.CommandOpenTracker != null)
            {
                BetterShuttleLaunchTextures.DrawIfAvailable(new Rect(headerRect.x + 2f, headerRect.y + 3f, 22f, 22f), BetterShuttleLaunchTextures.CommandOpenTracker, ScaleMode.ScaleToFit);
            }

            TooltipHandler.TipRegion(headerRect, "BSL_ShuttleTrackerHeaderTooltip".Translate());
            Widgets.Label(new Rect(headerRect.x + 28f, headerRect.y + 4f, headerRect.width - 94f, 24f), "BSL_ShuttleTracker".Translate());

            Rect filterRect = new Rect(headerRect.xMax - 56f, headerRect.y + 2f, 26f, 26f);
            if (DrawIconToggleButton(
                    filterRect,
                    BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.TrackerFilterLocal, TexButton.ShowColonistBar),
                    BetterShuttleLaunchMod.ActiveSettings.TrackerShowOnlyCurrentMapShuttles,
                    BetterShuttleLaunchMod.ActiveSettings.TrackerShowOnlyCurrentMapShuttles
                        ? "BSL_TrackerFilterLocalTooltip".Translate()
                        : "BSL_TrackerFilterAllTooltip".Translate()))
            {
                ToggleCurrentMapFilter();
            }

            Rect minimizeRect = new Rect(headerRect.xMax - 28f, headerRect.y + 2f, 26f, 26f);
            if (DrawTrackerButton(
                    minimizeRect,
                    BetterShuttleLaunchMod.ActiveSettings.TrackerWindowMinimized ? "+" : "-",
                    true,
                    BetterShuttleLaunchMod.ActiveSettings.TrackerWindowMinimized
                        ? "BSL_RestoreTrackerTooltip".Translate()
                        : "BSL_MinimizeTrackerTooltip".Translate()))
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

            if (BetterShuttleLaunchMod.ActiveSettings.TrackerShowOnlyCurrentMapShuttles)
            {
                AddMapPassengerShuttleRows(rows, map, queue);
                return rows;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                AddMapPassengerShuttleRows(rows, Find.Maps[i], queue);
            }

            AddPlayerCaravanShuttleRows(rows, queue);
            AddQueuedLaunchRows(rows, queue, map, false);
            AddTrackedFlightRows(rows, queue, map, false);
            return rows;
        }

        private static void AddMapPassengerShuttleRows(List<PassengerShuttleTrackerRow> rows, Map map, LaunchQueueGameComponent queue)
        {
            if (map == null)
            {
                return;
            }

            foreach (Building_PassengerShuttle shuttle in PassengerShuttleFinder.FindPassengerShuttles(map))
            {
                AddRowIfShuttleIsNotPresent(rows, new PassengerShuttleTrackerRow(shuttle, queue?.FindQueuedLaunch(shuttle), queue?.FindTrackedFlight(shuttle)));
            }
        }

        private static void AddPlayerCaravanShuttleRows(List<PassengerShuttleTrackerRow> rows, LaunchQueueGameComponent queue)
        {
            if (Find.WorldObjects?.Caravans == null)
            {
                return;
            }

            for (int i = 0; i < Find.WorldObjects.Caravans.Count; i++)
            {
                Caravan caravan = Find.WorldObjects.Caravans[i];
                if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer || caravan.Shuttle == null)
                {
                    continue;
                }

                AddRowIfShuttleIsNotPresent(rows, new PassengerShuttleTrackerRow(caravan.Shuttle, queue?.FindQueuedLaunch(caravan), queue?.FindTrackedFlight(caravan.Shuttle), caravan));
            }
        }

        private static void AddQueuedLaunchRows(List<PassengerShuttleTrackerRow> rows, LaunchQueueGameComponent queue, Map currentMap, bool currentMapOnly)
        {
            if (queue == null)
            {
                return;
            }

            for (int i = 0; i < queue.QueuedLaunches.Count; i++)
            {
                QueuedPassengerShuttleLaunch queuedLaunch = queue.QueuedLaunches[i];
                if (!CanShowTrackedShuttleInCurrentFilter(currentMap, queuedLaunch?.Shuttle, queuedLaunch?.OriginTile ?? default, queuedLaunch?.DestinationTile ?? default, queuedLaunch?.Caravan, currentMapOnly))
                {
                    continue;
                }

                AddRowIfShuttleIsNotPresent(rows, new PassengerShuttleTrackerRow(queuedLaunch.Shuttle, queuedLaunch, queue.FindTrackedFlight(queuedLaunch.Shuttle), queuedLaunch.Caravan));
            }
        }

        private static void AddTrackedFlightRows(List<PassengerShuttleTrackerRow> rows, LaunchQueueGameComponent queue, Map currentMap, bool currentMapOnly)
        {
            if (queue == null)
            {
                return;
            }

            for (int i = 0; i < queue.TrackedFlights.Count; i++)
            {
                TrackedPassengerShuttleFlight trackedFlight = queue.TrackedFlights[i];
                if (!CanShowTrackedShuttleInCurrentFilter(currentMap, trackedFlight?.Shuttle, trackedFlight?.OriginTile ?? default, trackedFlight?.DestinationTile ?? default, trackedFlight?.Caravan, currentMapOnly))
                {
                    continue;
                }

                AddRowIfShuttleIsNotPresent(rows, new PassengerShuttleTrackerRow(trackedFlight.Shuttle, queue.FindQueuedLaunch(trackedFlight.Shuttle), trackedFlight, trackedFlight.Caravan));
            }
        }

        private static void AddRowIfShuttleIsNotPresent(List<PassengerShuttleTrackerRow> rows, PassengerShuttleTrackerRow row)
        {
            if (row.Shuttle == null || rows.Exists(existingRow => existingRow.Shuttle == row.Shuttle))
            {
                return;
            }

            rows.Add(row);
        }

        private static bool CanShowTrackedShuttleInCurrentFilter(Map map, Building_PassengerShuttle shuttle, PlanetTile originTile, PlanetTile destinationTile, Caravan caravan, bool currentMapOnly)
        {
            if (shuttle == null || shuttle.Destroyed)
            {
                return false;
            }

            if (!currentMapOnly)
            {
                return true;
            }

            if (map?.Parent == null)
            {
                return false;
            }

            PlanetTile mapTile = map.Parent.Tile;
            if (originTile == mapTile || destinationTile == mapTile)
            {
                return true;
            }

            return caravan != null && !caravan.Destroyed && caravan.Tile == mapTile;
        }

        private void DrawRow(Rect rect, PassengerShuttleTrackerRow row)
        {
            if (!BetterShuttleLaunchTextures.DrawIfAvailable(rect, BetterShuttleLaunchTextures.TrackerRowBackground))
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            Rect labelRect = new Rect(rect.x + 4f, rect.y + 4f, 150f, 22f);
            Widgets.Label(labelRect, row.Shuttle?.LabelCap ?? "BSL_StatusUnavailable".Translate());
            AddShuttleFocusTooltipAndHighlight(labelRect, row);

            PassengerShuttleFlightState state = GetRowState(row, out string statusText);
            Rect stateIconRect = new Rect(rect.x + 4f, rect.y + 31f, 18f, 18f);
            DrawStateIcon(stateIconRect, state, row.Shuttle);
            TooltipHandler.TipRegion(stateIconRect, "BSL_StateIconTooltip".Translate(statusText));
            DrawCompactStats(new Rect(rect.x + 28f, rect.y + 28f, 150f, 26f), row.Shuttle, row);

            Rect routeRect = new Rect(rect.x + 164f, rect.y + 5f, rect.width - 268f, 48f);
            DrawRoute(routeRect, row, state);

            Rect selectRect = new Rect(rect.x, rect.y, rect.width - 92f, rect.height);
            HandleRowSelection(selectRect, row);

            bool quickReturnVisible = ShouldShowQuickReturn(row, state);
            bool focusButtonVisible = CanFocusTrackedRow(row);
            if (focusButtonVisible)
            {
                Rect focusRect = new Rect(rect.xMax - 86f, rect.y + 3f, 80f, 17f);
                if (DrawTrackerButton(focusRect, "BSL_FocusShuttle".Translate(), true, "BSL_FocusShuttleTooltip".Translate()))
                {
                    SelectTrackedEntity(row);
                }
            }

            if (row.QueuedLaunch == null && state != PassengerShuttleFlightState.InFlight)
            {
                float readyY = focusButtonVisible ? rect.y + 21f : rect.y + (quickReturnVisible ? 4f : 16f);
                Rect readyRect = new Rect(rect.xMax - 86f, readyY, 80f, focusButtonVisible ? 17f : 24f);
                if (DrawTrackerButton(readyRect, "BSL_ReadyShortcut".Translate(), true, "BSL_ReadyShortcutTooltip".Translate()))
                {
                    PassengerShuttleLaunchQueueCommandUtility.StartLaunchWhenReadyFlow(row.Shuttle);
                }
            }

            if (quickReturnVisible)
            {
                Rect returnRect = new Rect(rect.xMax - 86f, focusButtonVisible ? rect.y + 40f : rect.y + 32f, 80f, focusButtonVisible ? 17f : 24f);
                if (PassengerShuttleLaunchQueueCommandUtility.CanStartReturnFlow(row.Shuttle))
                {
                    if (DrawTrackerButton(returnRect, "BSL_QuickReturn".Translate(), true, "BSL_QuickReturnTooltip".Translate()))
                    {
                        PassengerShuttleLaunchQueueCommandUtility.StartReturnFlow(row.Shuttle);
                    }
                }
                else
                {
                    DrawTrackerButton(returnRect, "BSL_QuickReturn".Translate(), false, "BSL_DisabledReasonTooltip".Translate("BSL_QuickReturnTooltip".Translate(), "BSL_LastDepartureCellUnavailable".Translate()));
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
                if (row.TrackedFlight.State == PassengerShuttleFlightState.InFlight && HasRowArrived(row))
                {
                    statusText = "BSL_StatusArrived".Translate();
                    return PassengerShuttleFlightState.Arrived;
                }

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
            TooltipHandler.TipRegion(rect, "BSL_RouteTooltip".Translate(origin, destination));

            Rect lineRect = new Rect(rect.x + 6f, rect.y + 30f, rect.width - 12f, 2f);
            Rect railRect = lineRect.ExpandedBy(0f, 3f);
            if (!BetterShuttleLaunchTextures.DrawIfAvailable(railRect, BetterShuttleLaunchTextures.TrackerProgressRail))
            {
                DrawSolidRect(lineRect, new Color(0.45f, 0.48f, 0.50f, 1f));
            }

            float progress = GetRouteProgress(row, state);
            if (BetterShuttleLaunchTextures.TrackerProgressFill != null)
            {
                Rect fillRect = new Rect(railRect.x, railRect.y, railRect.width * progress, railRect.height);
                BetterShuttleLaunchTextures.DrawIfAvailable(fillRect, BetterShuttleLaunchTextures.TrackerProgressFill);
            }

            Rect iconRect = new Rect(lineRect.x + (lineRect.width - 24f) * progress, rect.y + 19f, 24f, 24f);
            if (BetterShuttleLaunchTextures.TrackerShuttleMarker != null)
            {
                BetterShuttleLaunchTextures.DrawIfAvailable(iconRect.ExpandedBy(2f), BetterShuttleLaunchTextures.TrackerShuttleMarker, ScaleMode.ScaleToFit);
                PassengerShuttleIconDrawer.Draw(iconRect.ContractedBy(3f), row.Shuttle);
            }
            else
            {
                PassengerShuttleIconDrawer.Draw(iconRect, row.Shuttle);
            }

            AddShuttleFocusTooltipAndHighlight(iconRect, row);
            if (state == PassengerShuttleFlightState.Arrived && Find.TickManager.TicksGame % 40 < 20)
            {
                Widgets.DrawBox(iconRect.ExpandedBy(2f), 2);
            }
        }

        private static float GetRouteProgress(PassengerShuttleTrackerRow row, PassengerShuttleFlightState state)
        {
            if (state == PassengerShuttleFlightState.Arrived || HasRowArrived(row))
            {
                return 1f;
            }

            PlanetTile originTile = row.QueuedLaunch?.OriginTile ?? row.TrackedFlight?.OriginTile ?? default;
            PlanetTile destinationTile = row.QueuedLaunch?.DestinationTile ?? row.TrackedFlight?.DestinationTile ?? default;
            PlanetTile currentTile = GetCurrentWorldTile(row);
            if (originTile.Valid && destinationTile.Valid && currentTile.Valid && originTile != destinationTile)
            {
                int totalDistance = Find.WorldGrid.TraversalDistanceBetween(originTile, destinationTile, true, int.MaxValue, true);
                if (totalDistance > 0)
                {
                    float remainingDistance = GetInterpolatedRemainingDistance(row, currentTile, destinationTile);
                    return Mathf.Clamp01((totalDistance - remainingDistance) / totalDistance);
                }
            }

            if (state == PassengerShuttleFlightState.InFlight)
            {
                return GetEstimatedInFlightProgress(row);
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

        private static float GetInterpolatedRemainingDistance(PassengerShuttleTrackerRow row, PlanetTile currentTile, PlanetTile destinationTile)
        {
            float currentRemainingDistance = Find.WorldGrid.TraversalDistanceBetween(currentTile, destinationTile, true, int.MaxValue, true);
            Caravan caravan = GetRowCaravan(row);
            if (caravan?.pather == null || !caravan.pather.MovingNow || !caravan.pather.nextTile.Valid || caravan.pather.nextTileCostTotal <= 0f)
            {
                return currentRemainingDistance;
            }

            float nextRemainingDistance = Find.WorldGrid.TraversalDistanceBetween(caravan.pather.nextTile, destinationTile, true, int.MaxValue, true);
            float segmentProgress = Mathf.Clamp01(1f - caravan.pather.nextTileCostLeft / caravan.pather.nextTileCostTotal);
            return Mathf.Lerp(currentRemainingDistance, nextRemainingDistance, segmentProgress);
        }

        private static float GetEstimatedInFlightProgress(PassengerShuttleTrackerRow row)
        {
            TrackedPassengerShuttleFlight trackedFlight = row.TrackedFlight;
            if (trackedFlight == null || trackedFlight.LaunchedTick <= 0)
            {
                return 0.5f;
            }

            PlanetTile originTile = trackedFlight.OriginTile;
            PlanetTile destinationTile = trackedFlight.DestinationTile;
            if (!originTile.Valid || !destinationTile.Valid)
            {
                return 0.5f;
            }

            int distance = Find.WorldGrid.TraversalDistanceBetween(originTile, destinationTile, true, int.MaxValue, true);
            int estimatedDurationTicks = distance * EstimatedShuttleTravelTicksPerTile;
            if (estimatedDurationTicks < 600)
            {
                estimatedDurationTicks = 600;
            }

            return Mathf.Clamp01((float)(Find.TickManager.TicksGame - trackedFlight.LaunchedTick) / estimatedDurationTicks);
        }

        private static void DrawStateIcon(Rect rect, PassengerShuttleFlightState state, Building_PassengerShuttle shuttle)
        {
            Texture2D customStatusIcon = BetterShuttleLaunchTextures.GetStatusIcon(state);
            if (customStatusIcon != null)
            {
                if (state != PassengerShuttleFlightState.Arrived || Find.TickManager.TicksGame % 40 < 20)
                {
                    BetterShuttleLaunchTextures.DrawIfAvailable(rect, customStatusIcon, ScaleMode.ScaleToFit);
                }

                return;
            }

            switch (state)
            {
                case PassengerShuttleFlightState.Loading:
                    DrawHorizontalBars(rect);
                    break;
                case PassengerShuttleFlightState.Waiting:
                    DrawDiamond(rect);
                    break;
                case PassengerShuttleFlightState.Ready:
                    DrawTriangle(rect);
                    break;
                case PassengerShuttleFlightState.InFlight:
                    PassengerShuttleIconDrawer.Draw(rect, shuttle);
                    break;
                case PassengerShuttleFlightState.Arrived:
                    DrawCheck(rect);
                    break;
                case PassengerShuttleFlightState.Failed:
                    DrawCross(rect);
                    break;
                default:
                    DrawBoxIcon(rect);
                    break;
            }
        }

        private static void DrawHorizontalBars(Rect rect)
        {
            DrawSolidRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width - 4f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 2f, rect.y + 8f, rect.width - 4f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 2f, rect.y + 13f, rect.width - 4f, 2f), Color.white);
        }

        private static void DrawDiamond(Rect rect)
        {
            DrawSolidRect(new Rect(rect.x + 8f, rect.y + 2f, 2f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 5f, rect.y + 5f, 8f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 2f, rect.y + 8f, 14f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 5f, rect.y + 11f, 8f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 8f, rect.y + 14f, 2f, 2f), Color.white);
        }

        private static void DrawTriangle(Rect rect)
        {
            DrawSolidRect(new Rect(rect.x + 8f, rect.y + 2f, 2f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 6f, rect.y + 6f, 6f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 4f, rect.y + 10f, 10f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 2f, rect.y + 14f, 14f, 2f), Color.white);
        }

        private static void DrawCheck(Rect rect)
        {
            if (Find.TickManager.TicksGame % 40 >= 20)
            {
                return;
            }

            DrawSolidRect(new Rect(rect.x + 3f, rect.y + 10f, 4f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 6f, rect.y + 12f, 3f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 9f, rect.y + 8f, 3f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 12f, rect.y + 5f, 3f, 2f), Color.white);
        }

        private static void DrawCross(Rect rect)
        {
            DrawSolidRect(new Rect(rect.x + 4f, rect.y + 4f, 10f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 4f, rect.y + 12f, 10f, 2f), Color.white);
            DrawSolidRect(new Rect(rect.x + 8f, rect.y + 6f, 2f, 6f), Color.white);
        }

        private static void DrawBoxIcon(Rect rect)
        {
            Widgets.DrawBox(rect, 1);
        }

        private void ToggleMinimized()
        {
            BetterShuttleLaunchSettings settings = BetterShuttleLaunchMod.Settings;
            settings.TrackerWindowMinimized = !settings.TrackerWindowMinimized;
            windowRect.height = settings.TrackerWindowMinimized ? MinimizedHeight : ExpandedHeight;
        }

        private void ToggleCurrentMapFilter()
        {
            BetterShuttleLaunchSettings settings = BetterShuttleLaunchMod.Settings;
            settings.TrackerShowOnlyCurrentMapShuttles = !settings.TrackerShowOnlyCurrentMapShuttles;
            scrollPosition = Vector2.zero;
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

        private static bool DrawTrackerButton(Rect rect, string label, bool enabled = true, string tooltip = null)
        {
            Texture2D buttonTexture = null;
            if (enabled)
            {
                buttonTexture = rect.Contains(Event.current.mousePosition)
                    ? BetterShuttleLaunchTextures.TrackerButtonHover
                    : BetterShuttleLaunchTextures.TrackerButtonNormal;
            }
            else
            {
                buttonTexture = BetterShuttleLaunchTextures.TrackerButtonDisabled;
            }

            if (buttonTexture == null)
            {
                if (enabled && rect.Contains(Event.current.mousePosition))
                {
                    Widgets.DrawHighlight(rect);
                }
                else if (!enabled)
                {
                    Widgets.DrawHighlight(rect);
                }

                Widgets.DrawBox(rect, 1);
            }
            else
            {
                BetterShuttleLaunchTextures.DrawIfAvailable(rect, buttonTexture);
            }

            GameFont oldFont = Text.Font;
            if (rect.height < 20f)
            {
                Text.Font = GameFont.Tiny;
            }

            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            Event ev = Event.current;
            if (!enabled || ev.type != EventType.MouseDown || ev.button != 0 || !rect.Contains(ev.mousePosition))
            {
                return false;
            }

            ev.Use();
            return true;
        }

        private static bool DrawIconToggleButton(Rect rect, Texture2D icon, bool active, string tooltip)
        {
            if (active)
            {
                Widgets.DrawHighlight(rect);
            }
            else if (rect.Contains(Event.current.mousePosition))
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            Widgets.DrawBox(rect, 1);
            if (icon != null)
            {
                Color oldColor = GUI.color;
                GUI.color = active ? Color.white : new Color(0.78f, 0.78f, 0.78f, 1f);
                GUI.DrawTexture(rect.ContractedBy(4f), icon, ScaleMode.ScaleToFit);
                GUI.color = oldColor;
            }

            TooltipHandler.TipRegion(rect, tooltip);
            Event ev = Event.current;
            if (ev.type != EventType.MouseDown || ev.button != 0 || !rect.Contains(ev.mousePosition))
            {
                return false;
            }

            ev.Use();
            return true;
        }

        private static void HandleRowSelection(Rect rect, PassengerShuttleTrackerRow row)
        {
            Event ev = Event.current;
            if (ev.type != EventType.MouseDown || ev.button != 0 || !rect.Contains(ev.mousePosition))
            {
                return;
            }

            SelectTrackedEntity(row);
            ev.Use();
        }

        private static void AddShuttleFocusTooltipAndHighlight(Rect rect, PassengerShuttleTrackerRow row)
        {
            string label = row.Shuttle?.LabelCap ?? row.Caravan?.LabelCap ?? "BSL_StatusUnavailable".Translate();
            TooltipHandler.TipRegion(rect, "BSL_ShuttleFocusTooltip".Translate(label));
            if (!rect.Contains(Event.current.mousePosition))
            {
                return;
            }

            HighlightShuttleOnCurrentMap(row);
        }

        private static void HighlightShuttleOnCurrentMap(PassengerShuttleTrackerRow row)
        {
            if (row.Shuttle == null || row.Shuttle.Destroyed || !row.Shuttle.Spawned || row.Shuttle.Map != Find.CurrentMap)
            {
                return;
            }

            TargetHighlighter.Highlight(new GlobalTargetInfo(row.Shuttle), true, true, true);
        }

        private static void SelectTrackedEntity(PassengerShuttleTrackerRow row)
        {
            if (row.Shuttle != null && !row.Shuttle.Destroyed && row.Shuttle.Spawned)
            {
                CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(row.Shuttle), CameraJumper.MovementMode.Pan);
                return;
            }

            Caravan caravan = GetRowCaravan(row);
            if (caravan != null && !caravan.Destroyed)
            {
                CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(caravan), CameraJumper.MovementMode.Pan);
                return;
            }

            PlanetTile tile = GetCurrentWorldTile(row);
            if (tile.Valid)
            {
                CameraJumper.TryJump(tile, CameraJumper.MovementMode.Pan);
                Find.WorldSelector.ClearSelection();
                Find.WorldSelector.SelectedTile = tile;
            }
        }

        private static bool CanFocusTrackedRow(PassengerShuttleTrackerRow row)
        {
            if (row.Shuttle != null && !row.Shuttle.Destroyed && row.Shuttle.Spawned)
            {
                return true;
            }

            Caravan caravan = GetRowCaravan(row);
            if (caravan != null && !caravan.Destroyed)
            {
                return true;
            }

            return GetCurrentWorldTile(row).Valid;
        }

        private static PlanetTile GetCurrentWorldTile(PassengerShuttleTrackerRow row)
        {
            Caravan caravan = GetRowCaravan(row);
            if (caravan != null && !caravan.Destroyed && caravan.Tile.Valid)
            {
                return caravan.Tile;
            }

            if (row.Shuttle != null && !row.Shuttle.Destroyed && row.Shuttle.Tile.Valid)
            {
                return row.Shuttle.Tile;
            }

            if (row.TrackedFlight != null && row.TrackedFlight.State == PassengerShuttleFlightState.Arrived && row.TrackedFlight.DestinationTile.Valid)
            {
                return row.TrackedFlight.DestinationTile;
            }

            return default;
        }

        private static bool HasRowArrived(PassengerShuttleTrackerRow row)
        {
            PlanetTile destinationTile = row.QueuedLaunch?.DestinationTile ?? row.TrackedFlight?.DestinationTile ?? default;
            PlanetTile currentTile = GetCurrentWorldTile(row);
            return destinationTile.Valid
                   && currentTile.Valid
                   && currentTile == destinationTile;
        }

        private static bool ShouldShowQuickReturn(PassengerShuttleTrackerRow row, PassengerShuttleFlightState state)
        {
            if (row.Shuttle == null || row.Shuttle.Destroyed || row.QueuedLaunch != null || state == PassengerShuttleFlightState.InFlight)
            {
                return false;
            }

            Map map = row.Shuttle.Map;
            return map?.Parent != null && (!map.IsPlayerHome || map.Parent.Faction != Faction.OfPlayer);
        }

        private static Caravan GetRowCaravan(PassengerShuttleTrackerRow row)
        {
            return row.Caravan ?? row.QueuedLaunch?.Caravan ?? row.TrackedFlight?.Caravan;
        }

        private static string GetCompactStatusText(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null || shuttle.Destroyed)
            {
                return "BSL_StatusUnavailable".Translate();
            }

            int pawnCount = CountLoadedPawns(shuttle.TransporterComp);
            string fuelText = shuttle.FuelLevel.ToStringPercent();
            string hitPointText = shuttle.HitPoints + "/" + shuttle.MaxHitPoints;
            string massText = GetMassUsageText(shuttle.TransporterComp, shuttle.GetStatValue(StatDefOf.Mass));
            return "BSL_ShuttleCompactStats".Translate(fuelText, hitPointText, massText, pawnCount.ToString());
        }

        private static void DrawCompactStats(Rect rect, Building_PassengerShuttle shuttle, PassengerShuttleTrackerRow row)
        {
            if (shuttle == null || shuttle.Destroyed)
            {
                Widgets.Label(rect, "BSL_StatusUnavailable".Translate());
                return;
            }

            int pawnCount = CountLoadedPawns(shuttle.TransporterComp);
            float hitPointRatio = shuttle.MaxHitPoints <= 0 ? 0f : (float)shuttle.HitPoints / shuttle.MaxHitPoints;
            float massRatio = GetMassUsageRatio(shuttle.TransporterComp, row);
            float passengerRatio = Mathf.Clamp01(pawnCount / 8f);
            float segmentWidth = rect.width / 4f;
            DrawVerticalStatBar(new Rect(rect.x, rect.y, segmentWidth, rect.height), BetterShuttleLaunchTextures.BadgeFuel, "F", shuttle.FuelLevel, "BSL_ShuttleFuelTooltip".Translate(shuttle.FuelLevel.ToStringPercent()));
            DrawVerticalStatBar(new Rect(rect.x + segmentWidth, rect.y, segmentWidth, rect.height), BetterShuttleLaunchTextures.BadgeHealth, "H", hitPointRatio, "BSL_ShuttleHealthTooltip".Translate(shuttle.HitPoints.ToString(), shuttle.MaxHitPoints.ToString()));
            DrawVerticalStatBar(new Rect(rect.x + segmentWidth * 2f, rect.y, segmentWidth, rect.height), BetterShuttleLaunchTextures.BadgeMass, "M", massRatio, "BSL_ShuttleMassTooltip".Translate(GetMassUsageText(shuttle.TransporterComp, shuttle.GetStatValue(StatDefOf.Mass))));
            DrawVerticalStatBar(new Rect(rect.x + segmentWidth * 3f, rect.y, segmentWidth, rect.height), BetterShuttleLaunchTextures.BadgePassengers, "P", passengerRatio, "BSL_ShuttlePassengersTooltip".Translate(pawnCount.ToString()));
        }

        private static void DrawVerticalStatBar(Rect rect, Texture2D badgeTexture, string fallbackLabel, float fillPercent, string tooltip)
        {
            Rect iconRect = new Rect(rect.x + 1f, rect.y + 1f, 12f, 12f);
            if (badgeTexture != null)
            {
                BetterShuttleLaunchTextures.DrawIfAvailable(iconRect, badgeTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                GameFont oldFont = Text.Font;
                Text.Font = GameFont.Tiny;
                TextAnchor oldAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(iconRect, fallbackLabel);
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
            }

            Rect barRect = new Rect(rect.x + 16f, rect.y + 2f, 7f, rect.height - 4f);
            Widgets.DrawBox(barRect, 1);
            float clampedFill = Mathf.Clamp01(fillPercent);
            Rect fillRect = new Rect(barRect.x + 1f, barRect.yMax - 1f - (barRect.height - 2f) * clampedFill, barRect.width - 2f, (barRect.height - 2f) * clampedFill);
            DrawSolidRect(fillRect, GetStatBarColor(clampedFill));
            TooltipHandler.TipRegion(rect, tooltip);
        }

        private static Color GetStatBarColor(float fillPercent)
        {
            if (fillPercent < 0.25f)
            {
                return new Color(0.85f, 0.25f, 0.20f, 1f);
            }

            if (fillPercent < 0.55f)
            {
                return new Color(0.95f, 0.72f, 0.24f, 1f);
            }

            return new Color(0.35f, 0.78f, 0.45f, 1f);
        }

        internal static int CountLoadedPawns(CompTransporter transporter)
        {
            if (transporter == null)
            {
                return 0;
            }

            FieldInfo innerContainerField = typeof(CompTransporter).GetField("innerContainer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (!(innerContainerField?.GetValue(transporter) is IEnumerable things))
            {
                return 0;
            }

            int count = 0;
            foreach (object thing in things)
            {
                if (thing is Pawn)
                {
                    count++;
                }
            }

            return count;
        }

        private static float GetMassUsageRatio(CompTransporter transporter, PassengerShuttleTrackerRow row)
        {
            if (transporter != null && transporter.MassCapacity > 0f)
            {
                return Mathf.Clamp01(transporter.MassUsage / transporter.MassCapacity);
            }

            Caravan caravan = GetRowCaravan(row);
            if (caravan != null && caravan.MassCapacity > 0f)
            {
                return Mathf.Clamp01(caravan.MassUsage / caravan.MassCapacity);
            }

            return 0f;
        }

        private static string GetMassUsageText(CompTransporter transporter, float fallbackMass)
        {
            if (transporter != null && transporter.MassCapacity > 0f)
            {
                return transporter.MassUsage.ToStringMass() + " / " + transporter.MassCapacity.ToStringMass();
            }

            return fallbackMass.ToStringMass();
        }

        private readonly struct PassengerShuttleTrackerRow
        {
            public readonly Building_PassengerShuttle Shuttle;
            public readonly QueuedPassengerShuttleLaunch QueuedLaunch;
            public readonly TrackedPassengerShuttleFlight TrackedFlight;
            public readonly Caravan Caravan;

            public PassengerShuttleTrackerRow(Building_PassengerShuttle shuttle, QueuedPassengerShuttleLaunch queuedLaunch, TrackedPassengerShuttleFlight trackedFlight, Caravan caravan = null)
            {
                Shuttle = shuttle;
                QueuedLaunch = queuedLaunch;
                TrackedFlight = trackedFlight;
                Caravan = caravan;
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

            DrawLoadedPassengerShuttleMapOverlays(Find.CurrentMap);
            if (trackerWindow == null || !Find.WindowStack.Windows.Contains(trackerWindow))
            {
                trackerWindow = new PassengerShuttleTrackerWindow();
                Find.WindowStack.Add(trackerWindow);
            }
        }

        private static void DrawLoadedPassengerShuttleMapOverlays(Map map)
        {
            if (map == null)
            {
                return;
            }

            foreach (Building_PassengerShuttle shuttle in PassengerShuttleFinder.FindPassengerShuttles(map))
            {
                int pawnCount = PassengerShuttleTrackerWindow.CountLoadedPawns(shuttle.TransporterComp);
                if (pawnCount <= 0)
                {
                    continue;
                }

                Vector2 labelPosition = GenMapUI.LabelDrawPosFor(shuttle, 1.2f);
                Rect iconRect = new Rect(labelPosition.x - 9f, labelPosition.y - 28f, 18f, 18f);
                Texture2D passengerIcon = BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.BadgePassengers, TexButton.ShowColonistBar);
                if (passengerIcon != null)
                {
                    GUI.DrawTexture(iconRect, passengerIcon, ScaleMode.ScaleToFit);
                }
                else
                {
                    Widgets.DrawBox(iconRect, 1);
                }

                GameFont oldFont = Text.Font;
                TextAnchor oldAnchor = Text.Anchor;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(iconRect.xMax - 2f, iconRect.yMax - 10f, 18f, 12f), pawnCount.ToString());
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;

                TooltipHandler.TipRegion(iconRect.ExpandedBy(4f), "BSL_MapPassengerOverlayTooltip".Translate(shuttle.LabelCap, pawnCount.ToString()));
                if (iconRect.ExpandedBy(4f).Contains(Event.current.mousePosition))
                {
                    TargetHighlighter.Highlight(new GlobalTargetInfo(shuttle), true, true, true);
                }
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

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                if (Find.Maps[i] == map)
                {
                    continue;
                }

                foreach (Building_PassengerShuttle shuttle in PassengerShuttleFinder.FindPassengerShuttles(Find.Maps[i]))
                {
                    if (shuttle != null)
                    {
                        return true;
                    }
                }
            }

            if (Find.WorldObjects?.Caravans != null)
            {
                for (int i = 0; i < Find.WorldObjects.Caravans.Count; i++)
                {
                    Caravan caravan = Find.WorldObjects.Caravans[i];
                    if (caravan != null && !caravan.Destroyed && caravan.Faction == Faction.OfPlayer && caravan.Shuttle != null)
                    {
                        return true;
                    }
                }
            }

            LaunchQueueGameComponent queue = LaunchQueueGameComponent.Current;
            if (queue != null && map.Parent != null)
            {
                PlanetTile mapTile = map.Parent.Tile;
                for (int i = 0; i < queue.QueuedLaunches.Count; i++)
                {
                    QueuedPassengerShuttleLaunch queuedLaunch = queue.QueuedLaunches[i];
                    if (queuedLaunch != null && (queuedLaunch.OriginTile == mapTile || queuedLaunch.DestinationTile == mapTile))
                    {
                        return true;
                    }
                }

                for (int i = 0; i < queue.TrackedFlights.Count; i++)
                {
                    TrackedPassengerShuttleFlight trackedFlight = queue.TrackedFlights[i];
                    if (trackedFlight != null && (trackedFlight.OriginTile == mapTile || trackedFlight.DestinationTile == mapTile))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
