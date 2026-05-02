using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapePartyStatusWindow : MonoBehaviour
    {
        private DungeonEscapeGameState gameState;
        private PlayerGridController player;
        private DungeonEscapeUiSettings uiSettings;
        private DungeonEscapeUiTheme uiTheme;
        private GUIStyle statusStyle;
        private GUIStyle deadStyle;
        private GUIStyle lowHealthStyle;
        private float lastPixelScale;
        private string lastThemeSignature;

        private void OnGUI()
        {
            if (DungeonEscapeTitleMenu.IsOpen ||
                DungeonEscapeGameMenu.IsOpen ||
                DungeonEscapeStoreWindow.IsOpen ||
                DungeonEscapeMessageBox.IsAnyVisible)
            {
                return;
            }

            EnsureReferences();
            if (player != null && player.IsMovementActive)
            {
                return;
            }

            var party = gameState == null ? null : gameState.Party;
            if (party == null)
            {
                return;
            }

            var members = party.ActiveMembers.ToList();
            if (members.Count == 0)
            {
                return;
            }

            EnsureStyles();
            DrawWindow(members);
        }

        private void DrawWindow(IList<Hero> members)
        {
            var scale = GetPixelScale();
            var portraitWidth = 32f * scale;
            var portraitHeight = 48f * scale;
            var statusWidth = 110f * scale;
            var memberWidth = portraitWidth + statusWidth + 15f * scale;
            var windowWidth = Mathf.Max(memberWidth, members.Count * memberWidth + 10f * scale);
            var windowHeight = 76f * scale;
            var windowRect = new Rect(10f * scale, 10f * scale, windowWidth, windowHeight);

            GUI.Box(windowRect, GUIContent.none, uiTheme.PanelStyle);

            GUILayout.BeginArea(new Rect(
                windowRect.x + 4f * scale,
                windowRect.y + 4f * scale,
                windowRect.width - 8f * scale,
                windowRect.height - 8f * scale));

            GUILayout.BeginHorizontal();
            for (var i = 0; i < members.Count; i++)
            {
                DrawMember(members[i], portraitWidth, portraitHeight, statusWidth, scale);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawMember(Hero member, float portraitWidth, float portraitHeight, float statusWidth, float scale)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(portraitWidth + statusWidth + 15f * scale));

            Sprite sprite;
            var spriteRect = GUILayoutUtility.GetRect(
                portraitWidth,
                portraitHeight,
                GUILayout.Width(portraitWidth),
                GUILayout.Height(portraitHeight));
            if (DungeonEscapeUiAssetResolver.TryGetHeroSprite(member, out sprite))
            {
                DrawSprite(spriteRect, sprite);
            }

            GUILayout.Space(5f * scale);

            var style = GetMemberStatusStyle(member);
            GUILayout.BeginVertical(GUILayout.Width(statusWidth));
            GUILayout.Label(GetShortName(member.Name), style, GUILayout.Width(statusWidth), GUILayout.Height(GetLineHeight(scale)));
            DrawProgressRow("HP", member.Health, member.MaxHealth, style, statusWidth, scale);
            if (member.MaxMagic != 0)
            {
                DrawProgressRow("MP", member.Magic, member.MaxMagic, style, statusWidth, scale);
            }
            else
            {
                GUILayout.Label(" ", style, GUILayout.Width(statusWidth), GUILayout.Height(GetLineHeight(scale)));
            }

            DrawSplitLabel(GetClassPrefix(member.Class) + ":", member.Level.ToString(), style, statusWidth, scale);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawSplitLabel(string label, string value, GUIStyle style, float width, float scale)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(GetLineHeight(scale)));
            GUILayout.Label(label, style, GUILayout.Width(width / 2f), GUILayout.Height(GetLineHeight(scale)));
            GUILayout.Label(value, style, GUILayout.Width(width / 2f), GUILayout.Height(GetLineHeight(scale)));
            GUILayout.EndHorizontal();
        }

        private void DrawProgressRow(string label, int value, int maxValue, GUIStyle style, float width, float scale)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(GetLineHeight(scale)));
            GUILayout.Label(label + ":", style, GUILayout.Width(24f * scale), GUILayout.Height(GetLineHeight(scale)));
            DrawProgressBar(maxValue <= 0 ? 0f : Mathf.Clamp01((float)value / maxValue), width - 28f * scale, 10f * scale);
            GUILayout.EndHorizontal();
        }

        private void DrawProgressBar(float progress, float width, float height)
        {
            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(GetLineHeight(GetPixelScale())));
            rect.y += (rect.height - height) / 2f;
            rect.height = height;
            GUI.Box(rect, GUIContent.none, uiTheme.ButtonStyle);

            var previousColor = GUI.color;
            GUI.color = Color.white;
            var inset = Mathf.Max(1f, uiTheme.BorderThickness);
            GUI.DrawTexture(
                new Rect(
                    rect.x + inset,
                    rect.y + inset,
                    Mathf.Max(0f, rect.width - inset * 2f) * progress,
                    Mathf.Max(0f, rect.height - inset * 2f)),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static float GetLineHeight(float scale)
        {
            return 16f * scale;
        }

        private GUIStyle GetMemberStatusStyle(Hero member)
        {
            if (member.IsDead)
            {
                return deadStyle;
            }

            if (member.MaxHealth > 0 && member.Health < member.MaxHealth / 10)
            {
                return lowHealthStyle;
            }

            return statusStyle;
        }

        private static string GetShortName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }

            return name.Length <= 10 ? name : name.Substring(0, 10);
        }

        private static string GetClassPrefix(Class heroClass)
        {
            var className = heroClass.ToString();
            return className.Length <= 3 ? className : className.Substring(0, 3);
        }

        private void EnsureReferences()
        {
            if (gameState == null)
            {
                gameState = DungeonEscapeGameState.GetOrCreate();
            }

            if (player == null)
            {
                player = FindAnyObjectByType<PlayerGridController>();
            }

            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = DungeonEscapeSettingsCache.Current;
            var themeSignature = DungeonEscapeUiTheme.GetSignature(settings);
            if (uiTheme != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                lastThemeSignature == themeSignature)
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = DungeonEscapeUiTheme.Create(settings, scale);

            statusStyle = new GUIStyle(uiTheme.SmallStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            deadStyle = new GUIStyle(statusStyle);
            deadStyle.normal.textColor = Color.red;
            deadStyle.hover.textColor = Color.red;
            deadStyle.active.textColor = Color.red;
            deadStyle.focused.textColor = Color.red;

            lowHealthStyle = new GUIStyle(statusStyle);
            var orange = new Color(1f, 0.55f, 0f, 1f);
            lowHealthStyle.normal.textColor = orange;
            lowHealthStyle.hover.textColor = orange;
            lowHealthStyle.active.textColor = orange;
            lowHealthStyle.focused.textColor = orange;
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var aspect = textureRect.width / textureRect.height;
            var drawWidth = rect.width;
            var drawHeight = drawWidth / aspect;
            if (drawHeight > rect.height)
            {
                drawHeight = rect.height;
                drawWidth = drawHeight * aspect;
            }

            var drawRect = new Rect(
                rect.x + (rect.width - drawWidth) / 2f,
                rect.y + (rect.height - drawHeight) / 2f,
                drawWidth,
                drawHeight);
            var texCoords = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(drawRect, texture, texCoords, true);
        }
    }
}
