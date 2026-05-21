using UnityEngine;
using UnityEngine.UIElements;

namespace Redpoint.DungeonEscape.Unity.UI
{
    internal static class ToolkitTextStyles
    {
        private static Font runtimeFont;

        public static void Apply(Label label, Color color, int fontSize)
        {
            if (label == null)
            {
                return;
            }

            label.style.color = color;
            label.style.fontSize = fontSize;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            var font = GetRuntimeFont();
            if (font != null)
            {
                label.style.unityFont = font;
            }
        }

        private static Font GetRuntimeFont()
        {
            if (runtimeFont != null)
            {
                return runtimeFont;
            }

            runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return runtimeFont;
        }
    }
}
