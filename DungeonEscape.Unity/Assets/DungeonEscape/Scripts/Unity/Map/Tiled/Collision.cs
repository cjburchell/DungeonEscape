using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Map.Tiled
{
    public static class Collision
    {
        public static HashSet<int> BuildBlockedTiles(
            XElement map,
            TiledMapInfo mapInfo,
            int mapWidth,
            int mapHeight,
            GameState gameState,
            string mapId)
        {
            var blocked = new HashSet<int>();

            foreach (var layer in map.Elements("layer"))
            {
                if (!IsBlockingLayer(layer, gameState, mapId))
                {
                    continue;
                }

                var gids = ParseCsvTileData(layer);
                for (var i = 0; i < gids.Count; i++)
                {
                    if (gids[i] != 0)
                    {
                        blocked.Add(i);
                    }
                }
            }

            AddBlockedObjects(blocked, mapInfo, mapWidth, mapHeight, gameState, mapId);
            return blocked;
        }

        private static void AddBlockedObjects(
            HashSet<int> blocked,
            TiledMapInfo mapInfo,
            int mapWidth,
            int mapHeight,
            GameState gameState,
            string mapId)
        {
            if (mapInfo == null || mapInfo.ObjectGroups == null)
            {
                return;
            }

            foreach (var group in mapInfo.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (IsNpcObject(mapObject))
                    {
                        continue;
                    }

                    if (gameState != null &&
                        (IsDoorObject(mapObject) && gameState.IsObjectOpen(mapId, mapObject.Id) ||
                         gameState.IsMapObjectRemoved(mapId, mapObject)))
                    {
                        continue;
                    }

                    string collideable;
                    if (mapObject.Properties == null ||
                        !mapObject.Properties.TryGetValue("Collideable", out collideable) ||
                        !TiledTileData.IsTrue(collideable))
                    {
                        continue;
                    }

                    AddBlockedObjectBounds(blocked, mapObject, mapInfo, mapWidth, mapHeight);
                }
            }
        }

        private static void AddBlockedObjectBounds(
            HashSet<int> blocked,
            TiledObjectInfo mapObject,
            TiledMapInfo mapInfo,
            int mapWidth,
            int mapHeight)
        {
            foreach (var index in TiledTileData.GetObjectBoundsTileIndexes(
                         mapObject,
                         mapInfo.TileWidth,
                         mapInfo.TileHeight,
                         mapWidth,
                         mapHeight))
            {
                blocked.Add(index);
            }
        }

        private static bool IsBlockingLayer(XElement layer, GameState gameState, string mapId)
        {
            var properties = ReadProperties(layer);
            string water;
            if (properties.TryGetValue("Water", out water) &&
                TiledTileData.IsTrue(water) &&
                gameState != null &&
                gameState.Party != null &&
                IsOverworldMap(mapId) &&
                gameState.Party.HasShip)
            {
                return false;
            }

            string collideable;
            if (properties.TryGetValue("Collideable", out collideable))
            {
                return TiledTileData.IsTrue(collideable);
            }

            return false;
        }

        private static bool IsOverworldMap(string mapId)
        {
            return string.Equals(Loader.NormalizeMapId(mapId), "overworld", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNpcObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   mapObject.Class != null &&
                   mapObject.Class.StartsWith("Npc", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDoorObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "Door", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetIntProperty(TiledObjectInfo mapObject, string propertyName, int defaultValue)
        {
            string value;
            int result;
            return mapObject.Properties != null &&
                   mapObject.Properties.TryGetValue(propertyName, out value) &&
                   int.TryParse(value, out result)
                ? result
                : defaultValue;
        }

        private static Dictionary<string, string> ReadProperties(XElement element)
        {
            var result = new Dictionary<string, string>();
            var properties = element.Element("properties");
            if (properties == null)
            {
                return result;
            }

            foreach (var property in properties.Elements("property"))
            {
                var name = GetString(property, "name");
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var value = GetString(property, "value");
                result[name] = value ?? property.Value;
            }

            return result;
        }

        private static List<int> ParseCsvTileData(XElement layer)
        {
            var data = layer.Element("data");
            if (data == null)
            {
                return new List<int>();
            }

            return TiledTileData.ParseCsvTileData(data.Value);
        }

        private static string GetString(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            return attribute == null ? null : attribute.Value;
        }
    }
}
