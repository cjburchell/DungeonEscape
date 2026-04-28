using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class TiledMapCollision
    {
        public static HashSet<int> BuildBlockedTiles(
            XElement map,
            TiledMapInfo mapInfo,
            int mapWidth,
            int mapHeight)
        {
            var blocked = new HashSet<int>();

            foreach (var layer in map.Elements("layer"))
            {
                if (!IsBlockingLayer(layer))
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

            AddBlockedObjects(blocked, mapInfo, mapWidth, mapHeight);
            return blocked;
        }

        private static void AddBlockedObjects(HashSet<int> blocked, TiledMapInfo mapInfo, int mapWidth, int mapHeight)
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

                    string collideable;
                    if (mapObject.Properties == null ||
                        !mapObject.Properties.TryGetValue("Collideable", out collideable) ||
                        !IsTrue(collideable))
                    {
                        continue;
                    }

                    var column = Mathf.FloorToInt(mapObject.X / mapInfo.TileWidth);
                    var row = IsNpcObject(mapObject)
                        ? Mathf.FloorToInt((mapObject.Y - 0.001f) / mapInfo.TileHeight)
                        : Mathf.FloorToInt((mapObject.Y - mapObject.Height) / mapInfo.TileHeight);

                    if (column < 0 || row < 0 || column >= mapWidth || row >= mapHeight)
                    {
                        continue;
                    }

                    blocked.Add(row * mapWidth + column);
                }
            }
        }

        private static bool IsBlockingLayer(XElement layer)
        {
            var properties = ReadProperties(layer);
            string collideable;
            if (properties.TryGetValue("Collideable", out collideable))
            {
                return IsTrue(collideable);
            }

            return false;
        }

        private static bool IsNpcObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   mapObject.Class != null &&
                   mapObject.Class.StartsWith("Npc", StringComparison.OrdinalIgnoreCase);
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

            return data.Value
                .Split(new[] { ',', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
        }

        private static bool IsTrue(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }

        private static string GetString(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            return attribute == null ? null : attribute.Value;
        }
    }
}
