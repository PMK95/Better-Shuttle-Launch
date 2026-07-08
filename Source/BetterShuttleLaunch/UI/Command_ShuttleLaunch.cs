using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterShuttleLaunch.UI
{
    public class Command_ShuttleLaunch : Command_Action
    {
        private readonly Func<List<FloatMenuOption>> createOptions;
        private static Texture2D floatMenuIndicatorTexture;

        public Command_ShuttleLaunch(Func<List<FloatMenuOption>> createOptions)
        {
            this.createOptions = createOptions;
        }

        protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
        {
            GizmoResult result = base.GizmoOnGUIInt(butRect, parms);
            DrawFloatMenuIndicator(butRect);
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
                Find.WindowStack.Add(new FloatMenu(CreateOptions()));
                ev.Use();
                return;
            }

            base.ProcessInput(ev);
        }

        private static void DrawFloatMenuIndicator(Rect butRect)
        {
            Rect indicatorRect = new Rect(butRect.xMax - 16f, butRect.y + 3f, 13f, 13f);
            GUI.DrawTexture(indicatorRect, FloatMenuIndicatorTexture);
        }

        private static Texture2D FloatMenuIndicatorTexture
        {
            get
            {
                if (floatMenuIndicatorTexture == null)
                {
                    floatMenuIndicatorTexture = CreateFloatMenuIndicatorTexture();
                }

                return floatMenuIndicatorTexture;
            }
        }

        private static Texture2D CreateFloatMenuIndicatorTexture()
        {
            const int size = 16;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = new Color(0.62f, 0.62f, 0.62f, 0.95f);
            Color edge = new Color(0.22f, 0.22f, 0.22f, 0.95f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < size - 1 - y)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    bool isEdge = x == size - 1 || y == size - 1 || x == size - 1 - y;
                    texture.SetPixel(x, y, isEdge ? edge : fill);
                }
            }

            texture.Apply(false, true);
            return texture;
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
