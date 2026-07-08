using System;
using System.Collections.Generic;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class DeferredUiActionGameComponent : GameComponent
    {
        private static readonly Queue<Action> pendingActions = new Queue<Action>();

        public DeferredUiActionGameComponent(Game game)
        {
            pendingActions.Clear();
        }

        public static void RunOnNextUpdate(Action action)
        {
            if (action == null)
            {
                return;
            }

            pendingActions.Enqueue(action);
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
            while (pendingActions.Count > 0)
            {
                Action action = pendingActions.Dequeue();
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    Log.Error("[Better Shuttle Launch] 지연된 UI 작업 실행 중 오류가 발생했습니다: " + exception);
                }
            }
        }
    }
}
