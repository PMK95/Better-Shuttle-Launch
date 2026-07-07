using System;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Dialog_SelectReturnLaunchMode : Window
    {
        private readonly Action chooseLandingAction;
        private readonly Action lastDepartureAction;

        public Dialog_SelectReturnLaunchMode(Action chooseLandingAction, Action lastDepartureAction)
        {
            this.chooseLandingAction = chooseLandingAction;
            this.lastDepartureAction = lastDepartureAction;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(520f, 220f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 34f), "BSL_SelectReturnMode".Translate());
            Text.Font = GameFont.Small;

            Rect chooseLandingRect = new Rect(0f, 48f, inRect.width, 42f);
            if (Widgets.ButtonText(chooseLandingRect, "BSL_ReturnWithLandingSelection".Translate()))
            {
                Close();
                chooseLandingAction?.Invoke();
            }

            Rect lastDepartureRect = new Rect(0f, 100f, inRect.width, 42f);
            if (Widgets.ButtonText(lastDepartureRect, "BSL_ReturnToLastDepartureCell".Translate()))
            {
                Close();
                lastDepartureAction?.Invoke();
            }
        }
    }
}
