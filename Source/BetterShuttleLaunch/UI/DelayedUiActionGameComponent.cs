using System;
using System.Collections.Generic;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class DelayedUiActionGameComponent : GameComponent
    {
        private static readonly List<DelayedUiAction> pendingActions = new List<DelayedUiAction>();

        public DelayedUiActionGameComponent(Game game)
        {
            pendingActions.Clear();
        }

        public static void RunAfterWindowStackSettles(Action action)
        {
            if (action == null)
            {
                return;
            }

            pendingActions.Add(new DelayedUiAction(action, 2));
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
            for (int i = pendingActions.Count - 1; i >= 0; i--)
            {
                DelayedUiAction pendingAction = pendingActions[i];
                pendingAction.UpdatesLeft--;
                if (pendingAction.UpdatesLeft > 0)
                {
                    pendingActions[i] = pendingAction;
                    continue;
                }

                pendingActions.RemoveAt(i);
                try
                {
                    pendingAction.Action();
                }
                catch (Exception exception)
                {
                    Log.Error("[Better Shuttle Launch] 지연된 UI 작업 실행 중 오류가 발생했습니다: " + exception);
                }
            }
        }

        private struct DelayedUiAction
        {
            public readonly Action Action;
            public int UpdatesLeft;

            public DelayedUiAction(Action action, int updatesLeft)
            {
                Action = action;
                UpdatesLeft = updatesLeft;
            }
        }
    }
}
