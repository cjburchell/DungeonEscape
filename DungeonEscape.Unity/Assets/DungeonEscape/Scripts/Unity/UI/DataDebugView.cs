using System.Text;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class DataDebugView : MonoBehaviour
    {
        private GUIStyle textStyle;
        private UiSettings uiSettings;
        private float lastPixelScale;
        private View mapView;
        private GameState gameState;

        private void Start()
        {
            mapView = FindAnyObjectByType<View>();
            gameState = FindAnyObjectByType<GameState>();
        }

        private void OnGUI()
        {
            if (!SettingsCache.Current.MapDebugInfo)
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
                uiSettings = UiSettings.GetOrCreate();
            }

            return uiSettings.PixelScale;
        }

        private string BuildRuntimeSummary()
        {
            if (mapView == null)
            {
                mapView = FindAnyObjectByType<View>();
            }

            if (gameState == null)
            {
                gameState = FindAnyObjectByType<GameState>();
            }

            var builder = new StringBuilder();

            if (mapView != null)
            {
                builder.AppendLine("Viewport: " + mapView.StartColumn + ", " + mapView.StartRow);
            }

            if (gameState != null && gameState.Party != null)
            {
                builder.AppendLine("Map: " + gameState.Party.CurrentMapId);
                builder.AppendLine("Biome: " + gameState.Party.CurrentBiome);
                builder.AppendLine("Steps: " + gameState.Party.StepCount);
            }

            return builder.ToString();
        }
    }
}
