using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public static class UiControls
    {
        public static void BeginSelectableRow(int rowIndex, int selectedRowIndex, UiTheme theme)
        {
            var style = theme == null
                ? GUI.skin.box
                : rowIndex == selectedRowIndex ? theme.SelectedRowStyle : theme.RowStyle;
            GUILayout.BeginVertical(style);
        }

        public static void EndSelectableRow()
        {
            GUILayout.EndVertical();
        }

        public static bool TabButton(string label, bool selected, UiTheme theme, float height)
        {
            var style = theme == null
                ? GUI.skin.button
                : selected ? theme.SelectedTabStyle : theme.TabStyle;
            return GUILayout.Button(label, style, GUILayout.Height(height));
        }

        public static bool Button(string label, bool selected, UiTheme theme, params GUILayoutOption[] options)
        {
            var style = theme == null
                ? GUI.skin.button
                : selected ? theme.SelectedTabStyle : theme.ButtonStyle;
            return GUILayout.Button(label, style, options);
        }

        public static bool ChoiceButton(Rect rect, string label, bool selected, UiTheme theme)
        {
            if (theme == null)
            {
                return GUI.Button(rect, label);
            }

            var style = selected ? theme.SelectedRowStyle : theme.RowStyle;
            if (GUI.Button(rect, GUIContent.none, style))
            {
                return true;
            }

            var textStyle = new GUIStyle(theme.LabelStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal
            };
            textStyle.normal.textColor = selected ? theme.HighlightColor : theme.TextColor;

            var inset = theme.BorderThickness;
            GUI.Label(
                new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f),
                label,
                textStyle);
            return false;
        }

        public static string TextFieldRow(string label, string value, string fallback, UiTheme theme)
        {
            var style = theme == null ? GUI.skin.textField : theme.ButtonStyle;
            var labelStyle = theme == null ? GUI.skin.label : theme.LabelStyle;
            GUILayout.Label(label, labelStyle);
            return GUILayout.TextField(string.IsNullOrEmpty(value) ? fallback : value, style);
        }

        public static float SliderRow(
            string label,
            float value,
            float leftValue,
            float rightValue,
            UiTheme theme)
        {
            var labelStyle = theme == null ? GUI.skin.label : theme.LabelStyle;
            GUILayout.Label(label, labelStyle);
            return Slider(value, leftValue, rightValue, theme);
        }

        public static bool CheckboxRow(bool value, string label, UiTheme theme, float scale)
        {
            return Checkbox(value, label, theme, scale);
        }

        public static bool Checkbox(bool value, string label, UiTheme theme, float scale)
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

        public static float Slider(float value, float leftValue, float rightValue, UiTheme theme)
        {
            if (theme == null)
            {
                return GUILayout.HorizontalSlider(value, leftValue, rightValue);
            }

            return GUILayout.HorizontalSlider(value, leftValue, rightValue, theme.SliderStyle, theme.SliderThumbStyle);
        }

        public static void SpriteIcon(Sprite sprite, float size, UiTheme theme)
        {
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (theme != null)
            {
                GUI.Box(rect, GUIContent.none, theme.RowStyle);
            }

            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var drawRect = FitRect(sprite.rect.width, sprite.rect.height, rect);
            var texCoords = new Rect(
                sprite.rect.x / sprite.texture.width,
                sprite.rect.y / sprite.texture.height,
                sprite.rect.width / sprite.texture.width,
                sprite.rect.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, texCoords);
        }

        private static Rect FitRect(float sourceWidth, float sourceHeight, Rect target)
        {
            if (sourceWidth <= 0f || sourceHeight <= 0f)
            {
                return target;
            }

            var scale = Mathf.Min(target.width / sourceWidth, target.height / sourceHeight);
            var width = sourceWidth * scale;
            var height = sourceHeight * scale;
            return new Rect(
                target.x + (target.width - width) / 2f,
                target.y + (target.height - height) / 2f,
                width,
                height);
        }
    }
}
