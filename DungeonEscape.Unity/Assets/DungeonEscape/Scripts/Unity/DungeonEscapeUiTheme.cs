using System;
using Redpoint.DungeonEscape;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeUiTheme
    {
        public readonly Color BackgroundColor;
        public readonly Color HoverColor;
        public readonly Color ActiveColor;
        public readonly Color BorderColor;
        public readonly Color TextColor;
        public readonly Color HighlightColor;
        public readonly int BorderThickness;
        public readonly string Signature;

        public readonly GUIStyle PanelStyle;
        public readonly GUIStyle RowStyle;
        public readonly GUIStyle SelectedRowStyle;
        public readonly GUIStyle TitleStyle;
        public readonly GUIStyle LabelStyle;
        public readonly GUIStyle SmallStyle;
        public readonly GUIStyle ButtonStyle;
        public readonly GUIStyle CheckboxStyle;
        public readonly GUIStyle CheckboxBoxStyle;
        public readonly GUIStyle CheckboxCheckedBoxStyle;
        public readonly GUIStyle SliderStyle;
        public readonly GUIStyle SliderThumbStyle;
        public readonly GUIStyle VerticalScrollbarStyle;
        public readonly GUIStyle VerticalScrollbarThumbStyle;
        public readonly GUIStyle TabStyle;
        public readonly GUIStyle SelectedTabStyle;

        private readonly Texture2D panelTexture;
        private readonly Texture2D rowTexture;
        private readonly Texture2D buttonTexture;
        private readonly Texture2D selectedRowTexture;
        private readonly Texture2D hoverTexture;
        private readonly Texture2D activeTexture;
        private readonly Texture2D selectedHoverTexture;
        private readonly Texture2D selectedActiveTexture;
        private readonly Texture2D checkboxTexture;
        private readonly Texture2D checkboxHoverTexture;
        private readonly Texture2D checkboxActiveTexture;
        private readonly Texture2D checkboxBoxTexture;
        private readonly Texture2D checkboxCheckedBoxTexture;
        private readonly Texture2D sliderTrackTexture;
        private readonly Texture2D sliderThumbTexture;
        private readonly Texture2D sliderThumbHoverTexture;
        private readonly Texture2D sliderThumbActiveTexture;
        private readonly Texture2D scrollbarTrackTexture;
        private readonly Texture2D scrollbarThumbTexture;
        private readonly Texture2D scrollbarThumbHoverTexture;
        private readonly Texture2D scrollbarThumbActiveTexture;

        private DungeonEscapeUiTheme(Settings settings, float scale)
        {
            BackgroundColor = ParseColor(settings == null ? null : settings.UiBackgroundColor, Color.black);
            BackgroundColor.a = settings == null ? 1f : Mathf.Clamp01(settings.UiBackgroundAlpha);
            HoverColor = ParseColor(settings == null ? null : settings.UiHoverColor, Color.gray);
            ActiveColor = ParseColor(settings == null ? null : settings.UiActiveColor, Color.lightGray);
            BorderColor = ParseColor(settings == null ? null : settings.UiBorderColor, Color.white);
            TextColor = ParseColor(settings == null ? null : settings.UiTextColor, Color.white);
            HighlightColor = ParseColor(settings == null ? null : settings.UiHighlightColor, Color.yellow);
            BorderThickness = GetBorderThickness(settings);
            Signature = GetSignature(settings);

            panelTexture = CreateBorderTexture(BackgroundColor, BorderColor, BorderThickness);
            rowTexture = CreateBorderTexture(BackgroundColor, BackgroundColor, BorderThickness);
            buttonTexture = CreateBorderTexture(BackgroundColor, BorderColor, BorderThickness);
            selectedRowTexture = CreateBorderTexture(BackgroundColor, HighlightColor, BorderThickness);
            hoverTexture = CreateBorderTexture(HoverColor, BorderColor, BorderThickness);
            activeTexture = CreateBorderTexture(ActiveColor, BorderColor, BorderThickness);
            selectedHoverTexture = CreateBorderTexture(HoverColor, HighlightColor, BorderThickness);
            selectedActiveTexture = CreateBorderTexture(ActiveColor, HighlightColor, BorderThickness);
            checkboxTexture = CreateBorderTexture(BackgroundColor, BackgroundColor, BorderThickness);
            checkboxHoverTexture = CreateBorderTexture(HoverColor, HoverColor, BorderThickness);
            checkboxActiveTexture = CreateBorderTexture(ActiveColor, ActiveColor, BorderThickness);
            checkboxBoxTexture = CreateBorderTexture(BackgroundColor, BorderColor, BorderThickness);
            checkboxCheckedBoxTexture = CreateBorderTexture(HighlightColor, HighlightColor, BorderThickness);
            sliderTrackTexture = CreateBorderTexture(BackgroundColor, BorderColor, BorderThickness);
            sliderThumbTexture = CreateBorderTexture(BorderColor, BorderColor, BorderThickness);
            sliderThumbHoverTexture = CreateBorderTexture(HoverColor, BorderColor, BorderThickness);
            sliderThumbActiveTexture = CreateBorderTexture(ActiveColor, BorderColor, BorderThickness);
            scrollbarTrackTexture = CreateBorderTexture(BackgroundColor, BorderColor, BorderThickness);
            scrollbarThumbTexture = CreateBorderTexture(BorderColor, BorderColor, BorderThickness);
            scrollbarThumbHoverTexture = CreateBorderTexture(HoverColor, BorderColor, BorderThickness);
            scrollbarThumbActiveTexture = CreateBorderTexture(ActiveColor, BorderColor, BorderThickness);
            var border = new RectOffset(BorderThickness, BorderThickness, BorderThickness, BorderThickness);

            PanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTexture, textColor = TextColor },
                border = border,
                padding = new RectOffset(12, 12, 10, 10)
            };
            RowStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = rowTexture, textColor = TextColor },
                border = border,
                padding = GUI.skin.box.padding,
                margin = GUI.skin.box.margin
            };
            SelectedRowStyle = new GUIStyle(RowStyle)
            {
                normal = { background = selectedRowTexture, textColor = HighlightColor }
            };
            TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(20f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = TextColor },
                wordWrap = true
            };
            LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                normal = { textColor = TextColor },
                wordWrap = true
            };
            SmallStyle = new GUIStyle(LabelStyle)
            {
                fontSize = Mathf.RoundToInt(14f * scale)
            };
            ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                fontStyle = FontStyle.Normal,
                normal = { background = buttonTexture, textColor = TextColor },
                hover = { background = hoverTexture, textColor = TextColor },
                active = { background = activeTexture, textColor = TextColor },
                focused = { background = selectedRowTexture, textColor = HighlightColor },
                onNormal = { background = selectedRowTexture, textColor = HighlightColor },
                onHover = { background = selectedHoverTexture, textColor = HighlightColor },
                onActive = { background = selectedActiveTexture, textColor = HighlightColor },
                onFocused = { background = selectedRowTexture, textColor = HighlightColor },
                border = border
            };
            CheckboxStyle = new GUIStyle(ButtonStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { background = checkboxTexture, textColor = TextColor },
                hover = { background = checkboxHoverTexture, textColor = TextColor },
                active = { background = checkboxActiveTexture, textColor = TextColor },
                focused = { background = checkboxTexture, textColor = TextColor },
                onNormal = { background = checkboxTexture, textColor = TextColor },
                onHover = { background = checkboxHoverTexture, textColor = TextColor },
                onActive = { background = checkboxActiveTexture, textColor = TextColor },
                onFocused = { background = checkboxTexture, textColor = TextColor },
                padding = new RectOffset(10, 10, ButtonStyle.padding.top, ButtonStyle.padding.bottom)
            };
            CheckboxBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = checkboxBoxTexture },
                border = border,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            CheckboxCheckedBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = checkboxCheckedBoxTexture },
                border = border,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            var sliderHeight = Mathf.RoundToInt(22f * scale);
            SliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                normal = { background = sliderTrackTexture },
                hover = { background = sliderTrackTexture },
                active = { background = sliderTrackTexture },
                focused = { background = sliderTrackTexture },
                border = border,
                fixedHeight = sliderHeight,
                margin = new RectOffset(4, 4, 4, 4)
            };
            SliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                normal = { background = sliderThumbTexture },
                hover = { background = sliderThumbHoverTexture },
                active = { background = sliderThumbActiveTexture },
                focused = { background = sliderThumbHoverTexture },
                border = border,
                fixedWidth = sliderHeight,
                fixedHeight = sliderHeight
            };
            VerticalScrollbarStyle = new GUIStyle(GUI.skin.verticalScrollbar)
            {
                normal = { background = scrollbarTrackTexture },
                hover = { background = scrollbarTrackTexture },
                active = { background = scrollbarTrackTexture },
                focused = { background = scrollbarTrackTexture },
                border = border,
                fixedWidth = Mathf.RoundToInt(18f * scale)
            };
            VerticalScrollbarThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb)
            {
                normal = { background = scrollbarThumbTexture },
                hover = { background = scrollbarThumbHoverTexture },
                active = { background = scrollbarThumbActiveTexture },
                focused = { background = scrollbarThumbHoverTexture },
                border = border,
                fixedWidth = Mathf.RoundToInt(18f * scale)
            };
            TabStyle = new GUIStyle(ButtonStyle);
            SelectedTabStyle = new GUIStyle(ButtonStyle)
            {
                fontStyle = FontStyle.Normal,
                normal = { background = selectedRowTexture, textColor = HighlightColor },
                hover = { background = selectedHoverTexture, textColor = HighlightColor },
                active = { background = selectedActiveTexture, textColor = HighlightColor },
                focused = { background = selectedRowTexture, textColor = HighlightColor },
                onNormal = { background = selectedRowTexture, textColor = HighlightColor },
                onHover = { background = selectedHoverTexture, textColor = HighlightColor },
                onActive = { background = selectedActiveTexture, textColor = HighlightColor },
                onFocused = { background = selectedRowTexture, textColor = HighlightColor }
            };
        }

        public static DungeonEscapeUiTheme Create(Settings settings, float scale)
        {
            return new DungeonEscapeUiTheme(settings, scale);
        }

        public static string GetThemeValue(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        public static int GetBorderThickness(Settings settings)
        {
            return settings == null || settings.UiBorderThickness < 2 ? 2 : settings.UiBorderThickness;
        }

        public static string GetSignature(Settings settings)
        {
            if (settings == null)
            {
                return "";
            }

            return string.Join(
                "|",
                GetThemeValue(settings.UiBackgroundColor, "#000000"),
                settings.UiBackgroundAlpha.ToString("0.000"),
                GetThemeValue(settings.UiHoverColor, "#808080"),
                GetThemeValue(settings.UiActiveColor, "#D3D3D3"),
                GetThemeValue(settings.UiBorderColor, "#FFFFFF"),
                GetBorderThickness(settings).ToString(),
                GetThemeValue(settings.UiTextColor, "#FFFFFF"),
                GetThemeValue(settings.UiHighlightColor, "#FFFF00"));
        }

        private static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            Color color;
            return ColorUtility.TryParseHtmlString(value, out color) ? color : fallback;
        }

        private static Texture2D CreateBorderTexture(Color background, Color border, int thickness)
        {
            var size = Mathf.Max(thickness * 2 + 4, 8);
            var texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isBorder = x < thickness || y < thickness || x >= size - thickness || y >= size - thickness;
                    texture.SetPixel(x, y, isBorder ? border : background);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
