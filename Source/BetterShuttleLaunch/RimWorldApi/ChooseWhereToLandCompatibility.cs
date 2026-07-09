using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterShuttleLaunch.RimWorldApi
{
    public static class ChooseWhereToLandCompatibility
    {
        private const string PackageId = "kearril.choosewheretoland";
        private const string ModTypeName = "ChooseWhereToLand.ChooseWhereToLand_Mod";
        private const string SettlementAttackActionTypeName = "ChooseWhereToLand.TransportersArrivalAction_CWTLAttackSettlement";
        private const string SiteLandingActionTypeName = "ChooseWhereToLand.TransportersArrivalAction_ChooseSpotAndLand";
        private const string SpaceLandingActionTypeName = "ChooseWhereToLand.TransportersArrivalAction_CWTLVisitSpace";

        private static bool creationFailureLogged;

        public static bool TryCreateSettlementAttackArrivalAction(
            Settlement settlement,
            out TransportersArrivalAction arrivalAction)
        {
            arrivalAction = null;
            return IsCustomLandingEnabled()
                   && TryCreateArrivalAction(SettlementAttackActionTypeName, settlement, out arrivalAction);
        }

        public static bool TryCreateSiteOrSpaceLandingOption(
            WorldObject worldObject,
            List<CompTransporter> transporters,
            Action<PlanetTile, TransportersArrivalAction> chooseArrivalAction,
            out FloatMenuOption option)
        {
            option = null;
            if (!IsCustomLandingEnabled()
                || worldObject == null
                || !worldObject.Spawned)
            {
                return false;
            }

            if (worldObject is MapParent mapParent
                && TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(transporters, mapParent))
            {
                return false;
            }

            if (worldObject is Site site)
            {
                return TryCreateSiteLandingOption(site, chooseArrivalAction, out option);
            }

            if (worldObject is SpaceMapParent spaceMapParent)
            {
                return TryCreateSpaceLandingOption(spaceMapParent, chooseArrivalAction, out option);
            }

            return false;
        }

        private static bool TryCreateSiteLandingOption(
            Site site,
            Action<PlanetTile, TransportersArrivalAction> chooseArrivalAction,
            out FloatMenuOption option)
        {
            option = null;
            if (!TryCreateArrivalAction(SiteLandingActionTypeName, site, out TransportersArrivalAction arrivalAction))
            {
                return false;
            }

            string label = "CWTL_ChooseSpotAndLand".Translate();
            if (site.EnterCooldownBlocksEntering())
            {
                string cooldownPeriod = site.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true, false);
                string failureMessage = "MessageEnterCooldownBlocksEntering".Translate(cooldownPeriod);
                option = new FloatMenuOption(
                    label,
                    () => Messages.Message(failureMessage, new GlobalTargetInfo(site), MessageTypeDefOf.RejectInput, false));
                return true;
            }

            option = new FloatMenuOption(
                label,
                () => chooseArrivalAction(site.Tile, arrivalAction));
            return true;
        }

        private static bool TryCreateSpaceLandingOption(
            SpaceMapParent spaceMapParent,
            Action<PlanetTile, TransportersArrivalAction> chooseArrivalAction,
            out FloatMenuOption option)
        {
            option = null;
            if (!TryCreateArrivalAction(
                    SpaceLandingActionTypeName,
                    spaceMapParent,
                    out TransportersArrivalAction arrivalAction))
            {
                return false;
            }

            option = new FloatMenuOption(
                "CWTL_LaunchTo".Translate(spaceMapParent.Named("LOCATION")),
                () =>
                {
                    Action chooseDestination = () => chooseArrivalAction(spaceMapParent.Tile, arrivalAction);
                    if (!ModsConfig.OdysseyActive || spaceMapParent.Tile.LayerDef != PlanetLayerDefOf.Orbit)
                    {
                        chooseDestination();
                        return;
                    }

                    TaggedString confirmationMessage = "OrbitalWarning".Translate();
                    confirmationMessage += "\n\n" + "LaunchToConfirmation".Translate();
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        confirmationMessage,
                        chooseDestination,
                        true,
                        null,
                        WindowLayer.Dialog));
                });
            return true;
        }

        private static bool TryCreateArrivalAction(
            string actionTypeName,
            WorldObject destination,
            out TransportersArrivalAction arrivalAction)
        {
            arrivalAction = null;
            Type actionType = AccessTools.TypeByName(actionTypeName);
            if (actionType == null || !typeof(TransportersArrivalAction).IsAssignableFrom(actionType))
            {
                return false;
            }

            try
            {
                arrivalAction = Activator.CreateInstance(actionType, destination) as TransportersArrivalAction;
                return arrivalAction != null;
            }
            catch (Exception exception)
            {
                if (!creationFailureLogged)
                {
                    creationFailureLogged = true;
                    Log.Warning("[Better Shuttle Launch] Choose Where To Land 호환 도착 행동을 생성하지 못해 바닐라 동작을 사용합니다: " + exception);
                }

                return false;
            }
        }

        private static bool IsCustomLandingEnabled()
        {
            if (!ModsConfig.IsActive(PackageId))
            {
                return false;
            }

            Type modType = AccessTools.TypeByName(ModTypeName);
            FieldInfo settingsField = modType == null ? null : AccessTools.Field(modType, "settings");
            object settings = settingsField?.GetValue(null);
            FieldInfo enabledField = settings == null ? null : AccessTools.Field(settings.GetType(), "useCustomLandingSpot");
            return enabledField?.GetValue(settings) is bool enabled && enabled;
        }
    }
}
