using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Command_PassengerShuttleLaunchMenu : Command
    {
        private readonly Action leftClickAction;
        private readonly Func<List<FloatMenuOption>> createRightClickOptions;

        public Command_PassengerShuttleLaunchMenu(Action leftClickAction, Func<List<FloatMenuOption>> createRightClickOptions)
        {
            this.leftClickAction = leftClickAction;
            this.createRightClickOptions = createRightClickOptions;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (ev != null && ev.button == 1)
            {
                List<FloatMenuOption> options = createRightClickOptions?.Invoke() ?? new List<FloatMenuOption>();
                if (options.Count == 0)
                {
                    options.Add(new FloatMenuOption("BSL_NoAvailableLaunchOptions".Translate(), null));
                }

                Find.WindowStack.Add(new FloatMenu(options));
                return;
            }

            leftClickAction?.Invoke();
        }
    }
}
