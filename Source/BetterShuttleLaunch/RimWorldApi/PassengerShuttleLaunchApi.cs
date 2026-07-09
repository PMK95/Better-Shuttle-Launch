using System;
using System.Collections.Generic;
using BetterShuttleLaunch.Domain;
using BetterShuttleLaunch.UI;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.RimWorldApi
{
    public static class PassengerShuttleLaunchApi
    {
        public static bool OpenLoadDialogThenRun(
            ShuttleContext context,
            Action<IReadOnlyList<Pawn>> afterAccepted)
        {
            if (context == null)
            {
                Messages.Message("BSL_ShuttleUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (context.IsCaravan)
            {
                afterAccepted?.Invoke(Array.Empty<Pawn>());
                return true;
            }

            if (!ShuttleContext.TryForMapShuttle(context.Shuttle, out ShuttleContext currentContext, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            Find.WindowStack.Add(new Dialog_LoadTransportersThenRunAction(
                currentContext.Map,
                new List<CompTransporter> { currentContext.Transporter },
                afterAccepted));
            return true;
        }

        public static bool TryStartQueuedWorldTargeting(
            ShuttleContext context,
            Action<PlanetTile, string, TransportersArrivalAction> destinationChosen)
        {
            if (!CanStartWorldTargeting(context, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            StopWorldTargeting();
            if (!context.IsCaravan && !TryJumpToWorldTile(context.OriginTile))
            {
                Messages.Message("BSL_DestinationTargetingStartFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            Find.WorldSelector.ClearSelection();
            QueuedLaunchTargetingSession.Begin(context);
            Find.WorldTargeter.BeginTargeting(
                target =>
                {
                    return ShowQueuedWorldTargetFloatMenu(context, target, destinationChosen);
                },
                true,
                CompLaunchable.TargeterMouseAttachment,
                false,
                QueuedLaunchTargetingSession.DrawVanillaStyleRangeRingsForActiveTargeting,
                target => GetQueuedTargetingLabel(context, target),
                target => CanSelectQueuedWorldTarget(context, target, out _),
                context.OriginTile,
                true);
            return true;
        }

        public static bool TryStartQueuedLandingCellTargeter(
            ShuttleContext context,
            MapParent destination,
            Action<PlanetTile, TransportersArrivalAction> destinationChosen)
        {
            if (!TryGetTransportersForArrival(context, out List<CompTransporter> transporters, out string failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!CanStartQueuedLandingCellTargeter(context, transporters, destination, out failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (Find.Targeter == null)
            {
                Messages.Message("BSL_DestinationTargetingStartFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            try
            {
                CameraJumper.TryJump(destination.Map.Center, destination.Map, CameraJumper.MovementMode.Pan);
            }
            catch (Exception exception)
            {
                Log.Warning("[Better Shuttle Launch] 예약 발사 착륙 위치 선택을 위해 목적지 맵으로 이동하는 중 오류가 발생했습니다: " + exception);
                Messages.Message("BSL_DestinationTargetingStartFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            TargetingParameters targetingParameters = TargetingParameters.ForCell();
            targetingParameters.validator = target => TryCreateQueuedSpecificLandingAction(
                context,
                destination,
                target.Cell,
                null,
                out _,
                out _,
                out _);

            Find.Targeter.StopTargeting();
            Find.Targeter.BeginTargeting(
                targetingParameters,
                target =>
                {
                    if (!TryCreateQueuedSpecificLandingAction(
                            context,
                            destination,
                            target.Cell,
                            null,
                            out PlanetTile destinationTile,
                            out TransportersArrivalAction arrivalAction,
                            out string selectionFailReason))
                    {
                        Messages.Message(selectionFailReason, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    Find.Targeter.StopTargeting();
                    destinationChosen(destinationTile, arrivalAction);
                },
                null);
            return true;
        }

        public static bool TryCreateQueuedSpecificLandingAction(
            ShuttleContext context,
            MapParent destination,
            IntVec3 cell,
            Rot4? rotation,
            out PlanetTile destinationTile,
            out TransportersArrivalAction arrivalAction,
            out string failReason)
        {
            destinationTile = default;
            arrivalAction = null;
            if (!TryGetTransportersForArrival(context, out List<CompTransporter> transporters, out failReason))
            {
                return false;
            }

            if (!CanStartQueuedLandingCellTargeter(context, transporters, destination, out failReason))
            {
                return false;
            }

            if (!cell.IsValid || !cell.InBounds(destination.Map))
            {
                failReason = "BSL_DestinationInvalid".Translate();
                return false;
            }

            AcceptanceReport canLand = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(
                new LocalTargetInfo(cell),
                destination.Map,
                context.Shuttle.def,
                rotation);
            if (!canLand.Accepted)
            {
                failReason = canLand.Reason;
                return false;
            }

            destinationTile = destination.Tile;
            arrivalAction = rotation.HasValue
                ? new TransportersArrivalAction_LandInSpecificCell(destination, cell, rotation.Value, true)
                : new TransportersArrivalAction_LandInSpecificCell(destination, cell);
            return true;
        }

        public static bool TryLaunchImmediately(QueuedShuttleLaunch queuedLaunch, out string failReason)
        {
            failReason = null;
            if (!TryGetContext(queuedLaunch, out ShuttleContext context, out failReason))
            {
                return false;
            }

            if (context.IsCaravan)
            {
                AcceptanceReport canLaunchCaravan = CaravanShuttleUtility.CanLaunchCaravanShuttle(context.Caravan);
                if (!canLaunchCaravan.Accepted)
                {
                    failReason = canLaunchCaravan.Reason;
                    return false;
                }

                CaravanShuttleUtility.LaunchShuttle(context.Caravan, queuedLaunch.DestinationTile, queuedLaunch.ArrivalAction);
                return true;
            }

            AcceptanceReport canLaunch = context.Launchable.CanLaunch();
            if (!canLaunch.Accepted)
            {
                failReason = canLaunch.Reason;
                return false;
            }

            context.Launchable.TryLaunch(queuedLaunch.DestinationTile, queuedLaunch.ArrivalAction);
            return true;
        }

        public static bool TryGetContext(QueuedShuttleLaunch queuedLaunch, out ShuttleContext context, out string failReason)
        {
            context = null;
            failReason = null;
            if (queuedLaunch == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            return queuedLaunch.Caravan != null
                ? ShuttleContext.TryForCaravan(queuedLaunch.Caravan, out context, out failReason)
                : ShuttleContext.TryForMapShuttle(queuedLaunch.Shuttle, out context, out failReason);
        }

        public static bool TryGetTransportersForArrival(ShuttleContext context, out List<CompTransporter> transporters, out string failReason)
        {
            transporters = null;
            failReason = null;
            if (context == null || context.Transporter == null)
            {
                failReason = "BSL_ShuttleUnavailable".Translate();
                return false;
            }

            if (context.IsCaravan)
            {
                transporters = new List<CompTransporter> { context.Transporter };
                return true;
            }

            transporters = context.Transporter.TransportersInGroup(context.Map);
            if (transporters != null)
            {
                return true;
            }

            failReason = "BSL_LoadingCanceled".Translate();
            return false;
        }

        public static void StopWorldTargeting()
        {
            QueuedLaunchTargetingSession.Clear();
            if (Find.WorldTargeter != null && Find.WorldTargeter.IsTargeting)
            {
                Find.WorldTargeter.StopTargeting();
            }
        }

        public static string GetWorldTargetLabel(GlobalTargetInfo target)
        {
            if (target.HasWorldObject && target.WorldObject != null && !target.WorldObject.Destroyed)
            {
                return target.WorldObject.LabelCap;
            }

            return GetWorldTargetLabel(target.Tile);
        }

        public static string GetWorldTargetLabel(PlanetTile tile)
        {
            if (tile.Valid && Find.WorldObjects != null)
            {
                WorldObject worldObject = Find.WorldObjects.MapParentAt(tile);
                if (worldObject == null)
                {
                    foreach (WorldObject candidate in Find.WorldObjects.ObjectsAt(tile))
                    {
                        if (candidate != null && !candidate.Destroyed)
                        {
                            worldObject = candidate;
                            break;
                        }
                    }
                }

                if (worldObject != null && !worldObject.Destroyed)
                {
                    return worldObject.LabelCap;
                }
            }

            return tile.Valid ? tile.ToString() : "BSL_StatusUnavailable".Translate();
        }

        private static bool CanStartWorldTargeting(ShuttleContext context, out string failReason)
        {
            failReason = null;
            if (Find.WorldTargeter == null || context == null || context.Launchable == null || !context.OriginTile.Valid)
            {
                failReason = "BSL_DestinationTargetingStartFailed".Translate();
                return false;
            }

            return true;
        }

        private static bool TryJumpToWorldTile(PlanetTile originTile)
        {
            if (!originTile.Valid)
            {
                return false;
            }

            try
            {
                CameraJumper.TryJump(originTile, CameraJumper.MovementMode.Pan);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("[Better Shuttle Launch] 예약 발사 목적지 선택을 위해 세계 지도로 이동하는 중 오류가 발생했습니다: " + exception);
                return false;
            }
        }

        private static TaggedString GetQueuedTargetingLabel(ShuttleContext context, GlobalTargetInfo target)
        {
            if (!target.IsValid || !target.Tile.Valid)
            {
                return null;
            }

            if (target.Tile.Layer != context.OriginTile.Layer && !context.OriginTile.Layer.HasConnectionPathTo(target.Tile.Layer))
            {
                GUI.color = ColorLibrary.RedReadable;
                return "TransportPodDestinationNoPath".Translate(target.Tile.Layer.Def.Named("LAYER"));
            }

            if (ModsConfig.OdysseyActive)
            {
                WorldObject blockedWorldObject = Find.World.worldObjects.WorldObjectAt<WorldObject>(target.Tile);
                if (blockedWorldObject != null && blockedWorldObject.RequiresSignalJammerToReach)
                {
                    GUI.color = ColorLibrary.RedReadable;
                    return "TransportPodDestinationRequiresSignalJammer".Translate();
                }
            }

            int maximumRange = context.Launchable.MaxLaunchDistanceEver(target.Tile.Layer);
            int distance = Find.WorldGrid.TraversalDistanceBetween(
                context.OriginTile,
                target.Tile,
                true,
                maximumRange,
                true);
            if (maximumRange > 0 && distance > maximumRange)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "TransportPodDestinationBeyondMaximumRange".Translate();
            }

            List<FloatMenuOption> options = CreateQueuedWorldTargetOptions(context, target, null);
            if (options.Count == 0)
            {
                return string.Empty;
            }

            string fuelCostText = GetVanillaFuelCostText(context, target.Tile, distance);
            if (options.Count == 1)
            {
                if (options[0].Disabled)
                {
                    GUI.color = ColorLibrary.RedReadable;
                }

                return options[0].Label + "\n" + fuelCostText;
            }

            if (target.WorldObject is MapParent mapParent)
            {
                return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap) + "\n" + fuelCostText;
            }

            return "ClickToSeeAvailableOrders_Empty".Translate() + "\n" + fuelCostText;
        }

        private static bool ShowQueuedWorldTargetFloatMenu(
            ShuttleContext context,
            GlobalTargetInfo target,
            Action<PlanetTile, string, TransportersArrivalAction> destinationChosen)
        {
            if (!CanSelectQueuedWorldTarget(context, target, out string disabledReason))
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                StopWorldTargeting();
                return true;
            }

            List<FloatMenuOption> options = CreateQueuedWorldTargetOptions(context, target, destinationChosen);
            if (options.Count == 0)
            {
                LogQueuedWorldTargetOptionFailure(context, target);
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            Find.WindowStack.Add(new FloatMenu(options));
            return false;
        }

        private static List<FloatMenuOption> CreateQueuedWorldTargetOptions(
            ShuttleContext context,
            GlobalTargetInfo target,
            Action<PlanetTile, string, TransportersArrivalAction> destinationChosen)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (!target.IsValid || !target.Tile.Valid || !TryGetTransportersForArrival(context, out List<CompTransporter> transporters, out _))
            {
                return options;
            }

            if (target.HasWorldObject && !target.WorldObject.def.validLaunchTarget)
            {
                return options;
            }

            Action<PlanetTile, TransportersArrivalAction> chooseArrivalAction = (destinationTile, arrivalAction) =>
            {
                if (destinationChosen == null)
                {
                    return;
                }

                StopWorldTargeting();
                destinationChosen(destinationTile, GetWorldTargetLabel(destinationTile), arrivalAction);
            };

            List<Pawn> pawnsExpectedAfterLoading = context.IsCaravan
                ? null
                : context.PawnsExpectedAfterLoading == null
                    ? FindPawnsExpectedAfterLoading(transporters)
                    : new List<Pawn>(context.PawnsExpectedAfterLoading);
            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                WorldObject worldObject = worldObjects[i];
                if (worldObject.Tile != target.Tile)
                {
                    continue;
                }

                int optionCountBeforeWorldObject = options.Count;
                foreach (FloatMenuOption option in worldObject.GetShuttleFloatMenuOptions(transporters, chooseArrivalAction))
                {
                    WrapQueuedTargetOption(option);
                    options.Add(option);
                }

                if (worldObject is Settlement settlement && options.Count == optionCountBeforeWorldObject)
                {
                    AddSettlementOptionsForPawnsWaitingToLoad(
                        context,
                        transporters,
                        settlement,
                        pawnsExpectedAfterLoading,
                        chooseArrivalAction,
                        options);
                }
                else if (options.Count == optionCountBeforeWorldObject
                         && ChooseWhereToLandCompatibility.TryCreateSiteOrSpaceLandingOption(
                             worldObject,
                             transporters,
                             ContainsNonDownedColonist(pawnsExpectedAfterLoading),
                             chooseArrivalAction,
                             out FloatMenuOption compatibilityOption))
                {
                    WrapQueuedTargetOption(compatibilityOption);
                    options.Add(compatibilityOption);
                }
            }

            if (options.Count == 0
                && !Find.World.Impassable(target.Tile)
                && !Find.WorldObjects.AnySettlementBaseAt(target.Tile)
                && !Find.WorldObjects.AnySiteAt(target.Tile)
                && target.Tile.LayerDef.canFormCaravans)
            {
                options.Add(new FloatMenuOption("FormCaravanHere".Translate(), delegate
                {
                    if (destinationChosen == null)
                    {
                        return;
                    }

                    StopWorldTargeting();
                    destinationChosen(
                        target.Tile,
                        GetWorldTargetLabel(target.Tile),
                        new TransportersArrivalAction_FormCaravan("MessageShuttleArrived"));
                }));
            }

            return options;
        }

        private static void LogQueuedWorldTargetOptionFailure(ShuttleContext context, GlobalTargetInfo target)
        {
            if (context == null
                || !target.Tile.Valid
                || (!Find.WorldObjects.AnySettlementBaseAt(target.Tile) && !Find.WorldObjects.AnySiteAt(target.Tile)))
            {
                return;
            }

            int capturedPawnCount = context.PawnsExpectedAfterLoading?.Count ?? -1;
            int detectedPawnCount = TryGetTransportersForArrival(
                context,
                out List<CompTransporter> transporters,
                out _)
                ? FindPawnsExpectedAfterLoading(transporters).Count
                : -1;
            Log.Warning(
                "[Better Shuttle Launch] 예약 발사 목적지 옵션을 만들지 못했습니다. "
                + "목적지=" + GetWorldTargetLabel(target)
                + ", 타일=" + target.Tile
                + ", 적재창에서 캡처한 정착민 수=" + capturedPawnCount
                + ", 현재 적재 및 대기 중인 정착민 수=" + detectedPawnCount
                + ", 셔틀=" + context.Label);
        }

        private static void AddSettlementOptionsForPawnsWaitingToLoad(
            ShuttleContext context,
            List<CompTransporter> transporters,
            Settlement settlement,
            List<Pawn> pawnsExpectedAfterLoading,
            Action<PlanetTile, TransportersArrivalAction> chooseArrivalAction,
            List<FloatMenuOption> options)
        {
            if (context == null
                || context.IsCaravan
                || settlement == null
                || pawnsExpectedAfterLoading == null)
            {
                return;
            }

            if (pawnsExpectedAfterLoading.Count == 0)
            {
                return;
            }

            int firstNewOptionIndex = options.Count;
            if (settlement.Visitable && ContainsPotentialCaravanOwner(pawnsExpectedAfterLoading))
            {
                options.Add(new FloatMenuOption(
                    "VisitSettlement".Translate(settlement.Label),
                    () => chooseArrivalAction(
                        settlement.Tile,
                        new TransportersArrivalAction_VisitSettlement(settlement, "MessageShuttleArrived"))));
            }

            if (CanTradeAfterLoading(pawnsExpectedAfterLoading, settlement))
            {
                options.Add(new FloatMenuOption(
                    "TradeWithSettlement".Translate(settlement.Label),
                    () => chooseArrivalAction(
                        settlement.Tile,
                        new TransportersArrivalAction_Trade(settlement, "MessageShuttleArrived"))));
            }

            AddAttackSettlementOptionForPawnsWaitingToLoad(
                context,
                pawnsExpectedAfterLoading,
                settlement,
                chooseArrivalAction,
                options);

            for (int i = firstNewOptionIndex; i < options.Count; i++)
            {
                WrapQueuedTargetOption(options[i]);
            }
        }

        private static List<Pawn> FindPawnsExpectedAfterLoading(List<CompTransporter> transporters)
        {
            List<Pawn> result = new List<Pawn>();
            HashSet<Pawn> addedPawns = new HashSet<Pawn>();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null)
                {
                    continue;
                }

                ThingOwner loadedThings = transporter.GetDirectlyHeldThings();
                for (int j = 0; j < loadedThings.Count; j++)
                {
                    if (loadedThings[j] is Pawn loadedPawn && addedPawns.Add(loadedPawn))
                    {
                        result.Add(loadedPawn);
                    }
                }

                List<TransferableOneWay> thingsWaitingToLoad = transporter.leftToLoad;
                if (thingsWaitingToLoad == null)
                {
                    continue;
                }

                for (int j = 0; j < thingsWaitingToLoad.Count; j++)
                {
                    TransferableOneWay transferable = thingsWaitingToLoad[j];
                    int pawnsLeftToFind = transferable?.CountToTransfer ?? 0;
                    if (pawnsLeftToFind <= 0 || transferable.things == null)
                    {
                        continue;
                    }

                    for (int k = 0; k < transferable.things.Count && pawnsLeftToFind > 0; k++)
                    {
                        if (transferable.things[k] is Pawn pawn)
                        {
                            if (addedPawns.Add(pawn))
                            {
                                result.Add(pawn);
                            }

                            pawnsLeftToFind--;
                        }
                    }
                }
            }

            return result;
        }

        private static bool ContainsPotentialCaravanOwner(List<Pawn> pawns)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                if (CaravanUtility.IsOwner(pawns[i], Faction.OfPlayer))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanTradeAfterLoading(List<Pawn> pawns, Settlement settlement)
        {
            if (!settlement.Visitable
                || settlement.Faction == null
                || settlement.Faction == Faction.OfPlayer
                || settlement.HasMap
                || settlement.Faction.def.permanentEnemy
                || settlement.Faction.HostileTo(Faction.OfPlayer)
                || !settlement.CanTradeNow)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.RaceProps.Humanlike && pawn.CanTradeWith(settlement.Faction, settlement.TraderKind).Accepted)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddAttackSettlementOptionForPawnsWaitingToLoad(
            ShuttleContext context,
            List<Pawn> pawnsExpectedAfterLoading,
            Settlement settlement,
            Action<PlanetTile, TransportersArrivalAction> chooseArrivalAction,
            List<FloatMenuOption> options)
        {
            TransportShip transportShip = context.Shuttle?.ShuttleComp?.shipParent;
            if (settlement.HasMap
                || transportShip == null
                || !settlement.Spawned
                || settlement.Faction == null
                || !settlement.Attackable
                || !ContainsNonDownedColonist(pawnsExpectedAfterLoading))
            {
                return;
            }

            bool useChooseWhereToLand = ChooseWhereToLandCompatibility.TryCreateSettlementAttackArrivalAction(
                settlement,
                out TransportersArrivalAction attackArrivalAction);
            string label = useChooseWhereToLand
                ? "CWTL_AttackSettlement".Translate(settlement.Label)
                : "AttackShuttle".Translate(settlement.Label);
            if (!useChooseWhereToLand)
            {
                attackArrivalAction = new TransportersArrivalAction_TransportShip(settlement, transportShip);
            }

            if (settlement.EnterCooldownBlocksEntering())
            {
                string cooldownPeriod = settlement.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true, false);
                string failureReason = "EnterCooldownBlocksEntering".Translate();
                string failureMessage = "MessageEnterCooldownBlocksEntering".Translate(cooldownPeriod);
                options.Add(new FloatMenuOption(
                    label + " (" + failureReason + ")",
                    () => Messages.Message(failureMessage, new GlobalTargetInfo(settlement), MessageTypeDefOf.RejectInput, false)));
                return;
            }

            TaggedString confirmationMessage = settlement.Faction.HostileTo(Faction.OfPlayer)
                ? "ConfirmLandOnHostileFactionBase".Translate(settlement.Faction)
                : "ConfirmLandOnNeutralFactionBase".Translate(settlement.Faction);
            options.Add(new FloatMenuOption(
                label,
                () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    confirmationMessage,
                    () => chooseArrivalAction(
                        settlement.Tile,
                        attackArrivalAction),
                    false,
                    null,
                    WindowLayer.Dialog))));
        }

        private static bool ContainsNonDownedColonist(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.IsColonist && !pawn.Downed)
                {
                    return true;
                }
            }

            return false;
        }

        private static void WrapQueuedTargetOption(FloatMenuOption option)
        {
            Action originalAction = option.action;
            if (originalAction == null)
            {
                return;
            }

            option.action = delegate
            {
                StopWorldTargeting();
                originalAction();
            };
        }

        private static string GetVanillaFuelCostText(ShuttleContext context, PlanetTile targetTile, int distance)
        {
            float fuelLevel = context.FuelLevel;
            float fuelRequired = context.Launchable.FuelNeededToLaunchAtDist(distance, targetTile.Layer);
            string text = string.Format(
                "{0}: {1}",
                "Cost".Translate().CapitalizeFirst(),
                "FuelAmount".Translate(fuelRequired, ThingDefOf.Chemfuel));
            if (fuelRequired > fuelLevel)
            {
                text = (text + string.Format(" ({0})", "TransportPodNotEnoughFuel".Translate())).Colorize(ColorLibrary.RedReadable);
            }

            return text;
        }

        private static bool CanSelectQueuedWorldTarget(ShuttleContext context, GlobalTargetInfo target, out string disabledReason)
        {
            disabledReason = null;
            if (!target.IsValid || !target.Tile.Valid)
            {
                disabledReason = "MessageTransportPodsDestinationIsInvalid".Translate();
                return false;
            }

            if (target.HasWorldObject && !target.WorldObject.def.validLaunchTarget)
            {
                disabledReason = "MessageWorldObjectIsInvalid".Translate(target.WorldObject.Named("OBJECT"));
                return false;
            }

            if (target.Tile.Layer != context.OriginTile.Layer && !context.OriginTile.Layer.HasConnectionPathTo(target.Tile.Layer))
            {
                disabledReason = "TransportPodDestinationNoPath".Translate(target.Tile.Layer.Def.Named("LAYER"));
                return false;
            }

            if (ModsConfig.OdysseyActive && target.HasWorldObject && target.WorldObject.RequiresSignalJammerToReach)
            {
                disabledReason = "TransportPodDestinationRequiresSignalJammer".Translate();
                return false;
            }

            LaunchRangeInfo rangeInfo = LaunchRangeInfo.ForTarget(context, target);
            if (rangeInfo.CanSelectDestination)
            {
                return true;
            }

            disabledReason = rangeInfo.FailureReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : rangeInfo.FailureReason;
            return false;
        }

        private static bool CanStartQueuedLandingCellTargeter(
            ShuttleContext context,
            List<CompTransporter> transporters,
            MapParent destination,
            out string failReason)
        {
            failReason = null;
            if (context == null
                || context.Launchable == null
                || transporters == null
                || destination == null
                || !destination.Spawned
                || !destination.HasMap
                || destination.Map == null)
            {
                failReason = "BSL_DestinationInvalid".Translate();
                return false;
            }

            LaunchRangeInfo rangeInfo = LaunchRangeInfo.ForTile(context, destination.Tile);
            if (rangeInfo.State != LaunchRangeState.BeyondMaximumRange && rangeInfo.State != LaunchRangeState.InvalidDestination)
            {
                return true;
            }

            failReason = rangeInfo.FailureReason.NullOrEmpty() ? "BSL_DestinationInvalid".Translate() : rangeInfo.FailureReason;
            return false;
        }
    }
}
