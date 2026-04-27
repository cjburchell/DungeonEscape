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
        public static int RenderVisibleTileLayers(
            Transform parent,
            IEnumerable<XElement> layers,
            IList<TiledTilesetSpriteSet> spriteSets,
            int mapWidth,
            int mapHeight,
            int startColumn,
            int startRow,
            int columns,
            int rows)
        {
            var renderedTileCount = 0;
            var layerOrder = 0;
            var clampedStartColumn = Math.Max(0, Math.Min(startColumn, mapWidth - 1));
            var clampedStartRow = Math.Max(0, Math.Min(startRow, mapHeight - 1));
            var visibleColumns = Math.Min(columns, mapWidth - clampedStartColumn);
            var visibleRows = Math.Min(rows, mapHeight - clampedStartRow);

            foreach (var layer in layers)
            {
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
                        renderer.sortingOrder = layerOrder;
                        renderedTileCount++;
                    }
                }

                layerOrder++;
            }

            return renderedTileCount;
        }

        public static void RenderObjectSprites(
            Transform parent,
            TiledMapInfo map,
            IList<TiledTilesetSpriteSet> spriteSets,
            int startColumn,
            int startRow,
            int columns,
            int rows,
            int sortingOrder)
        {
            if (map == null || map.ObjectGroups == null)
            {
                return;
            }

            foreach (var group in map.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (mapObject.Gid == 0)
                    {
                        continue;
                    }

                    Sprite sprite;
                    if (!TiledTilesetSprites.TryGetSprite(mapObject.Gid, spriteSets, out sprite))
                    {
                        continue;
                    }

                    var column = Mathf.FloorToInt(mapObject.X / map.TileWidth);
                    var row = Mathf.FloorToInt((mapObject.Y - mapObject.Height) / map.TileHeight);

                    if (column < startColumn || column >= startColumn + columns ||
                        row < startRow || row >= startRow + rows)
                    {
                        continue;
                    }

                    var markerObject = new GameObject("Object_" + group.Name + "_" + mapObject.Name);
                    markerObject.transform.SetParent(parent, false);
                    markerObject.transform.localPosition = new Vector3(column - startColumn, -(row - startRow), -0.1f);

                    var renderer = markerObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = sortingOrder;
                }
            }
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
    }
}
