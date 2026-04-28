using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class DungeonEscapeUiControls
    {
        public static bool Checkbox(bool value, string label, DungeonEscapeUiTheme theme, float scale)
        {
            if (theme == null)
            {
                return GUILayout.Toggle(value, label);
            }

            var labelContent = new GUIContent(label);
            var labelWidth = Mathf.Max(100f * scale, Screen.width * 0.25f);
            var height = Mathf.Max(30f * scale, theme.LabelStyle.CalcHeight(labelContent, labelWidth) + 8f * scale);
            var rect = GUILayoutUtility.GetRect(GUIContent.none, theme.CheckboxStyle, GUILayout.Height(height));
            if (GUI.Button(rect, GUIContent.none, theme.CheckboxStyle))
            {
                value = !value;
            }

            var boxSize = 18f * scale;
            var boxRect = new Rect(rect.x + 10f * scale, rect.y + (rect.height - boxSize) / 2f, boxSize, boxSize);
            GUI.Box(boxRect, GUIContent.none, theme.CheckboxBoxStyle);
            if (value)
            {
                var inset = Mathf.Max(4f * scale, 3f);
                GUI.Box(
                    new Rect(boxRect.x + inset, boxRect.y + inset, boxRect.width - inset * 2f, boxRect.height - inset * 2f),
                    GUIContent.none,
                    theme.CheckboxCheckedBoxStyle);
            }

            var textWidth = rect.width - boxSize - 28f * scale;
            var labelHeight = theme.LabelStyle.CalcHeight(labelContent, textWidth);
            var labelRect = new Rect(
                boxRect.xMax + 8f * scale,
                rect.y + (rect.height - labelHeight) / 2f,
                textWidth,
                labelHeight);
            GUI.Label(labelRect, labelContent, theme.LabelStyle);
            return value;
        }

        public static float Slider(float value, float leftValue, float rightValue, DungeonEscapeUiTheme theme)
        {
            if (theme == null)
            {
                return GUILayout.HorizontalSlider(value, leftValue, rightValue);
            }

            return GUILayout.HorizontalSlider(value, leftValue, rightValue, theme.SliderStyle, theme.SliderThumbStyle);
        }
    }
}
