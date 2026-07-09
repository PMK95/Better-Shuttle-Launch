using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Dialog_LoadTransportersThenRunAction : Dialog_LoadTransporters
    {
        private static readonly AccessTools.FieldRef<Dialog_LoadTransporters, List<TransferableOneWay>> TransferablesField =
            AccessTools.FieldRefAccess<Dialog_LoadTransporters, List<TransferableOneWay>>("transferables");

        private readonly Action<IReadOnlyList<Pawn>> afterAccepted;
        private readonly List<Pawn> acceptedPawns = new List<Pawn>();
        private bool accepted;

        public Dialog_LoadTransportersThenRunAction(
            Map map,
            List<CompTransporter> transporters,
            Action<IReadOnlyList<Pawn>> afterAccepted)
            : base(map, transporters)
        {
            this.afterAccepted = afterAccepted;
        }

        public void MarkAccepted()
        {
            CapturePawnsSelectedForLoading();
            accepted = true;
        }

        public void CapturePawnsSelectedForLoading()
        {
            acceptedPawns.Clear();
            List<TransferableOneWay> transferables = TransferablesField(this);
            if (transferables != null)
            {
                acceptedPawns.AddRange(TransferableUtility.GetPawnsFromTransferables(transferables));
            }
        }

        public void MarkCapturedSelectionAccepted()
        {
            accepted = true;
        }

        public override void PostClose()
        {
            base.PostClose();
            if (accepted && afterAccepted != null)
            {
                LongEventHandler.ExecuteWhenFinished(() => afterAccepted(acceptedPawns));
            }
        }
    }

    [HarmonyPatch(typeof(Dialog_LoadTransporters), "TryAccept")]
    public static class DialogLoadTransportersTryAcceptPatch
    {
        public static void Postfix(Dialog_LoadTransporters __instance, bool __result)
        {
            if (__result && __instance is Dialog_LoadTransportersThenRunAction dialog)
            {
                dialog.MarkAccepted();
            }
        }
    }

    [HarmonyPatch(typeof(Dialog_LoadTransporters), "DebugTryLoadInstantly")]
    public static class DialogLoadTransportersDebugTryLoadInstantlyPatch
    {
        public static void Prefix(Dialog_LoadTransporters __instance)
        {
            if (__instance is Dialog_LoadTransportersThenRunAction dialog)
            {
                dialog.CapturePawnsSelectedForLoading();
            }
        }

        public static void Postfix(Dialog_LoadTransporters __instance, bool __result)
        {
            if (__result && __instance is Dialog_LoadTransportersThenRunAction dialog)
            {
                dialog.MarkCapturedSelectionAccepted();
            }
        }
    }
}
