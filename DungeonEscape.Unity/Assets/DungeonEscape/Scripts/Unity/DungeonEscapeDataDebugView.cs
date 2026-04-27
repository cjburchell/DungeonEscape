using System.Linq;
using System.Text;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeDataDebugView : MonoBehaviour
    {
        [SerializeField]
        private DungeonEscapeBootstrap bootstrap;

        private string rightText;
        private GUIStyle textStyle;
        private PlayerPreviewMarker playerMarker;
        private TiledMapPreviewRenderer mapPreview;

        private void Start()
        {
            if (bootstrap == null)
            {
                bootstrap = FindObjectOfType<DungeonEscapeBootstrap>();
            }

            playerMarker = FindObjectOfType<PlayerPreviewMarker>();
            mapPreview = FindObjectOfType<TiledMapPreviewRenderer>();

            if (bootstrap == null || bootstrap.Data == null)
            {
                rightText = "Dungeon Escape data has not loaded.";
                return;
            }

            rightText = BuildSummary(bootstrap.Data);
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

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            var panelWidth = 420f;
            var panelHeight = 320f;
            GUI.Box(new Rect(12, 12, panelWidth, panelHeight), GUIContent.none);
            GUI.Label(new Rect(24, 24, panelWidth - 24, panelHeight - 24), BuildRuntimeSummary(), textStyle);

            var mapPanelWidth = 520f;
            GUI.Box(new Rect(Screen.width - mapPanelWidth - 12, 12, mapPanelWidth, panelHeight), GUIContent.none);
            GUI.Label(new Rect(Screen.width - mapPanelWidth, 24, mapPanelWidth - 24, panelHeight - 24), rightText ?? "", textStyle);
        }

        private string BuildRuntimeSummary()
        {
            if (playerMarker == null)
            {
                playerMarker = FindObjectOfType<PlayerPreviewMarker>();
            }

            if (mapPreview == null)
            {
                mapPreview = FindObjectOfType<TiledMapPreviewRenderer>();
            }

            var builder = new StringBuilder();
            builder.AppendLine("Preview");
            builder.AppendLine();

            if (playerMarker == null)
            {
                builder.AppendLine("Player: none");
            }
            else
            {
                builder.AppendLine("Player tile: " + playerMarker.Column + ", " + playerMarker.Row);
            }

            if (mapPreview != null)
            {
                builder.AppendLine("Viewport: " + mapPreview.StartColumn + ", " + mapPreview.StartRow);
            }

            builder.AppendLine();
            builder.AppendLine("Move: WASD / Arrow keys");
            builder.AppendLine("Blocked: wall, water, water2");
            return builder.ToString();
        }

        private static string BuildSummary(DungeonEscapeDataSet data)
        {
            var builder = new StringBuilder();
            AppendMap(builder, data.TestMap);
            return builder.ToString();
        }

        private static void AppendMap(StringBuilder builder, TiledMapInfo map)
        {
            builder.AppendLine();
            builder.AppendLine("Test map:");
            if (map == null)
            {
                builder.AppendLine("  none");
                return;
            }

            builder.AppendLine("  class: " + map.Class);
            builder.AppendLine("  size: " + map.Width + "x" + map.Height + " tiles");
            builder.AppendLine("  tile size: " + map.TileWidth + "x" + map.TileHeight);
            builder.AppendLine("  tilesets: " + Count(map.Tilesets));
            builder.AppendLine("  layers: " + Count(map.Layers));
            builder.AppendLine("  visible layers: " + (map.Layers == null ? 0 : map.Layers.Count(layer => layer.Visible)));
            builder.AppendLine("  object groups: " + Count(map.ObjectGroups));
            builder.AppendLine("  objects: " + (map.ObjectGroups == null ? 0 : map.ObjectGroups.Sum(group => group.ObjectCount)));
            AppendSamples(builder, "Visible map layers", map.Layers == null ? null : map.Layers.Where(layer => layer.Visible).Select(layer => layer.Name).Take(8));
            AppendSamples(builder, "Object groups", map.ObjectGroups == null ? null : map.ObjectGroups.Select(group => group.Name + " (" + group.ObjectCount + ")").Take(8));
            AppendTilesets(builder, map);
        }

        private static void AppendTilesets(StringBuilder builder, TiledMapInfo map)
        {
            builder.AppendLine("Tilesets:");
            if (map.Tilesets == null || map.Tilesets.Count == 0)
            {
                builder.AppendLine("  none");
                return;
            }

            foreach (var tileset in map.Tilesets.Take(8))
            {
                var name = string.IsNullOrEmpty(tileset.Source) ? tileset.Name : tileset.Source;
                var status = tileset.TilesetFound ? "tsx found" : "tsx missing";
                var image = tileset.ImageFound ? "image found" : "image missing";
                if (string.IsNullOrEmpty(tileset.Source))
                {
                    image = "embedded image";
                }

                builder.AppendLine("  " + name);
                builder.AppendLine("    " + status + ", " + image);
                if (!string.IsNullOrEmpty(tileset.UnityImagePath))
                {
                    builder.AppendLine("    " + tileset.UnityImagePath);
                }
            }
        }

        private static void AppendSamples(StringBuilder builder, string label, System.Collections.Generic.IEnumerable<string> values)
        {
            builder.AppendLine(label + ":");

            if (values == null)
            {
                builder.AppendLine("  none");
                return;
            }

            var any = false;
            foreach (var value in values)
            {
                any = true;
                builder.AppendLine("  " + value);
            }

            if (!any)
            {
                builder.AppendLine("  none");
            }
        }

        private static int Count<T>(System.Collections.Generic.ICollection<T> values)
        {
            return values == null ? 0 : values.Count;
        }
    }
}
