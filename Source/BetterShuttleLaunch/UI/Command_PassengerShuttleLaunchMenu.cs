using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Command_PassengerShuttleLaunchMenu : Command
    {
        private readonly Func<List<FloatMenuOption>> createOptions;

        public Command_PassengerShuttleLaunchMenu(Func<List<FloatMenuOption>> createOptions)
        {
            this.createOptions = createOptions;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (disabled || ev == null || (ev.button != 0 && ev.button != 1))
            {
                return;
            }

            List<FloatMenuOption> options = createOptions?.Invoke() ?? new List<FloatMenuOption>();
            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("BSL_NoAvailableLaunchOptions".Translate(), null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
            ev.Use();
        }
    }
}
