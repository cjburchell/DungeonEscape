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

        private void Start()
        {
            playerMarker = FindObjectOfType<PlayerGridController>();
            mapView = FindObjectOfType<TiledMapView>();
        }

        private void OnGUI()
        {
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
            var panelHeight = 92f * scale;
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
                playerMarker = FindObjectOfType<PlayerGridController>();
            }

            if (mapView == null)
            {
                mapView = FindObjectOfType<TiledMapView>();
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

            return builder.ToString();
        }
    }
}
