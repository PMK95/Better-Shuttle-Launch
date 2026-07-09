using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Command_ShuttleLaunch : Command_Action
    {
        private readonly Func<List<FloatMenuOption>> createOptions;

        public Command_ShuttleLaunch(Func<List<FloatMenuOption>> createOptions)
        {
            this.createOptions = createOptions;
            action = ShowFloatMenu;
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => CreateOptions();

        protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
        {
            GizmoResult result = base.GizmoOnGUIInt(butRect, parms);
            Designator_Dropdown.DrawExtraOptionsIcon(butRect.position, butRect.width);
            return result;
        }

        public override void ProcessInput(Event ev)
        {
            if (disabled)
            {
                base.ProcessInput(ev);
                return;
            }

            if (ev != null && ev.button == 1)
            {
                ShowFloatMenu();
                ev.Use();
                return;
            }

            base.ProcessInput(ev);
        }

        private void ShowFloatMenu()
        {
            if (disabled)
            {
                return;
            }

            Find.WindowStack.Add(new FloatMenu(CreateOptions()));
        }

        private List<FloatMenuOption> CreateOptions()
        {
            List<FloatMenuOption> options = createOptions?.Invoke() ?? new List<FloatMenuOption>();
            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("BSL_NoAvailableLaunchOptions".Translate(), null));
            }

            return options;
        }
    }
}
