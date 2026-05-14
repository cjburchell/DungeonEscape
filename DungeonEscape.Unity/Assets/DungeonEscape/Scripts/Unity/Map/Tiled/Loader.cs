using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Map.Tiled
{
    public static class Loader
    {
        private static readonly Dictionary<string, LoadedMap> LoadedMaps = new Dictionary<string, LoadedMap>();
        private static readonly Dictionary<string, TiledTilesetDocumentInfo> TilesetDocuments = new Dictionary<string, TiledTilesetDocumentInfo>();

        public static LoadedMap Load(string mapAssetPath)
        {
            mapAssetPath = NormalizeMapAssetPath(mapAssetPath);
            LoadedMap cachedMap;
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
            ValidateWarps(info, mapAssetPath);

            var loadedMap = new LoadedMap
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
            return TiledMapPaths.NormalizeMapAssetPath(mapIdOrAssetPath);
        }

        public static string NormalizeMapId(string mapIdOrAssetPath)
        {
            return TiledMapPaths.NormalizeMapId(mapIdOrAssetPath);
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
                    tileset.ImageFound = true;
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

        private static void ValidateWarps(TiledMapInfo map, string sourceMapAssetPath)
        {
            if (map == null || map.ObjectGroups == null)
            {
                return;
            }

            foreach (var group in map.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (!string.Equals(mapObject.Class, "Warp", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string targetMapId;
                    if (mapObject.Properties == null ||
                        !mapObject.Properties.TryGetValue("WarpMap", out targetMapId) ||
                        string.IsNullOrEmpty(targetMapId))
                    {
                        Debug.LogWarning("Warp is missing WarpMap in " + sourceMapAssetPath + " object " + mapObject.Id + ".");
                        continue;
                    }

                    var targetMapAssetPath = NormalizeMapAssetPath(targetMapId);
                    var targetMapPath = ToFullAssetPath(targetMapAssetPath);
                    if (!File.Exists(targetMapPath))
                    {
                        Debug.LogWarning("Warp target map not found from " + sourceMapAssetPath + " object " + mapObject.Id + ": " + targetMapAssetPath);
                        continue;
                    }

                    string spawnId;
                    if (mapObject.Properties.TryGetValue("SpawnId", out spawnId) && !string.IsNullOrEmpty(spawnId) &&
                        !TargetMapHasSpawn(targetMapAssetPath, spawnId))
                    {
                        Debug.LogWarning("Warp target spawn not found from " + sourceMapAssetPath + " object " + mapObject.Id + ": " + targetMapAssetPath + " / " + spawnId);
                    }
                }
            }
        }

        private static bool TargetMapHasSpawn(string mapAssetPath, string spawnId)
        {
            LoadedMap loadedMap;
            TiledMapInfo mapInfo;
            if (LoadedMaps.TryGetValue(mapAssetPath, out loadedMap) && loadedMap.Info != null)
            {
                mapInfo = loadedMap.Info;
            }
            else
            {
                var mapPath = ToFullAssetPath(mapAssetPath);
                if (!File.Exists(mapPath))
                {
                    return false;
                }

                mapInfo = TiledMapInfo.Parse(File.ReadAllText(mapPath));
            }

            if (mapInfo.ObjectGroups == null)
            {
                return false;
            }

            foreach (var group in mapInfo.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (string.Equals(mapObject.Class, "Spawn", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(mapObject.Name, spawnId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ResolveTilesetAssetPath(string source)
        {
            return TiledMapPaths.ResolveTilesetAssetPath(source);
        }

        private static string ResolveTilesetImageAssetPath(string source)
        {
            return TiledMapPaths.ResolveTilesetImageAssetPath(source);
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
            return UnityAssetPath.ToRuntimePath(assetPath);
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

    public sealed class LoadedMap
    {
        public string AssetPath { get; set; }
        public XElement Root { get; set; }
        public TiledMapInfo Info { get; set; }
        public List<XElement> VisibleLayers { get; set; }
        public List<XElement> RenderableElements { get; set; }
        public HashSet<int> BlockedTiles { get; set; }
        public bool? BlockedTilesAllowShip { get; set; }
        public Dictionary<XElement, int[]> LayerGidCache { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
    }
}
