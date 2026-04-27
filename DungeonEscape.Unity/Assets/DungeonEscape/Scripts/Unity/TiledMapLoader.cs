using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class TiledMapLoader
    {
        public static TiledLoadedMap Load(string mapAssetPath)
        {
            var mapPath = ToFullAssetPath(mapAssetPath);

            if (!File.Exists(mapPath))
            {
                Debug.LogError("Map TMX not found: " + mapAssetPath);
                return null;
            }

            var document = XDocument.Parse(File.ReadAllText(mapPath));
            var map = document.Root;
            if (map == null)
            {
                Debug.LogError("Map TMX has no root element: " + mapAssetPath);
                return null;
            }

            var layers = map.Elements("layer").Where(IsRenderableLayer).ToList();
            if (layers.Count == 0)
            {
                Debug.LogError("Map TMX has no visible tile layers: " + mapAssetPath);
                return null;
            }

            return new TiledLoadedMap
            {
                Root = map,
                VisibleLayers = layers,
                Width = GetInt(map, "width"),
                Height = GetInt(map, "height"),
                TileWidth = GetInt(map, "tilewidth"),
                TileHeight = GetInt(map, "tileheight")
            };
        }

        private static bool IsRenderableLayer(XElement layer)
        {
            return GetString(layer, "visible") != "0";
        }

        private static string ToFullAssetPath(string assetPath)
        {
            return Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
        }

        private static string GetString(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            return attribute == null ? null : attribute.Value;
        }

        private static int GetInt(XElement element, string name)
        {
            var value = GetString(element, name);
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }
    }

    public sealed class TiledLoadedMap
    {
        public XElement Root { get; set; }
        public List<XElement> VisibleLayers { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
    }
}
