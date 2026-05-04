using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Map.Tiled
{
    public static class Renderer
    {
        private const float TileLayerOverlapScale = 1.006f;
        private const uint TiledGidMask = 0x1FFFFFFF;
        private static Sprite hiddenObjectSprite;

        public static int RenderVisibleLayers(
            SpriteRendererPool pool,
            IEnumerable<XElement> elements,
            IList<TilesetSpriteSet> spriteSets,
            int mapWidth,
            int mapHeight,
            int tileWidth,
            int tileHeight,
            bool showHiddenObjects,
            string mapId,
            GameState gameState,
            out int spritesSortingOrder)
        {
            var renderedSpriteCount = 0;
            var sortingOrder = 0;
            spritesSortingOrder = 0;
            var renderStartColumn = 0;
            var renderStartRow = 0;
            var renderEndColumn = mapWidth;
            var renderEndRow = mapHeight;
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
                        tileWidth,
                        tileHeight,
                        renderStartColumn,
                        renderStartRow,
                        renderColumns,
                        renderRows,
                        sortingOrder);
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
                        sortingOrder,
                        showHiddenObjects,
                        mapId,
                        gameState);
                }

                sortingOrder++;
            }

            return renderedSpriteCount;
        }

        private static int RenderTileLayer(
            SpriteRendererPool pool,
            XElement layer,
            IList<TilesetSpriteSet> spriteSets,
            int mapWidth,
            int tileWidth,
            int tileHeight,
            int startColumn,
            int startRow,
            int visibleColumns,
            int visibleRows,
            int sortingOrder)
        {
            var renderedTileCount = 0;
            var gids = ParseCsvTileData(layer);
            var layerTexture = new Texture2D(
                Math.Max(1, visibleColumns * tileWidth),
                Math.Max(1, visibleRows * tileHeight));
            layerTexture.filterMode = FilterMode.Point;
            layerTexture.wrapMode = TextureWrapMode.Clamp;
            var clearPixels = new Color[layerTexture.width * layerTexture.height];
            for (var i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = Color.clear;
            }

            layerTexture.SetPixels(clearPixels);
            var animatedTiles = new List<TileSpriteRenderInfo>();

            for (var row = 0; row < visibleRows; row++)
            {
                for (var column = 0; column < visibleColumns; column++)
                {
                    var sourceColumn = startColumn + column;
                    var sourceRow = startRow + row;
                    var gid = gids[sourceRow * mapWidth + sourceColumn];
                    if (gid == 0)
                    {
                        continue;
                    }

                    Sprite sprite;
                    if (!TilesetSprites.TryGetSprite(gid, spriteSets, out sprite))
                    {
                        continue;
                    }

                    List<SpriteAnimationFrame> animationFrames;
                    TilesetSprites.TryGetAnimation(gid, spriteSets, out animationFrames);

                    BlitSprite(layerTexture, sprite, column, row, visibleRows);
                    if (animationFrames != null && animationFrames.Count > 1)
                    {
                        animatedTiles.Add(new TileSpriteRenderInfo
                        {
                            Sprite = sprite,
                            AnimationFrames = animationFrames,
                            SourceColumn = sourceColumn,
                            SourceRow = sourceRow
                        });
                    }

                    renderedTileCount++;
                }
            }

            layerTexture.Apply(false, false);
            var layerName = GetString(layer, "name");
            var layerKey = "TileLayer|" + sortingOrder + "|" + layerName;
            pool.ShowGeneratedTexture(
                layerKey,
                layerTexture,
                GetLayerTextureLocalPosition(
                    startColumn,
                    startRow,
                    visibleColumns,
                    visibleRows),
                GetLayerSortingOrder(sortingOrder),
                "TileLayer_" + layerName,
                tileWidth);

            foreach (var tile in animatedTiles)
            {
                var key = "TileAnimation|" + sortingOrder + "|" + layerName + "|" + tile.SourceColumn + "|" + tile.SourceRow;
                pool.Show(
                    key,
                    tile.Sprite,
                    tile.AnimationFrames,
                    new Vector3(tile.SourceColumn, -tile.SourceRow, 0),
                    GetTileLayerScale(),
                    GetLayerSortingOrder(sortingOrder) + 1,
                    "Tile_" + layerName + "_" + tile.SourceColumn + "_" + tile.SourceRow);
            }

            return renderedTileCount;
        }

        private static int RenderObjectGroup(
            SpriteRendererPool pool,
            XElement objectGroup,
            IList<TilesetSpriteSet> spriteSets,
            int tileWidth,
            int tileHeight,
            int startColumn,
            int startRow,
            int columns,
            int rows,
            int sortingOrder,
            bool showHiddenObjects,
            string mapId,
            GameState gameState)
        {
            var renderedObjectCount = 0;
            var groupName = GetString(objectGroup, "name");

            foreach (var mapObject in objectGroup.Elements("object"))
            {
                if (IsRuntimeNpc(mapObject))
                {
                    continue;
                }

                if (IsOpenDoor(mapObject, mapId, gameState))
                {
                    continue;
                }

                if (!CanRenderPickupObject(mapObject, mapId, gameState))
                {
                    continue;
                }

                var gid = GetGid(mapObject, "gid");
                if (gid == 0 && !showHiddenObjects)
                {
                    continue;
                }

                Sprite sprite;
                if (gid == 0)
                {
                    sprite = GetHiddenObjectSprite();
                }
                else if (!TryGetObjectSprite(gid, mapObject, spriteSets, mapId, gameState, out sprite))
                {
                    continue;
                }

                List<SpriteAnimationFrame> animationFrames = null;
                if (gid != 0)
                {
                    TryGetObjectAnimation(gid, mapObject, spriteSets, mapId, gameState, out animationFrames);
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
                    animationFrames,
                    GetObjectLocalPosition(
                        x,
                        y,
                        width,
                        height,
                        tileWidth,
                        tileHeight,
                        gid == 0),
                    GetObjectLocalScale(width, height, tileWidth, tileHeight, gid == 0),
                    GetObjectSortingOrder(
                        sortingOrder,
                        GetObjectSortRow(mapObject, y, height, tileHeight, gid == 0)),
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
                .Select(ParseGid)
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

        private static bool TryGetObjectSprite(
            int gid,
            XElement mapObject,
            IList<TilesetSpriteSet> spriteSets,
            string mapId,
            GameState gameState,
            out Sprite sprite)
        {
            sprite = null;
            if (gameState != null &&
                gameState.IsObjectOpen(mapId, GetInt(mapObject, "id")) &&
                string.Equals(GetObjectClass(mapObject), "Chest", StringComparison.OrdinalIgnoreCase))
            {
                var openImage = GetIntProperty(mapObject, "OpenImage", 135);
                if (TilesetSprites.TryGetSpriteFromSameSet(gid, openImage, spriteSets, out sprite))
                {
                    return true;
                }
            }

            return TilesetSprites.TryGetSprite(gid, spriteSets, out sprite);
        }

        private static int GetObjectSortingOrder(int layerSortingOrder, int row)
        {
            return layerSortingOrder * 1000 + row;
        }

        private static int GetLayerSortingOrder(int layerSortingOrder)
        {
            return layerSortingOrder * 1000;
        }

        private static int GetObjectSortRow(XElement mapObject, float y, float height, int tileHeight, bool useObjectBounds)
        {
            if (!useObjectBounds && StartsWith(GetObjectClass(mapObject), "Npc"))
            {
                return Mathf.FloorToInt((y - 0.001f) / tileHeight);
            }

            return useObjectBounds
                ? Mathf.FloorToInt((y + Math.Max(height, tileHeight) - 0.001f) / tileHeight)
                : Mathf.FloorToInt((y - 0.001f) / tileHeight);
        }

        private static bool TryGetObjectAnimation(
            int gid,
            XElement mapObject,
            IList<TilesetSpriteSet> spriteSets,
            string mapId,
            GameState gameState,
            out List<SpriteAnimationFrame> frames)
        {
            frames = null;
            if (gameState != null &&
                gameState.IsObjectOpen(mapId, GetInt(mapObject, "id")) &&
                string.Equals(GetObjectClass(mapObject), "Chest", StringComparison.OrdinalIgnoreCase))
            {
                var openImage = GetIntProperty(mapObject, "OpenImage", 135);
                if (TilesetSprites.TryGetAnimationFromSameSet(gid, openImage, spriteSets, out frames))
                {
                    return true;
                }
            }

            return TilesetSprites.TryGetAnimation(gid, spriteSets, out frames) ||
                   TryGetCharacterIdleAnimation(gid, mapObject, spriteSets, out frames);
        }

        private static bool TryGetCharacterIdleAnimation(
            int gid,
            XElement mapObject,
            IList<TilesetSpriteSet> spriteSets,
            out List<SpriteAnimationFrame> frames)
        {
            frames = null;
            var objectClass = GetObjectClass(mapObject);
            if (!StartsWith(objectClass, "Npc") && !GetBoolProperty(mapObject, "IdleAnimation"))
            {
                return false;
            }

            Direction direction;
            if (TryGetDirectionProperty(mapObject, out direction) &&
                TilesetSprites.TryGetDirectionalAnimation(gid, spriteSets, direction, out frames))
            {
                return true;
            }

            return TilesetSprites.TryGetDirectionalAnimation(gid, spriteSets, out frames);
        }

        private static bool IsRuntimeNpc(XElement mapObject)
        {
            return StartsWith(GetObjectClass(mapObject), "Npc");
        }

        private static bool IsOpenDoor(XElement mapObject, string mapId, GameState gameState)
        {
            return gameState != null &&
                   string.Equals(GetObjectClass(mapObject), "Door", StringComparison.OrdinalIgnoreCase) &&
                   gameState.IsObjectOpen(mapId, GetInt(mapObject, "id"));
        }

        private static bool CanRenderPickupObject(XElement mapObject, string mapId, GameState gameState)
        {
            var objectClass = GetObjectClass(mapObject);
            if (!string.Equals(objectClass, "HiddenItem", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string itemId;
            return gameState == null ||
                   gameState.CanPickupMapObject(
                       mapId,
                       GetInt(mapObject, "id"),
                       objectClass,
                       TryGetStringProperty(mapObject, "ItemId", out itemId) ? itemId : null);
        }

        private static void ApplyAnimation(SpriteRenderer renderer, List<SpriteAnimationFrame> animationFrames)
        {
            if (animationFrames == null || animationFrames.Count <= 1)
            {
                return;
            }

            var player = renderer.gameObject.AddComponent<SpriteAnimationPlayer>();
            player.Configure(renderer, animationFrames);
        }

        private static Vector3 GetObjectLocalPosition(
            float x,
            float y,
            float width,
            float height,
            int tileWidth,
            int tileHeight,
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
                    centerX - 0.5f,
                    -centerY + 0.5f,
                    -0.1f);
            }

            return new Vector3(
                centerX - 0.5f,
                -centerY + 0.5f,
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

        private static Vector3 GetTileLayerScale()
        {
            return new Vector3(TileLayerOverlapScale, TileLayerOverlapScale, 1f);
        }

        private static Vector3 GetLayerTextureLocalPosition(
            int startColumn,
            int startRow,
            int columns,
            int rows)
        {
            return new Vector3(
                startColumn + (columns - 1) / 2f,
                -startRow - (rows - 1) / 2f,
                0f);
        }

        private static void BlitSprite(Texture2D target, Sprite sprite, int column, int row, int visibleRows)
        {
            if (target == null || sprite == null || sprite.texture == null)
            {
                return;
            }

            var rect = sprite.textureRect;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var pixels = sprite.texture.GetPixels(
                Mathf.RoundToInt(rect.x),
                Mathf.RoundToInt(rect.y),
                width,
                height);
            target.SetPixels(
                column * width,
                (visibleRows - row - 1) * height,
                width,
                height,
                pixels);
        }

        private sealed class TileSpriteRenderInfo
        {
            public Sprite Sprite { get; set; }
            public List<SpriteAnimationFrame> AnimationFrames { get; set; }
            public int SourceColumn { get; set; }
            public int SourceRow { get; set; }
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

        private static string GetObjectClass(XElement element)
        {
            return GetString(element, "class") ?? GetString(element, "type");
        }

        private static bool StartsWith(string value, string prefix)
        {
            return value != null && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
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

        private static int GetGid(XElement element, string name)
        {
            return ParseGid(GetString(element, name));
        }

        private static int ParseGid(string value)
        {
            uint result;
            return uint.TryParse(value, out result) ? (int)(result & TiledGidMask) : 0;
        }

        private static int GetIntProperty(XElement element, string propertyName, int defaultValue)
        {
            var properties = element.Element("properties");
            if (properties == null)
            {
                return defaultValue;
            }

            foreach (var property in properties.Elements("property"))
            {
                if (!string.Equals(GetString(property, "name"), propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = GetString(property, "value") ?? property.Value;
                int result;
                return int.TryParse(value, out result) ? result : defaultValue;
            }

            return defaultValue;
        }

        private static bool GetBoolProperty(XElement element, string propertyName)
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
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static bool TryGetDirectionProperty(XElement element, out Direction direction)
        {
            direction = Direction.Down;
            string value;
            if (!TryGetStringProperty(element, "Direction", out value) &&
                !TryGetStringProperty(element, "Facing", out value))
            {
                return false;
            }

            return TryParseDirection(value, out direction);
        }

        private static bool TryParseDirection(string value, out Direction direction)
        {
            direction = Direction.Down;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (string.Equals(value, "North", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Up", StringComparison.OrdinalIgnoreCase))
            {
                direction = Direction.Up;
                return true;
            }

            if (string.Equals(value, "East", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Right", StringComparison.OrdinalIgnoreCase))
            {
                direction = Direction.Right;
                return true;
            }

            if (string.Equals(value, "South", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Down", StringComparison.OrdinalIgnoreCase))
            {
                direction = Direction.Down;
                return true;
            }

            if (string.Equals(value, "West", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Left", StringComparison.OrdinalIgnoreCase))
            {
                direction = Direction.Left;
                return true;
            }

            return false;
        }

        private static bool TryGetStringProperty(XElement element, string propertyName, out string value)
        {
            value = null;
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

                value = GetString(property, "value") ?? property.Value;
                return true;
            }

            return false;
        }

        private static float GetFloat(XElement element, string name)
        {
            var value = GetString(element, name);
            float result;
            return float.TryParse(value, out result) ? result : 0f;
        }
    }
}
