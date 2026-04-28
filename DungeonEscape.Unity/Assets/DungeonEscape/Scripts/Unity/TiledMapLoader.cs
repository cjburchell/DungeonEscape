using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class TiledMapLoader
    {
        private static readonly Dictionary<string, TiledLoadedMap> LoadedMaps = new Dictionary<string, TiledLoadedMap>();
        private static readonly Dictionary<string, TiledTilesetDocumentInfo> TilesetDocuments = new Dictionary<string, TiledTilesetDocumentInfo>();

        public static TiledLoadedMap Load(string mapAssetPath)
        {
            mapAssetPath = NormalizeMapAssetPath(mapAssetPath);
            TiledLoadedMap cachedMap;
            if (LoadedMaps.TryGetValue(mapAssetPath, out cachedMap))
            {
                return cachedMap;
            }

            var mapPath = ToFullAssetPath(mapAssetPath);

            if (!File.Exists(mapPath))
            {
                Debug.LogError("Map TMX not found: " + mapAssetPath);
                return null;
            }

            var text = File.ReadAllText(mapPath);
            var document = XDocument.Parse(text);
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

            var renderableElements = map.Elements()
                .Where(element =>
                    element.Name.LocalName == "layer" && IsRenderableLayer(element) ||
                    element.Name.LocalName == "objectgroup" && IsRenderableObjectGroup(element))
                .ToList();

            var info = TiledMapInfo.Parse(text);
            ValidateTilesets(info);

            var loadedMap = new TiledLoadedMap
            {
                Root = map,
                Info = info,
                AssetPath = mapAssetPath,
                VisibleLayers = layers,
                RenderableElements = renderableElements,
                Width = GetInt(map, "width"),
                Height = GetInt(map, "height"),
                TileWidth = GetInt(map, "tilewidth"),
                TileHeight = GetInt(map, "tileheight")
            };

            LoadedMaps[mapAssetPath] = loadedMap;
            return loadedMap;
        }

        public static void ClearCache()
        {
            LoadedMaps.Clear();
            TilesetDocuments.Clear();
        }

        public static string NormalizeMapAssetPath(string mapIdOrAssetPath)
        {
            if (string.IsNullOrEmpty(mapIdOrAssetPath))
            {
                return "Assets/DungeonEscape/Maps/overworld.tmx";
            }

            var normalized = mapIdOrAssetPath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase)
                    ? normalized
                    : normalized + ".tmx";
            }

            const string mapsPrefix = "maps/";
            if (normalized.StartsWith(mapsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(mapsPrefix.Length);
            }

            if (!normalized.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase))
            {
                normalized += ".tmx";
            }

            return "Assets/DungeonEscape/Maps/" + normalized;
        }

        private static void ValidateTilesets(TiledMapInfo map)
        {
            if (map == null || map.Tilesets == null)
            {
                return;
            }

            foreach (var tileset in map.Tilesets)
            {
                if (string.IsNullOrEmpty(tileset.Source))
                {
                    tileset.TilesetFound = true;
                    continue;
                }

                tileset.UnityTilesetPath = ResolveTilesetAssetPath(tileset.Source);
                var tilesetFullPath = ToFullAssetPath(tileset.UnityTilesetPath);
                tileset.TilesetFound = File.Exists(tilesetFullPath);
                if (!tileset.TilesetFound)
                {
                    Debug.LogWarning("Tileset not found: " + tileset.UnityTilesetPath);
                    continue;
                }

                TiledTilesetDocumentInfo document;
                if (!TilesetDocuments.TryGetValue(tileset.UnityTilesetPath, out document))
                {
                    document = TiledTilesetDocumentInfo.Parse(File.ReadAllText(tilesetFullPath));
                    TilesetDocuments[tileset.UnityTilesetPath] = document;
                }

                tileset.Document = document;
                tileset.UnityImagePath = ResolveTilesetImageAssetPath(tileset.Document.ImageSource);
                tileset.ImageFound = File.Exists(ToFullAssetPath(tileset.UnityImagePath));
                if (!tileset.ImageFound)
                {
                    Debug.LogWarning("Tileset image not found: " + tileset.UnityImagePath);
                }
            }
        }

        private static string ResolveTilesetAssetPath(string source)
        {
            return "Assets/DungeonEscape/Tilesets/" + Path.GetFileName(source);
        }

        private static string ResolveTilesetImageAssetPath(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            var normalized = source.Replace('\\', '/');
            const string imagesPrefix = "images/";
            if (normalized.StartsWith(imagesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(imagesPrefix.Length);
            }
            else
            {
                const string imagesSegment = "/Images/";
                var imagesIndex = normalized.IndexOf(imagesSegment, StringComparison.OrdinalIgnoreCase);
                if (imagesIndex >= 0)
                {
                    normalized = normalized.Substring(imagesIndex + imagesSegment.Length);
                }
            }

            return "Assets/DungeonEscape/Images/" + normalized;
        }

        private static bool IsRenderableLayer(XElement layer)
        {
            return GetString(layer, "visible") != "0";
        }

        private static bool IsRenderableObjectGroup(XElement objectGroup)
        {
            return GetString(objectGroup, "visible") != "0";
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
        public string AssetPath { get; set; }
        public XElement Root { get; set; }
        public TiledMapInfo Info { get; set; }
        public List<XElement> VisibleLayers { get; set; }
        public List<XElement> RenderableElements { get; set; }
        public HashSet<int> BlockedTiles { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
    }
}
