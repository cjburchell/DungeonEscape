using System.Text;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeDataDebugView : MonoBehaviour
    {
        [SerializeField]
        private DungeonEscapeBootstrap bootstrap;

        private GUIStyle textStyle;
        private PlayerGridController playerMarker;
        private TiledMapView mapView;

        private void Start()
        {
            if (bootstrap == null)
            {
                bootstrap = FindObjectOfType<DungeonEscapeBootstrap>();
            }

            playerMarker = FindObjectOfType<PlayerGridController>();
            mapView = FindObjectOfType<TiledMapView>();
        }

        private void OnGUI()
        {
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    normal = { textColor = Color.white },
                    wordWrap = true
                };
            }

            var panelWidth = 270f;
            var panelHeight = 92f;
            GUI.Box(new Rect(12, 12, panelWidth, panelHeight), GUIContent.none);
            GUI.Label(new Rect(24, 24, panelWidth - 24, panelHeight - 24), BuildRuntimeSummary(), textStyle);
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
