using System.Text;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeDataDebugView : MonoBehaviour
    {
        private GUIStyle textStyle;
        private DungeonEscapeUiSettings uiSettings;
        private float lastPixelScale;
        private PlayerGridController playerMarker;
        private TiledMapView mapView;
        private DungeonEscapeGameState gameState;

        private void Start()
        {
            playerMarker = FindFirstObjectByType<PlayerGridController>();
            mapView = FindFirstObjectByType<TiledMapView>();
            gameState = FindFirstObjectByType<DungeonEscapeGameState>();
        }

        private void OnGUI()
        {
            if (!DungeonEscapeSettingsCache.Current.MapDebugInfo)
            {
                return;
            }

            var scale = GetPixelScale();
            if (textStyle == null || !Mathf.Approximately(lastPixelScale, scale))
            {
                lastPixelScale = scale;
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(16f * scale),
                    normal = { textColor = Color.white },
                    wordWrap = true
                };
            }

            var margin = 12f * scale;
            var padding = 12f * scale;
            var panelWidth = 270f * scale;
            var panelHeight = 132f * scale;
            GUI.Box(new Rect(margin, margin, panelWidth, panelHeight), GUIContent.none);
            GUI.Label(new Rect(margin + padding, margin + padding, panelWidth - padding * 2f, panelHeight - padding * 2f), BuildRuntimeSummary(), textStyle);
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            return uiSettings.PixelScale;
        }

        private string BuildRuntimeSummary()
        {
            if (playerMarker == null)
            {
                playerMarker = FindFirstObjectByType<PlayerGridController>();
            }

            if (mapView == null)
            {
                mapView = FindFirstObjectByType<TiledMapView>();
            }

            if (gameState == null)
            {
                gameState = FindFirstObjectByType<DungeonEscapeGameState>();
            }

            var builder = new StringBuilder();

            if (playerMarker == null)
            {
                builder.AppendLine("Player: none");
            }
            else
            {
                builder.AppendLine("Player tile: " + playerMarker.Column + ", " + playerMarker.Row);
            }

            if (mapView != null)
            {
                builder.AppendLine("Viewport: " + mapView.StartColumn + ", " + mapView.StartRow);
            }

            if (gameState != null && gameState.Party != null)
            {
                builder.AppendLine("Map: " + gameState.Party.CurrentMapId);
                builder.AppendLine("Steps: " + gameState.Party.StepCount);

                var hero = gameState.Party.GetOrderedHero(0);
                if (hero != null)
                {
                    builder.AppendLine("Hero: " + hero.Name + " L" + hero.Level + " HP " + hero.Health + "/" + hero.MaxHealth);
                    builder.AppendLine("Items: " + hero.Items.Count);
                }
                else
                {
                    builder.AppendLine("Hero: none");
                }
            }

            return builder.ToString();
        }
    }
}
