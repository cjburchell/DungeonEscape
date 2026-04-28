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
        private const int RenderBufferTiles = 2;
        private static Sprite hiddenObjectSprite;

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
            bool showHiddenObjects,
            out int spritesSortingOrder)
        {
            var renderedSpriteCount = 0;
            var sortingOrder = 0;
            spritesSortingOrder = 0;
            var clampedStartColumn = Math.Max(0, Math.Min(startColumn, mapWidth - 1));
            var clampedStartRow = Math.Max(0, Math.Min(startRow, mapHeight - 1));
            var renderStartColumn = Math.Max(0, clampedStartColumn - RenderBufferTiles);
            var renderStartRow = Math.Max(0, clampedStartRow - RenderBufferTiles);
            var renderEndColumn = Math.Min(mapWidth, clampedStartColumn + columns + RenderBufferTiles);
            var renderEndRow = Math.Min(mapHeight, clampedStartRow + rows + RenderBufferTiles);
            var renderColumns = renderEndColumn - renderStartColumn;
            var renderRows = renderEndRow - renderStartRow;

            foreach (var element in elements)
            {
                if (element.Name.LocalName == "layer")
                {
                    renderedSpriteCount += RenderTileLayer(
                        parent,
                        element,
                        spriteSets,
                        mapWidth,
                        renderStartColumn,
                        renderStartRow,
                        renderColumns,
                        renderRows,
                        clampedStartColumn,
                        clampedStartRow,
                        sortingOrder,
                        showHiddenObjects);
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
                        renderStartColumn,
                        renderStartRow,
                        renderColumns,
                        renderRows,
                        clampedStartColumn,
                        clampedStartRow,
                        sortingOrder,
                        showHiddenObjects);
                }

                sortingOrder++;
            }

            return renderedSpriteCount;
        }

        public static int RenderVisibleLayers(
            TiledSpriteRendererPool pool,
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
            bool showHiddenObjects,
            out int spritesSortingOrder)
        {
            var renderedSpriteCount = 0;
            var sortingOrder = 0;
            spritesSortingOrder = 0;
            var clampedStartColumn = Math.Max(0, Math.Min(startColumn, mapWidth - 1));
            var clampedStartRow = Math.Max(0, Math.Min(startRow, mapHeight - 1));
            var renderStartColumn = Math.Max(0, clampedStartColumn - RenderBufferTiles);
            var renderStartRow = Math.Max(0, clampedStartRow - RenderBufferTiles);
            var renderEndColumn = Math.Min(mapWidth, clampedStartColumn + columns + RenderBufferTiles);
            var renderEndRow = Math.Min(mapHeight, clampedStartRow + rows + RenderBufferTiles);
            var renderColumns = renderEndColumn - renderStartColumn;
            var renderRows = renderEndRow - renderStartRow;

            foreach (var element in elements)
            {
                if (element.Name.LocalName == "layer")
                {
                    renderedSpriteCount += RenderTileLayer(
                        pool,
                        element,
                        spriteSets,
                        mapWidth,
                        renderStartColumn,
                        renderStartRow,
                        renderColumns,
                        renderRows,
                        clampedStartColumn,
                        clampedStartRow,
                        sortingOrder,
                        showHiddenObjects);
                }
                else if (element.Name.LocalName == "objectgroup")
                {
                    if (HasPropertyValue(element, "RenderRole", "Sprites"))
                    {
                        spritesSortingOrder = sortingOrder;
                    }

                    renderedSpriteCount += RenderObjectGroup(
                        pool,
                        element,
                        spriteSets,
                        tileWidth,
                        tileHeight,
                        renderStartColumn,
                        renderStartRow,
                        renderColumns,
                        renderRows,
                        clampedStartColumn,
                        clampedStartRow,
                        sortingOrder,
                        showHiddenObjects);
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
            int viewportStartColumn,
            int viewportStartRow,
            int sortingOrder,
            bool showHiddenObjects)
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

                    var tileObject = new GameObject("Tile_" + GetString(layer, "name") + "_" + sourceColumn + "_" + sourceRow);
                    tileObject.transform.SetParent(parent, false);
                    tileObject.transform.localPosition = new Vector3(sourceColumn - viewportStartColumn, -(sourceRow - viewportStartRow), 0);

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
            int viewportStartColumn,
            int viewportStartRow,
            int sortingOrder,
            bool showHiddenObjects)
        {
            var renderedObjectCount = 0;
            var groupName = GetString(objectGroup, "name");

            foreach (var mapObject in objectGroup.Elements("object"))
            {
                var gid = GetInt(mapObject, "gid");
                if (gid == 0 && !showHiddenObjects)
                {
                    continue;
                }

                Sprite sprite;
                if (gid == 0)
                {
                    sprite = GetHiddenObjectSprite();
                }
                else if (!TiledTilesetSprites.TryGetSprite(gid, spriteSets, out sprite))
                {
                    continue;
                }

                var x = GetFloat(mapObject, "x");
                var y = GetFloat(mapObject, "y");
                var width = GetFloat(mapObject, "width");
                var height = GetFloat(mapObject, "height");
                int minColumn;
                int minRow;
                int maxColumn;
                int maxRow;
                GetObjectTileBounds(x, y, width, height, tileWidth, tileHeight, gid == 0, out minColumn, out minRow, out maxColumn, out maxRow);

                if (maxColumn < startColumn || minColumn >= startColumn + columns ||
                    maxRow < startRow || minRow >= startRow + rows)
                {
                    continue;
                }

                var markerObject = new GameObject("Object_" + groupName + "_" + GetString(mapObject, "name"));
                markerObject.transform.SetParent(parent, false);
                markerObject.transform.localPosition = GetObjectLocalPosition(
                    x,
                    y,
                    width,
                    height,
                    tileWidth,
                    tileHeight,
                    viewportStartColumn,
                    viewportStartRow,
                    gid == 0);
                markerObject.transform.localScale = GetObjectLocalScale(width, height, tileWidth, tileHeight, gid == 0);

                var renderer = markerObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder;
                renderedObjectCount++;
            }

            return renderedObjectCount;
        }

        private static int RenderTileLayer(
            TiledSpriteRendererPool pool,
            XElement layer,
            IList<TiledTilesetSpriteSet> spriteSets,
            int mapWidth,
            int clampedStartColumn,
            int clampedStartRow,
            int visibleColumns,
            int visibleRows,
            int viewportStartColumn,
            int viewportStartRow,
            int sortingOrder,
            bool showHiddenObjects)
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

                    var key = "Tile|" + sortingOrder + "|" + GetString(layer, "name") + "|" + sourceColumn + "|" + sourceRow;
                    pool.Show(
                        key,
                        sprite,
                        new Vector3(sourceColumn - viewportStartColumn, -(sourceRow - viewportStartRow), 0),
                        sortingOrder,
                        "Tile_" + GetString(layer, "name") + "_" + sourceColumn + "_" + sourceRow);
                    renderedTileCount++;
                }
            }

            return renderedTileCount;
        }

        private static int RenderObjectGroup(
            TiledSpriteRendererPool pool,
            XElement objectGroup,
            IList<TiledTilesetSpriteSet> spriteSets,
            int tileWidth,
            int tileHeight,
            int startColumn,
            int startRow,
            int columns,
            int rows,
            int viewportStartColumn,
            int viewportStartRow,
            int sortingOrder,
            bool showHiddenObjects)
        {
            var renderedObjectCount = 0;
            var groupName = GetString(objectGroup, "name");

            foreach (var mapObject in objectGroup.Elements("object"))
            {
                var gid = GetInt(mapObject, "gid");
                if (gid == 0 && !showHiddenObjects)
                {
                    continue;
                }

                Sprite sprite;
                if (gid == 0)
                {
                    sprite = GetHiddenObjectSprite();
                }
                else if (!TiledTilesetSprites.TryGetSprite(gid, spriteSets, out sprite))
                {
                    continue;
                }

                var x = GetFloat(mapObject, "x");
                var y = GetFloat(mapObject, "y");
                var width = GetFloat(mapObject, "width");
                var height = GetFloat(mapObject, "height");
                int minColumn;
                int minRow;
                int maxColumn;
                int maxRow;
                GetObjectTileBounds(x, y, width, height, tileWidth, tileHeight, gid == 0, out minColumn, out minRow, out maxColumn, out maxRow);

                if (maxColumn < startColumn || minColumn >= startColumn + columns ||
                    maxRow < startRow || minRow >= startRow + rows)
                {
                    continue;
                }

                var key = "Object|" + sortingOrder + "|" + groupName + "|" + GetString(mapObject, "id");
                pool.Show(
                    key,
                    sprite,
                    GetObjectLocalPosition(
                        x,
                        y,
                        width,
                        height,
                        tileWidth,
                        tileHeight,
                        viewportStartColumn,
                        viewportStartRow,
                        gid == 0),
                    GetObjectLocalScale(width, height, tileWidth, tileHeight, gid == 0),
                    sortingOrder,
                    "Object_" + groupName + "_" + GetString(mapObject, "name"));
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

        private static Sprite GetHiddenObjectSprite()
        {
            if (hiddenObjectSprite != null)
            {
                return hiddenObjectSprite;
            }

            var texture = new Texture2D(16, 16);
            var clear = new Color(0f, 0f, 0f, 0f);
            var marker = new Color(1f, 0.1f, 0.1f, 0.75f);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var border = x == 0 || y == 0 || x == texture.width - 1 || y == texture.height - 1;
                    var diagonal = x == y || x == texture.width - y - 1;
                    texture.SetPixel(x, y, border || diagonal ? marker : clear);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();
            hiddenObjectSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            return hiddenObjectSprite;
        }

        private static Vector3 GetObjectLocalPosition(
            float x,
            float y,
            float width,
            float height,
            int tileWidth,
            int tileHeight,
            int viewportStartColumn,
            int viewportStartRow,
            bool useObjectBounds)
        {
            var objectWidth = width <= 0f ? tileWidth : width;
            var objectHeight = height <= 0f ? tileHeight : height;
            var centerX = (x + objectWidth / 2f) / tileWidth;
            var centerY = useObjectBounds
                ? (y + objectHeight / 2f) / tileHeight
                : (y - objectHeight / 2f) / tileHeight;

            if (!useObjectBounds)
            {
                return new Vector3(
                    centerX - viewportStartColumn - 0.5f,
                    -(centerY - viewportStartRow) + 0.5f,
                    -0.1f);
            }

            return new Vector3(
                centerX - viewportStartColumn - 0.5f,
                -(centerY - viewportStartRow) + 0.5f,
                -0.1f);
        }

        private static Vector3 GetObjectLocalScale(float width, float height, int tileWidth, int tileHeight, bool useObjectBounds)
        {
            if (!useObjectBounds)
            {
                return Vector3.one;
            }

            var objectWidth = width <= 0f ? tileWidth : width;
            var objectHeight = height <= 0f ? tileHeight : height;
            return new Vector3(objectWidth / tileWidth, objectHeight / tileHeight, 1f);
        }

        private static void GetObjectTileBounds(
            float x,
            float y,
            float width,
            float height,
            int tileWidth,
            int tileHeight,
            bool useObjectBounds,
            out int minColumn,
            out int minRow,
            out int maxColumn,
            out int maxRow)
        {
            var objectWidth = width <= 0f ? tileWidth : width;
            var objectHeight = height <= 0f ? tileHeight : height;
            var left = x;
            var right = x + objectWidth;
            var top = useObjectBounds ? y : y - objectHeight;
            var bottom = useObjectBounds ? y + objectHeight : y;

            minColumn = Mathf.FloorToInt(left / tileWidth);
            maxColumn = Mathf.FloorToInt((right - 0.001f) / tileWidth);
            minRow = Mathf.FloorToInt(top / tileHeight);
            maxRow = Mathf.FloorToInt((bottom - 0.001f) / tileHeight);
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
