using System.Collections.Generic;
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
        private Vector2 scrollPosition;
        private bool dragging;
        private bool resizing;

        private static BetterShuttleLaunchUiConfigDef UiConfig => BetterShuttleLaunchUiConfigDef.ActiveConfig;
        private static float MinWidth => UiConfig.trackerMinWidth;
        private static float MinHeight => UiConfig.trackerMinHeight;
        private static float MaxWidth => UiConfig.trackerMaxWidth;
        private static float MaxHeight => UiConfig.trackerMaxHeight;
        private static float MinimizedHeight => UiConfig.trackerMinimizedHeight;
        private static float RowStrideHeight => UiConfig.trackerRowStrideHeight;
        private static float RowDrawHeight => UiConfig.trackerRowDrawHeight;
        private static int EstimatedShuttleTravelTicksPerTile => Mathf.Max(1, UiConfig.estimatedShuttleTravelTicksPerTile);

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
            float width = Mathf.Clamp(settings.TrackerWindowWidth, MinWidth, MaxWidth);
            float height = Mathf.Clamp(settings.TrackerWindowHeight, MinHeight, MaxHeight);
            float x = settings.TrackerWindowX < 0f ? Verse.UI.screenWidth - width - 18f : settings.TrackerWindowX;
            float y = settings.TrackerWindowY;
            windowRect = new Rect(x, y, width, settings.TrackerWindowMinimized ? MinimizedHeight : height);
        }

        public override Vector2 InitialSize => new Vector2(
            Mathf.Clamp(BetterShuttleLaunchMod.ActiveSettings.TrackerWindowWidth, MinWidth, MaxWidth),
            BetterShuttleLaunchMod.ActiveSettings.TrackerWindowMinimized
                ? MinimizedHeight
                : Mathf.Clamp(BetterShuttleLaunchMod.ActiveSettings.TrackerWindowHeight, MinHeight, MaxHeight));

        public override void DoWindowContents(Rect inRect)
        {
            BetterShuttleLaunchTextures.DrawIfAvailable(inRect, BetterShuttleLaunchTextures.TrackerPanelBackground);
            Rect headerRect = new Rect(0f, 0f, inRect.width, 30f);
            BetterShuttleLaunchTextures.DrawIfAvailable(headerRect, BetterShuttleLaunchTextures.TrackerPanelHeader);
            Widgets.DrawBox(headerRect, 1);
            HandleRightMouseDrag(headerRect);
            if (BetterShuttleLaunchTextures.CommandOpenTracker != null)
            {
                BetterShuttleLaunchTextures.DrawIfAvailable(new Rect(headerRect.x + 2f, headerRect.y + 3f, 22f, 22f), BetterShuttleLaunchTextures.CommandOpenTracker, ScaleMode.ScaleToFit);
            }

            Rect titleRect = new Rect(headerRect.x + 28f, headerRect.y + 4f, headerRect.width - 112f, 24f);
            AddOptionalTooltip(titleRect, "BSL_ShuttleTrackerHeaderTooltip".Translate());
            Widgets.Label(titleRect, "BSL_ShuttleTracker".Translate());

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
                    BetterShuttleLaunchMod.ActiveSettings.TrackerWindowMinimized ? "[]" : "-",
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
            List<PassengerShuttleTrackerRow> rows = BuildRows(map);
            if (rows.Count == 0)
            {
                Widgets.Label(new Rect(0f, 38f, inRect.width, 26f), "BSL_NoShuttles".Translate());
                HandleResize(inRect);
                return;
            }

            Rect outRect = new Rect(0f, 36f, inRect.width, inRect.height - 36f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, rows.Count * RowStrideHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            for (int i = 0; i < rows.Count; i++)
            {
                DrawRow(new Rect(0f, i * RowStrideHeight, viewRect.width, RowDrawHeight), rows[i]);
            }

            Widgets.EndScrollView();
            HandleResize(inRect);
        }

        private static List<PassengerShuttleTrackerRow> BuildRows(Map map)
        {
            List<PassengerShuttleTrackerRow> rows = new List<PassengerShuttleTrackerRow>();
            LaunchQueueGameComponent queue = LaunchQueueGameComponent.Current;

            if (BetterShuttleLaunchMod.ActiveSettings.TrackerShowOnlyCurrentMapShuttles)
            {
                if (map != null)
                {
                    AddMapPassengerShuttleRows(rows, map, queue);
                }

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
            AddOptionalTooltip(stateIconRect, "BSL_StateIconTooltip".Translate(statusText));
            DrawCompactStats(new Rect(rect.x + 28f, rect.y + 25f, 108f, 30f), row.Shuttle, row);

            Rect routeRect = new Rect(rect.x + 146f, rect.y + 5f, rect.width - 244f, 48f);
            DrawRoute(routeRect, row, state);

            Rect selectRect = new Rect(rect.x, rect.y, rect.width - 92f, rect.height);
            HandleRowSelection(selectRect, row);

            Rect actionRect = new Rect(rect.xMax - 86f, rect.y + 18f, 80f, 24f);
            if (DrawTrackerButton(actionRect, "BSL_TrackerActions".Translate(), row.Shuttle != null && !row.Shuttle.Destroyed, "BSL_TrackerActionsTooltip".Translate()))
            {
                OpenActionMenu(row);
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

        private static void OpenActionMenu(PassengerShuttleTrackerRow row)
        {
            Caravan caravan = GetRowCaravan(row);
            List<FloatMenuOption> options = caravan != null && !caravan.Destroyed && caravan.Shuttle == row.Shuttle
                ? PassengerShuttleTrackerActionMenu.CreateForCaravan(caravan)
                : PassengerShuttleTrackerActionMenu.CreateForMapShuttle(row.Shuttle);
            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("BSL_NoAvailableLaunchOptions".Translate(), null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void DrawRoute(Rect rect, PassengerShuttleTrackerRow row, PassengerShuttleFlightState state)
        {
            string origin = row.QueuedLaunch?.OriginLabel ?? row.TrackedFlight?.OriginLabel ?? "BSL_StatusIdle".Translate();
            string destination = row.QueuedLaunch?.DestinationLabel ?? row.TrackedFlight?.DestinationLabel ?? "BSL_StatusIdle".Translate();
            PlanetTile originTile = row.QueuedLaunch?.OriginTile ?? row.TrackedFlight?.OriginTile ?? default;
            PlanetTile destinationTile = row.QueuedLaunch?.DestinationTile ?? row.TrackedFlight?.DestinationTile ?? default;
            Rect lineRect;
            if (BetterShuttleLaunchMod.ActiveSettings.ShowTrackerRouteEndpointIcons)
            {
                RouteEndpointInfo originEndpoint = BuildRouteEndpointInfo(originTile, origin, "BSL_RouteEndpointOrigin".Translate());
                RouteEndpointInfo destinationEndpoint = BuildRouteEndpointInfo(destinationTile, destination, "BSL_RouteEndpointDestination".Translate());
                Rect originRect = new Rect(rect.x, rect.y + 18f, 24f, 24f);
                Rect destinationRect = new Rect(rect.xMax - 24f, rect.y + 18f, 24f, 24f);
                lineRect = new Rect(originRect.center.x, rect.y + 30f, destinationRect.center.x - originRect.center.x, 2f);
                DrawRouteEndpointIcon(originRect, originEndpoint);
                DrawRouteEndpointIcon(destinationRect, destinationEndpoint);
                AddOptionalTooltip(lineRect.ExpandedBy(4f), "BSL_RouteTooltip".Translate(origin, destination));
            }
            else
            {
                Widgets.Label(new Rect(rect.x, rect.y, rect.width * 0.45f, 20f), origin);
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(new Rect(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.45f, 20f), destination);
                Text.Anchor = TextAnchor.UpperLeft;
                AddOptionalTooltip(rect, "BSL_RouteTooltip".Translate(origin, destination));
                lineRect = new Rect(rect.x + 6f, rect.y + 30f, rect.width - 12f, 2f);
            }

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

            float markerCenterX = Mathf.Lerp(lineRect.x, lineRect.xMax, progress);
            Rect iconRect = new Rect(markerCenterX - 12f, rect.y + 19f, 24f, 24f);
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

        private static RouteEndpointInfo BuildRouteEndpointInfo(PlanetTile tile, string fallbackLabel, string roleLabel)
        {
            WorldObject worldObject = FindBestWorldObjectAtTile(tile);
            MapParent mapParent = worldObject as MapParent;
            if (mapParent == null && tile.Valid && Find.WorldObjects != null)
            {
                mapParent = Find.WorldObjects.MapParentAt(tile);
            }

            string label = worldObject != null && !worldObject.Destroyed
                ? worldObject.LabelCap
                : fallbackLabel.NullOrEmpty()
                    ? tile.Valid ? tile.ToString() : "BSL_StatusUnavailable".Translate()
                    : fallbackLabel;
            string kindLabel = GetRouteEndpointKindLabel(worldObject);
            Texture2D icon = GetRouteEndpointIcon(worldObject, mapParent, out Color iconColor);
            return new RouteEndpointInfo(tile, worldObject, mapParent, icon, iconColor, roleLabel, label, kindLabel);
        }

        private static WorldObject FindBestWorldObjectAtTile(PlanetTile tile)
        {
            if (!tile.Valid || Find.WorldObjects == null)
            {
                return null;
            }

            MapParent mapParent = Find.WorldObjects.MapParentAt(tile);
            if (mapParent != null && !mapParent.Destroyed)
            {
                return mapParent;
            }

            foreach (WorldObject worldObject in Find.WorldObjects.ObjectsAt(tile))
            {
                if (worldObject != null && !worldObject.Destroyed)
                {
                    return worldObject;
                }
            }

            return null;
        }

        private static Texture2D GetRouteEndpointIcon(WorldObject worldObject, MapParent mapParent, out Color iconColor)
        {
            iconColor = Color.white;
            Texture2D icon = null;
            if (worldObject != null && !worldObject.Destroyed)
            {
                iconColor = worldObject.ExpandingIconColor;
                FactionDef factionDef = worldObject.Faction?.def;
                if (mapParent != null)
                {
                    icon = worldObject.ExpandingIcon;
                    if (icon == null && factionDef != null)
                    {
                        icon = factionDef.SettlementTexture;
                    }

                    if (icon == null)
                    {
                        icon = BetterShuttleLaunchTextures.TrackerEndpointMap;
                    }
                }
                else if (factionDef != null)
                {
                    icon = factionDef.FactionIcon;
                    iconColor = factionDef.DefaultColor;
                    if (icon == null)
                    {
                        icon = worldObject.ExpandingIcon;
                    }

                    if (icon == null)
                    {
                        icon = BetterShuttleLaunchTextures.TrackerEndpointFaction;
                    }
                }
                else
                {
                    icon = worldObject.ExpandingIcon;
                }
            }

            if (icon == null)
            {
                icon = BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.TrackerEndpointEmpty, TexButton.ShowWorldFeatures);
            }

            return icon;
        }

        private static string GetRouteEndpointKindLabel(WorldObject worldObject)
        {
            if (worldObject == null || worldObject.Destroyed)
            {
                return "BSL_RouteEndpointEmpty".Translate();
            }

            if (worldObject is MapParent)
            {
                return "BSL_RouteEndpointMap".Translate();
            }

            if (worldObject.Faction != null)
            {
                return "BSL_RouteEndpointFaction".Translate(worldObject.Faction.Name);
            }

            return worldObject.def?.label?.CapitalizeFirst() ?? "BSL_RouteEndpointWorldObject".Translate();
        }

        private static void DrawRouteEndpointIcon(Rect rect, RouteEndpointInfo endpoint)
        {
            if (ShouldShowTrackerHoverHelpAndHighlight())
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            Widgets.DrawBox(rect, 1);
            if (endpoint.Icon != null)
            {
                Color oldColor = GUI.color;
                GUI.color = endpoint.IconColor;
                GUI.DrawTexture(rect.ContractedBy(3f), endpoint.Icon, ScaleMode.ScaleToFit);
                GUI.color = oldColor;
            }

            AddOptionalTooltip(rect, "BSL_RouteEndpointTooltip".Translate(endpoint.RoleLabel, endpoint.Label, endpoint.KindLabel));
            if (ShouldShowTrackerHoverHelpAndHighlight() && rect.Contains(Event.current.mousePosition))
            {
                HighlightRouteEndpoint(endpoint);
            }

            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && rect.Contains(ev.mousePosition))
            {
                JumpToRouteEndpoint(endpoint);
                ev.Use();
            }
        }

        private static void HighlightRouteEndpoint(RouteEndpointInfo endpoint)
        {
            if (endpoint.MapParent != null && !endpoint.MapParent.Destroyed && endpoint.MapParent.HasMap && endpoint.MapParent.Map == Find.CurrentMap)
            {
                TargetHighlighter.Highlight(new GlobalTargetInfo(endpoint.MapParent.Map.Center, endpoint.MapParent.Map), true, true, true);
                return;
            }

            if (endpoint.WorldObject != null && !endpoint.WorldObject.Destroyed)
            {
                TargetHighlighter.Highlight(new GlobalTargetInfo(endpoint.WorldObject), true, true, true);
                return;
            }

            if (endpoint.Tile.Valid)
            {
                TargetHighlighter.Highlight(new GlobalTargetInfo(endpoint.Tile), true, true, true);
            }
        }

        private static void JumpToRouteEndpoint(RouteEndpointInfo endpoint)
        {
            if (endpoint.MapParent != null && !endpoint.MapParent.Destroyed && endpoint.MapParent.HasMap)
            {
                CameraJumper.TryJump(endpoint.MapParent.Map.Center, endpoint.MapParent.Map, CameraJumper.MovementMode.Pan);
                return;
            }

            if (endpoint.WorldObject != null && !endpoint.WorldObject.Destroyed)
            {
                CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(endpoint.WorldObject), CameraJumper.MovementMode.Pan);
                return;
            }

            if (endpoint.Tile.Valid)
            {
                CameraJumper.TryJump(endpoint.Tile, CameraJumper.MovementMode.Pan);
                Find.WorldSelector.ClearSelection();
                Find.WorldSelector.SelectedTile = endpoint.Tile;
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
            windowRect.height = settings.TrackerWindowMinimized ? MinimizedHeight : Mathf.Clamp(settings.TrackerWindowHeight, MinHeight, MaxHeight);
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

        private void HandleResize(Rect inRect)
        {
            Rect resizeRect = new Rect(inRect.xMax - 16f, inRect.yMax - 16f, 14f, 14f);
            Widgets.DrawBox(resizeRect, 1);
            DrawSolidRect(new Rect(resizeRect.x + 3f, resizeRect.yMax - 4f, resizeRect.width - 5f, 1f), Color.white);
            DrawSolidRect(new Rect(resizeRect.xMax - 4f, resizeRect.y + 3f, 1f, resizeRect.height - 5f), Color.white);

            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && resizeRect.Contains(ev.mousePosition))
            {
                resizing = true;
                ev.Use();
            }

            if (resizing && ev.type == EventType.MouseDrag && ev.button == 0)
            {
                BetterShuttleLaunchSettings settings = BetterShuttleLaunchMod.Settings;
                float newWidth = Mathf.Clamp(windowRect.width + ev.delta.x, MinWidth, MaxWidth);
                float newHeight = Mathf.Clamp(windowRect.height + ev.delta.y, MinHeight, MaxHeight);
                windowRect.width = newWidth;
                windowRect.height = newHeight;
                settings.TrackerWindowWidth = newWidth;
                settings.TrackerWindowHeight = newHeight;
                ev.Use();
            }

            if (resizing && ev.rawType == EventType.MouseUp && ev.button == 0)
            {
                resizing = false;
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

        private static bool ShouldShowTrackerHoverHelpAndHighlight()
        {
            return BetterShuttleLaunchMod.ActiveSettings.ShowTrackerHoverHelpAndHighlight;
        }

        internal static void AddOptionalTooltip(Rect rect, string tooltip)
        {
            if (ShouldShowTrackerHoverHelpAndHighlight() && !tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
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
            AddOptionalTooltip(rect, tooltip);

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

            AddOptionalTooltip(rect, tooltip);
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
            AddOptionalTooltip(rect, "BSL_ShuttleFocusTooltip".Translate(label));
            if (!ShouldShowTrackerHoverHelpAndHighlight() || !rect.Contains(Event.current.mousePosition))
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

        private static Caravan GetRowCaravan(PassengerShuttleTrackerRow row)
        {
            Caravan caravan = row.Caravan ?? row.QueuedLaunch?.Caravan ?? row.TrackedFlight?.Caravan;
            if (caravan != null && !caravan.Destroyed && caravan.Shuttle == row.Shuttle)
            {
                return caravan;
            }

            return FindPlayerCaravanContainingShuttle(row.Shuttle);
        }

        private static Caravan FindPlayerCaravanContainingShuttle(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null || Find.WorldObjects?.Caravans == null)
            {
                return null;
            }

            for (int i = 0; i < Find.WorldObjects.Caravans.Count; i++)
            {
                Caravan caravan = Find.WorldObjects.Caravans[i];
                if (caravan != null && !caravan.Destroyed && caravan.Faction == Faction.OfPlayer && caravan.Shuttle == shuttle)
                {
                    return caravan;
                }
            }

            return null;
        }

        private static void DrawCompactStats(Rect rect, Building_PassengerShuttle shuttle, PassengerShuttleTrackerRow row)
        {
            if (shuttle == null || shuttle.Destroyed)
            {
                Widgets.Label(rect, "BSL_StatusUnavailable".Translate());
                return;
            }

            float fuelRatio = shuttle.MaxFuelLevel <= 0f ? 0f : Mathf.Clamp01(shuttle.FuelLevel / shuttle.MaxFuelLevel);
            float hitPointRatio = shuttle.MaxHitPoints <= 0 ? 0f : (float)shuttle.HitPoints / shuttle.MaxHitPoints;
            float massRatio = GetMassUsageRatio(shuttle.TransporterComp, row);
            float segmentWidth = rect.width / 3f;
            DrawVerticalStatBar(new Rect(rect.x, rect.y, segmentWidth, rect.height), BetterShuttleLaunchTextures.BadgeFuel, "F", fuelRatio, false, "BSL_ShuttleFuelTooltip".Translate(GetFuelUsageText(shuttle)));
            DrawVerticalStatBar(new Rect(rect.x + segmentWidth, rect.y, segmentWidth, rect.height), BetterShuttleLaunchTextures.BadgeHealth, "H", hitPointRatio, false, "BSL_ShuttleHealthTooltip".Translate(shuttle.HitPoints.ToString(), shuttle.MaxHitPoints.ToString()));
            DrawVerticalStatBar(new Rect(rect.x + segmentWidth * 2f, rect.y, segmentWidth, rect.height), BetterShuttleLaunchTextures.BadgeMass, "M", massRatio, true, "BSL_ShuttleMassTooltip".Translate(GetMassUsageText(shuttle.TransporterComp, shuttle.GetStatValue(StatDefOf.Mass))));
        }

        private static void DrawVerticalStatBar(Rect rect, Texture2D badgeTexture, string fallbackLabel, float fillPercent, bool invertColor, string tooltip)
        {
            Rect iconRect = new Rect(rect.center.x - 6f, rect.yMax - 12f, 12f, 12f);
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

            Rect barRect = new Rect(rect.center.x - 4f, rect.y + 1f, 8f, rect.height - 15f);
            Widgets.DrawBox(barRect, 1);
            float clampedFill = Mathf.Clamp01(fillPercent);
            Rect fillRect = new Rect(barRect.x + 1f, barRect.yMax - 1f - (barRect.height - 2f) * clampedFill, barRect.width - 2f, (barRect.height - 2f) * clampedFill);
            DrawSolidRect(fillRect, GetStatBarColor(invertColor ? 1f - clampedFill : clampedFill));
            AddOptionalTooltip(rect, tooltip);
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

        private static string GetFuelUsageText(Building_PassengerShuttle shuttle)
        {
            if (shuttle == null)
            {
                return "0 / 0";
            }

            return shuttle.FuelLevel.ToString("0.#") + " / " + shuttle.MaxFuelLevel.ToString("0.#");
        }

        private readonly struct RouteEndpointInfo
        {
            public readonly PlanetTile Tile;
            public readonly WorldObject WorldObject;
            public readonly MapParent MapParent;
            public readonly Texture2D Icon;
            public readonly Color IconColor;
            public readonly string RoleLabel;
            public readonly string Label;
            public readonly string KindLabel;

            public RouteEndpointInfo(PlanetTile tile, WorldObject worldObject, MapParent mapParent, Texture2D icon, Color iconColor, string roleLabel, string label, string kindLabel)
            {
                Tile = tile;
                WorldObject = worldObject;
                MapParent = mapParent;
                Icon = icon;
                IconColor = iconColor;
                RoleLabel = roleLabel;
                Label = label;
                KindLabel = kindLabel;
            }
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
            if (!ModsConfig.OdysseyActive)
            {
                CloseTrackerWindow();
                return;
            }

            BetterShuttleLaunchSettings settings = BetterShuttleLaunchMod.ActiveSettings;
            if (!settings.ShowTrackerWindow || Find.CurrentMap == null || !ShouldShowForCurrentMap(Find.CurrentMap))
            {
                CloseTrackerWindow();
                return;
            }

            EnsureTrackerWindow();
        }

        internal static void RecreateTrackerWindow()
        {
            CloseTrackerWindow();
        }

        internal static void CloseTrackerWindow()
        {
            if (trackerWindow != null && Find.WindowStack != null && Find.WindowStack.Windows.Contains(trackerWindow))
            {
                trackerWindow.Close(false);
            }

            trackerWindow = null;
        }

        internal static void EnsureTrackerWindow()
        {
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

    [HarmonyPatch(typeof(WorldInterface), nameof(WorldInterface.WorldInterfaceOnGUI))]
    public static class PassengerShuttleTrackerWorldWindowPatch
    {
        public static void Postfix()
        {
            if (!ModsConfig.OdysseyActive)
            {
                PassengerShuttleTrackerWindowPatch.CloseTrackerWindow();
                return;
            }

            if (!BetterShuttleLaunchMod.ActiveSettings.ShowTrackerWindow)
            {
                PassengerShuttleTrackerWindowPatch.CloseTrackerWindow();
                return;
            }

            PassengerShuttleTrackerWindowPatch.EnsureTrackerWindow();
        }
    }

    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class PassengerShuttleTrackerGlobalControlsPatch
    {
        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (!ModsConfig.OdysseyActive || row == null)
            {
                return;
            }

            bool showTrackerWindow = BetterShuttleLaunchMod.ActiveSettings.ShowTrackerWindow;
            bool previous = showTrackerWindow;
            row.ToggleableIcon(
                ref showTrackerWindow,
                BetterShuttleLaunchTextures.OrFallback(BetterShuttleLaunchTextures.CommandOpenTracker, CompLaunchable.LaunchCommandTex),
                "BSL_SettingShowTrackerWindowDesc".Translate());
            if (showTrackerWindow == previous)
            {
                return;
            }

            BetterShuttleLaunchMod.Settings.ShowTrackerWindow = showTrackerWindow;
            if (!showTrackerWindow)
            {
                PassengerShuttleTrackerWindowPatch.CloseTrackerWindow();
            }
        }
    }
}
