using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Dialog_LoadTransportersThenRunAction : Dialog_LoadTransporters
    {
        private readonly Action afterAccepted;
        private bool accepted;

        public Dialog_LoadTransportersThenRunAction(Map map, List<CompTransporter> transporters, Action afterAccepted)
            : base(map, transporters)
        {
            this.afterAccepted = afterAccepted;
        }

        public void MarkAccepted()
        {
            accepted = true;
        }

        public override void PostClose()
        {
            base.PostClose();
            if (accepted)
            {
                DeferredUiActionGameComponent.RunOnNextUpdate(afterAccepted);
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
}
