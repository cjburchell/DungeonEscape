using System;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class CombatWindow
    {
        private void EnsureStyles()
        {
            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
            }

            var scale = GetPixelScale();
            var settings = SettingsCache.Current;
            var themeSignature = UiTheme.GetSignature(settings);
            if (uiTheme != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = UiTheme.Create(settings, scale);
            panelStyle = uiTheme.PanelStyle;
            labelStyle = new GUIStyle(uiTheme.LabelStyle)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            titleStyle = new GUIStyle(uiTheme.TitleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(22f * scale)
            };
            buttonStyle = uiTheme.ButtonStyle;
        }

        private GUIStyle GetCombatRowStyle(bool selected)
        {
            if (uiTheme == null)
            {
                return selected ? GUI.skin.box : GUIStyle.none;
            }

            return selected ? uiTheme.SelectedRowStyle : GUIStyle.none;
        }

        private GUIStyle GetCombatRowLabelStyle(bool selected)
        {
            var style = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            if (selected && uiTheme != null)
            {
                style.normal.textColor = uiTheme.HighlightColor;
                style.hover.textColor = uiTheme.HighlightColor;
                style.active.textColor = uiTheme.HighlightColor;
                style.focused.textColor = uiTheme.HighlightColor;
            }

            return style;
        }

        private GUIStyle GetTargetButtonStyle(IFighter target, bool selected)
        {
            var style = new GUIStyle(GetCombatRowLabelStyle(selected));
            var color = GetHealthColor(target == null ? 0 : target.Health, target == null ? 0 : target.MaxHealth);
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            return style;
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }

        private static void DrawTextureAtNativeCombatSize(Texture2D texture, Rect rect, float scale)
        {
            var width = texture.width * scale;
            var height = texture.height * scale;
            var maxWidth = rect.width;
            var maxHeight = rect.height;
            var shrink = Mathf.Min(1f, Mathf.Min(maxWidth / width, maxHeight / height));
            width *= shrink;
            height *= shrink;
            var drawRect = new Rect(
                rect.x + (rect.width - width) / 2f,
                rect.y + rect.height - height,
                width,
                height);
            GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill, true);
        }

        private static void DrawSprite(Sprite sprite, Rect rect)
        {
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

        private void DrawHealthBar(int currentHealth, int maxHealth, Rect rect)
        {
            GUI.Box(rect, GUIContent.none, buttonStyle);
            var previousColor = GUI.color;
            GUI.color = GetHealthColor(currentHealth, maxHealth);
            var inset = Mathf.Max(1f, uiTheme.BorderThickness);
            var progress = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)currentHealth / maxHealth);
            GUI.DrawTexture(
                new Rect(
                    rect.x + inset,
                    rect.y + inset,
                    Mathf.Max(0f, rect.width - inset * 2f) * progress,
                    Mathf.Max(0f, rect.height - inset * 2f)),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static Color GetHealthColor(int currentHealth, int maxHealth)
        {
            if (maxHealth <= 0 || currentHealth <= 0)
            {
                return Color.red;
            }

            var progress = Mathf.Clamp01((float)currentHealth / maxHealth);
            if (progress < 0.1f)
            {
                return new Color(1f, 0.55f, 0f, 1f);
            }

            return progress < 0.5f ? Color.yellow : Color.green;
        }

    }
}
