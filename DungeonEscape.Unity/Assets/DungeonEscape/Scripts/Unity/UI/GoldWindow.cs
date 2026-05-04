using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class GoldWindow : MonoBehaviour
    {
        private GameState gameState;
        private PlayerGridController player;
        private UiSettings uiSettings;
        private UiTheme uiTheme;
        private GUIStyle goldStyle;
        private float lastPixelScale;
        private string lastThemeSignature;

        private void OnGUI()
        {
            if (TitleMenu.IsOpen ||
                StoreWindow.IsOpen ||
                HealerWindow.IsOpen ||
                CombatWindow.IsOpen ||
                MessageBox.IsAnyVisible)
            {
                return;
            }

            EnsureReferences();
            if (!GameMenu.IsOpen && player != null && player.IsMovementActive)
            {
                return;
            }

            var party = gameState == null ? null : gameState.Party;
            if (party == null)
            {
                return;
            }

            EnsureStyles();
            DrawWindow(party.Gold);
        }

        private void DrawWindow(int gold)
        {
            var scale = GetPixelScale();
            var windowWidth = 150f * scale;
            var windowHeight = 50f * scale;
            var margin = 10f * scale;
            var windowRect = new Rect(
                margin,
                Screen.height - windowHeight - margin,
                windowWidth,
                windowHeight);

            GUI.Box(windowRect, GUIContent.none, uiTheme.PanelStyle);
            GUI.Label(windowRect, "Gold: " + gold, goldStyle);
        }

        private void EnsureReferences()
        {
            if (gameState == null)
            {
                gameState = GameState.GetOrCreate();
            }

            if (player == null)
            {
                player = FindAnyObjectByType<PlayerGridController>();
            }

            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
            }
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = SettingsCache.Current;
            var themeSignature = UiTheme.GetSignature(settings);
            if (uiTheme != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                lastThemeSignature == themeSignature)
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = UiTheme.Create(settings, scale);
            goldStyle = new GUIStyle(uiTheme.LabelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }
    }
}
