using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeGoldWindow : MonoBehaviour
    {
        private DungeonEscapeGameState gameState;
        private PlayerGridController player;
        private DungeonEscapeUiSettings uiSettings;
        private DungeonEscapeUiTheme uiTheme;
        private GUIStyle goldStyle;
        private float lastPixelScale;
        private string lastThemeSignature;

        private void OnGUI()
        {
            if (DungeonEscapeTitleMenu.IsOpen ||
                DungeonEscapeGameMenu.IsOpen ||
                DungeonEscapeStoreWindow.IsOpen ||
                DungeonEscapeCombatWindow.IsOpen ||
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
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }
    }
}
