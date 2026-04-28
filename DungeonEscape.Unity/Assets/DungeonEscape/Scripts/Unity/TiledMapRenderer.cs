using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class TiledMapRenderer
    {
        public static int RenderVisibleLayers(
            Transform parent,
            IEnumerable<XElement> elements,
            IList<TiledTilesetSpriteSet> spriteSets,
            int mapWidth,
            int mapHeight,
            int tileWidth,
            int tileHeight,
            int startColumn,
            int startRow,
            int columns,
            int rows,
            out int spritesSortingOrder)
        {
            var renderedSpriteCount = 0;
            var sortingOrder = 0;
            spritesSortingOrder = 0;
            var clampedStartColumn = Math.Max(0, Math.Min(startColumn, mapWidth - 1));
            var clampedStartRow = Math.Max(0, Math.Min(startRow, mapHeight - 1));
            var visibleColumns = Math.Min(columns, mapWidth - clampedStartColumn);
            var visibleRows = Math.Min(rows, mapHeight - clampedStartRow);

            foreach (var element in elements)
            {
                if (element.Name.LocalName == "layer")
                {
                    renderedSpriteCount += RenderTileLayer(
                        parent,
                        element,
                        spriteSets,
                        mapWidth,
                        clampedStartColumn,
                        clampedStartRow,
                        visibleColumns,
                        visibleRows,
                        sortingOrder);
                }
                else if (element.Name.LocalName == "objectgroup")
                {
                    if (HasPropertyValue(element, "RenderRole", "Sprites"))
                    {
                        spritesSortingOrder = sortingOrder;
                    }

                    renderedSpriteCount += RenderObjectGroup(
                        parent,
                        element,
                        spriteSets,
                        tileWidth,
                        tileHeight,
                        startColumn,
                        startRow,
                        columns,
                        rows,
                        sortingOrder);
                }

                sortingOrder++;
            }

            return renderedSpriteCount;
        }

        private static int RenderTileLayer(
            Transform parent,
            XElement layer,
            IList<TiledTilesetSpriteSet> spriteSets,
            int mapWidth,
            int clampedStartColumn,
            int clampedStartRow,
            int visibleColumns,
            int visibleRows,
            int sortingOrder)
        {
            var renderedTileCount = 0;
            var gids = ParseCsvTileData(layer);

            for (var row = 0; row < visibleRows; row++)
            {
                for (var column = 0; column < visibleColumns; column++)
                {
                    var sourceColumn = clampedStartColumn + column;
                    var sourceRow = clampedStartRow + row;
                    var gid = gids[sourceRow * mapWidth + sourceColumn];
                    if (gid == 0)
                    {
                        continue;
                    }

                    Sprite sprite;
                    if (!TiledTilesetSprites.TryGetSprite(gid, spriteSets, out sprite))
                    {
                        continue;
                    }

                    var tileObject = new GameObject("Tile_" + GetString(layer, "name") + "_" + column + "_" + row);
                    tileObject.transform.SetParent(parent, false);
                    tileObject.transform.localPosition = new Vector3(column, -row, 0);

                    var renderer = tileObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = sortingOrder;
                    renderedTileCount++;
                }
            }

            return renderedTileCount;
        }

        private static int RenderObjectGroup(
            Transform parent,
            XElement objectGroup,
            IList<TiledTilesetSpriteSet> spriteSets,
            int tileWidth,
            int tileHeight,
            int startColumn,
            int startRow,
            int columns,
            int rows,
            int sortingOrder)
        {
            var renderedObjectCount = 0;
            var groupName = GetString(objectGroup, "name");

            foreach (var mapObject in objectGroup.Elements("object"))
            {
                var gid = GetInt(mapObject, "gid");
                if (gid == 0)
                {
                    continue;
                }

                Sprite sprite;
                if (!TiledTilesetSprites.TryGetSprite(gid, spriteSets, out sprite))
                {
                    continue;
                }

                var x = GetFloat(mapObject, "x");
                var y = GetFloat(mapObject, "y");
                var height = GetFloat(mapObject, "height");
                var column = Mathf.FloorToInt(x / tileWidth);
                var row = Mathf.FloorToInt((y - height) / tileHeight);

                if (column < startColumn || column >= startColumn + columns ||
                    row < startRow || row >= startRow + rows)
                {
                    continue;
                }

                var markerObject = new GameObject("Object_" + groupName + "_" + GetString(mapObject, "name"));
                markerObject.transform.SetParent(parent, false);
                markerObject.transform.localPosition = new Vector3(column - startColumn, -(row - startRow), -0.1f);

                var renderer = markerObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder;
                renderedObjectCount++;
            }

            return renderedObjectCount;
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

        private static string GetString(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            return attribute == null ? null : attribute.Value;
        }

        private static bool HasPropertyValue(XElement element, string propertyName, string expectedValue)
        {
            var properties = element.Element("properties");
            if (properties == null)
            {
                return false;
            }

            foreach (var property in properties.Elements("property"))
            {
                if (!string.Equals(GetString(property, "name"), propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = GetString(property, "value") ?? property.Value;
                return string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static int GetInt(XElement element, string name)
        {
            var value = GetString(element, name);
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }

        private static float GetFloat(XElement element, string name)
        {
            var value = GetString(element, name);
            float result;
            return float.TryParse(value, out result) ? result : 0f;
        }
    }
}
