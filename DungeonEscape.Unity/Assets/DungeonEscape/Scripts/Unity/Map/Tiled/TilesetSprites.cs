using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Map.Tiled
{
    public static class TilesetSprites
    {
        private static readonly Dictionary<string, Dictionary<int, Sprite>> SpriteCache = new Dictionary<string, Dictionary<int, Sprite>>();

        public static List<TilesetSpriteSet> LoadSpriteSets(
            IEnumerable<TiledTilesetInfo> tilesets,
            int fallbackTileWidth,
            int fallbackTileHeight)
        {
            var spriteSets = new List<TilesetSpriteSet>();

            foreach (var tileset in tilesets)
            {
                if (string.IsNullOrEmpty(tileset.UnityImagePath))
                {
                    continue;
                }

                var texturePath = ToFullAssetPath(tileset.UnityImagePath);
                if (!File.Exists(texturePath))
                {
                    continue;
                }

                var tileWidth = tileset.Document.TileWidth == 0 ? fallbackTileWidth : tileset.Document.TileWidth;
                var tileHeight = tileset.Document.TileHeight == 0 ? fallbackTileHeight : tileset.Document.TileHeight;
                var cacheKey = tileset.UnityImagePath + "|" +
                               tileWidth + "|" +
                               tileHeight + "|" +
                               tileset.Document.Columns + "|" +
                               tileset.Document.TileCount + "|" +
                               tileset.Document.Spacing + "|" +
                               tileset.Document.Margin;

                Dictionary<int, Sprite> sprites;
                if (!SpriteCache.TryGetValue(cacheKey, out sprites))
                {
                    var texture = LoadTexture(texturePath);
                    sprites = SliceTexture(
                        texture,
                        tileWidth,
                        tileHeight,
                        tileset.Document.Columns,
                        tileset.Document.TileCount,
                        tileset.Document.Spacing,
                        tileset.Document.Margin);
                    SpriteCache[cacheKey] = sprites;
                }

                spriteSets.Add(new TilesetSpriteSet
                {
                    FirstGid = tileset.FirstGid,
                    Name = tileset.Document.Name,
                    Sprites = sprites,
                    Animations = BuildAnimations(tileset.Document, sprites),
                    DirectionalFrameCount = GetDirectionalFrameCount(tileset.Document),
                    DirectionOrder = GetDirectionOrder(tileset.Document),
                    DirectionalFrameSetOffset = GetDirectionalFrameSetOffset(tileset.Document),
                    DirectionalFrameSetWidth = GetDirectionalFrameSetWidth(tileset.Document),
                    DirectionalFrameSetSpacing = GetDirectionalFrameSetSpacing(tileset.Document),
                    Columns = tileset.Document.Columns
                });
            }

            return spriteSets.OrderBy(set => set.FirstGid).ToList();
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
        }

        public static bool TryGetSprite(int gid, IList<TilesetSpriteSet> spriteSets, out Sprite sprite)
        {
            sprite = null;

            var selected = GetSpriteSet(gid, spriteSets);

            if (selected == null)
            {
                return false;
            }

            return selected.Sprites.TryGetValue(gid - selected.FirstGid, out sprite);
        }

        public static bool TryGetAnimation(int gid, IList<TilesetSpriteSet> spriteSets, out List<SpriteAnimationFrame> frames)
        {
            frames = null;

            var selected = GetSpriteSet(gid, spriteSets);

            return selected != null &&
                   selected.Animations != null &&
                   selected.Animations.TryGetValue(gid - selected.FirstGid, out frames);
        }

        public static bool TryGetSpriteFromSameSet(int referenceGid, int tileId, IList<TilesetSpriteSet> spriteSets, out Sprite sprite)
        {
            sprite = null;

            var selected = GetSpriteSet(referenceGid, spriteSets);

            return selected != null && selected.Sprites.TryGetValue(tileId, out sprite);
        }

        public static bool TryGetLocalTileId(int gid, IList<TilesetSpriteSet> spriteSets, out int tileId)
        {
            tileId = 0;

            var selected = GetSpriteSet(gid, spriteSets);

            if (selected == null)
            {
                return false;
            }

            tileId = gid - selected.FirstGid;
            return true;
        }

        public static bool TryGetDirectionalAnimation(
            int gid,
            IList<TilesetSpriteSet> spriteSets,
            out List<SpriteAnimationFrame> frames)
        {
            frames = null;
            var selected = GetSpriteSet(gid, spriteSets);
            if (selected == null || selected.DirectionalFrameCount <= 1)
            {
                return false;
            }

            var localTileId = gid - selected.FirstGid;
            var frameStart = GetImplicitDirectionFrameStart(selected, localTileId);
            return TryBuildDirectionalAnimation(selected, frameStart, out frames);
        }

        public static bool TryGetDirectionalAnimation(
            int gid,
            IList<TilesetSpriteSet> spriteSets,
            Direction direction,
            out List<SpriteAnimationFrame> frames)
        {
            frames = null;
            var selected = GetSpriteSet(gid, spriteSets);
            if (selected == null ||
                selected.DirectionalFrameCount <= 1 ||
                string.IsNullOrEmpty(selected.DirectionOrder))
            {
                return false;
            }

            var directionIndex = selected.DirectionOrder.IndexOf(GetDirectionCode(direction));
            if (directionIndex < 0)
            {
                return false;
            }

            var localTileId = gid - selected.FirstGid;
            var frameStart = GetDirectionalFrameStart(selected, localTileId, directionIndex);
            return TryBuildDirectionalAnimation(selected, frameStart, out frames);
        }

        private static int GetDirectionalFrameStart(TilesetSpriteSet selected, int localTileId, int directionIndex)
        {
            var directionOrder = selected.DirectionOrder;
            if (selected.Columns > 0 && selected.DirectionalFrameCount != 3)
            {
                var normalizedTileId = Math.Max(0, localTileId - selected.DirectionalFrameSetOffset);
                var column = normalizedTileId % selected.Columns;
                var row = normalizedTileId / selected.Columns;
                return row * selected.Columns +
                       GetFrameSetColumnStart(selected, column) +
                       directionIndex * selected.DirectionalFrameCount;
            }

            if (selected.DirectionalFrameCount == 3 &&
                directionOrder != null &&
                directionOrder.Length == 4 &&
                selected.Columns > 0)
            {
                var normalizedTileId = Math.Max(0, localTileId - selected.DirectionalFrameSetOffset);
                var column = normalizedTileId % selected.Columns;
                var row = normalizedTileId / selected.Columns;
                var blockColumn = column / selected.DirectionalFrameCount;
                var blockRow = row / directionOrder.Length;
                return blockRow * selected.Columns * directionOrder.Length +
                       directionIndex * selected.Columns +
                       blockColumn * selected.DirectionalFrameCount;
            }

            var characterStart = localTileId - selected.DirectionalFrameSetOffset;
            return characterStart + directionIndex * selected.DirectionalFrameCount;
        }

        private static bool TryBuildDirectionalAnimation(
            TilesetSpriteSet selected,
            int frameStart,
            out List<SpriteAnimationFrame> frames)
        {
            frames = new List<SpriteAnimationFrame>();
            for (var i = 0; i < selected.DirectionalFrameCount; i++)
            {
                Sprite sprite;
                if (!selected.Sprites.TryGetValue(frameStart + i, out sprite))
                {
                    frames.Clear();
                    return false;
                }

                frames.Add(new SpriteAnimationFrame
                {
                    Sprite = sprite,
                    DurationSeconds = selected.DirectionalFrameCount == 3 ? 0.22f : 0.4f
                });
            }

            return frames.Count > 1;
        }

        private static char GetDirectionCode(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return 'N';
                case Direction.Right:
                    return 'E';
                case Direction.Down:
                    return 'S';
                case Direction.Left:
                    return 'W';
                default:
                    return 'S';
            }
        }

        private static int GetImplicitDirectionFrameStart(TilesetSpriteSet selected, int localTileId)
        {
            if (selected.Columns > 0)
            {
                var column = localTileId % selected.Columns;
                var row = localTileId / selected.Columns;
                var frameSetColumnStart = GetFrameSetColumnStart(selected, column);
                var columnInSet = column - frameSetColumnStart;
                return row * selected.Columns +
                       frameSetColumnStart +
                       columnInSet -
                       columnInSet % selected.DirectionalFrameCount;
            }

            return localTileId - localTileId % selected.DirectionalFrameCount;
        }

        private static int GetFrameSetColumnStart(TilesetSpriteSet selected, int column)
        {
            if (selected.DirectionalFrameSetWidth <= 0)
            {
                return column - column % selected.DirectionalFrameCount;
            }

            var stride = selected.DirectionalFrameSetWidth + Math.Max(0, selected.DirectionalFrameSetSpacing);
            if (stride <= 0)
            {
                return 0;
            }

            var start = column / stride * stride;
            return start;
        }

        public static bool TryGetAnimationFromSameSet(
            int referenceGid,
            int tileId,
            IList<TilesetSpriteSet> spriteSets,
            out List<SpriteAnimationFrame> frames)
        {
            frames = null;

            TilesetSpriteSet selected = null;
            foreach (var spriteSet in spriteSets)
            {
                if (spriteSet.FirstGid <= referenceGid)
                {
                    selected = spriteSet;
                }
                else
                {
                    break;
                }
            }

            return selected != null &&
                   selected.Animations != null &&
                   selected.Animations.TryGetValue(tileId, out frames);
        }

        private static Dictionary<int, List<SpriteAnimationFrame>> BuildAnimations(
            TiledTilesetDocumentInfo document,
            Dictionary<int, Sprite> sprites)
        {
            var result = new Dictionary<int, List<SpriteAnimationFrame>>();
            if (document == null || document.Animations == null)
            {
                return result;
            }

            foreach (var animation in document.Animations)
            {
                var frames = new List<SpriteAnimationFrame>();
                foreach (var frame in animation.Value)
                {
                    Sprite sprite;
                    if (!sprites.TryGetValue(frame.TileId, out sprite))
                    {
                        continue;
                    }

                    frames.Add(new SpriteAnimationFrame
                    {
                        Sprite = sprite,
                        DurationSeconds = Mathf.Max(frame.Duration / 1000f, 0.01f)
                    });
                }

                if (frames.Count > 0)
                {
                    result[animation.Key] = frames;
                }
            }

            return result;
        }

        private static int GetDirectionalFrameCount(TiledTilesetDocumentInfo document)
        {
            var propertyValue = GetProperty(document, "DirectionalFrameCount");
            int frameCount;
            if (int.TryParse(propertyValue, out frameCount))
            {
                return frameCount;
            }

            if (string.Equals(document.Name, "npc", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(document.Name, "ship", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(document.Name, "hero", System.StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(document.Name, "animals", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(document.Name, "cart", System.StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            return 0;
        }

        private static string GetDirectionOrder(TiledTilesetDocumentInfo document)
        {
            var propertyValue = GetProperty(document, "DirectionOrder");
            if (!string.IsNullOrEmpty(propertyValue))
            {
                return propertyValue.ToUpperInvariant();
            }

            if (string.Equals(document.Name, "npc", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(document.Name, "animals", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(document.Name, "cart", System.StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(document.Name, "npc", System.StringComparison.OrdinalIgnoreCase) ? "SEWN" : "NESW";
            }

            if (string.Equals(document.Name, "ship", System.StringComparison.OrdinalIgnoreCase))
            {
                return "SENW";
            }

            if (string.Equals(document.Name, "hero", System.StringComparison.OrdinalIgnoreCase))
            {
                return "NESW";
            }

            return null;
        }

        private static int GetDirectionalFrameSetOffset(TiledTilesetDocumentInfo document)
        {
            var propertyValue = GetProperty(document, "DirectionalFrameSetOffset");
            int offset;
            if (int.TryParse(propertyValue, out offset))
            {
                return Math.Max(0, offset);
            }

            return string.Equals(document.Name, "hero", System.StringComparison.OrdinalIgnoreCase) ? 4 : 0;
        }

        private static int GetDirectionalFrameSetWidth(TiledTilesetDocumentInfo document)
        {
            var propertyValue = GetProperty(document, "DirectionalFrameSetWidth");
            int width;
            if (int.TryParse(propertyValue, out width))
            {
                return Math.Max(0, width);
            }

            return GetDirectionalFrameCount(document) * (GetDirectionOrder(document) == null ? 0 : GetDirectionOrder(document).Length);
        }

        private static int GetDirectionalFrameSetSpacing(TiledTilesetDocumentInfo document)
        {
            var propertyValue = GetProperty(document, "DirectionalFrameSetSpacing");
            int spacing;
            return int.TryParse(propertyValue, out spacing) ? Math.Max(0, spacing) : 0;
        }

        private static string GetProperty(TiledTilesetDocumentInfo document, string name)
        {
            string value;
            return document != null &&
                   document.Properties != null &&
                   document.Properties.TryGetValue(name, out value)
                ? value
                : null;
        }

        private static TilesetSpriteSet GetSpriteSet(int gid, IList<TilesetSpriteSet> spriteSets)
        {
            TilesetSpriteSet selected = null;
            foreach (var spriteSet in spriteSets)
            {
                if (spriteSet.FirstGid <= gid)
                {
                    selected = spriteSet;
                }
                else
                {
                    break;
                }
            }

            return selected;
        }

        private static Texture2D LoadTexture(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.LoadImage(bytes);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, false);
            return texture;
        }

        private static Dictionary<int, Sprite> SliceTexture(
            Texture2D texture,
            int tileWidth,
            int tileHeight,
            int tilesetColumns,
            int tileCount,
            int spacing,
            int margin)
        {
            var sprites = new Dictionary<int, Sprite>();
            var columns = tilesetColumns > 0
                ? tilesetColumns
                : (texture.width - margin + spacing) / (tileWidth + spacing);
            var rows = tileCount > 0
                ? (tileCount + columns - 1) / columns
                : (texture.height - margin + spacing) / (tileHeight + spacing);

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var tileId = row * columns + column;
                    if (tileCount > 0 && tileId >= tileCount)
                    {
                        break;
                    }

                    var sourceX = margin + column * (tileWidth + spacing);
                    var sourceY = margin + row * (tileHeight + spacing);
                    var rect = new Rect(
                        sourceX,
                        texture.height - sourceY - tileHeight,
                        tileWidth,
                        tileHeight);

                    sprites[tileId] = Sprite.Create(
                        texture,
                        rect,
                        new Vector2(0.5f, 0.5f),
                        tileWidth,
                        1,
                        SpriteMeshType.FullRect);
                }
            }

            return sprites;
        }

        private static string ToFullAssetPath(string assetPath)
        {
            return UnityAssetPath.ToRuntimePath(assetPath);
        }
    }

    public sealed class TilesetSpriteSet
    {
        public int FirstGid { get; set; }
        public string Name { get; set; }
        public Dictionary<int, Sprite> Sprites { get; set; }
        public Dictionary<int, List<SpriteAnimationFrame>> Animations { get; set; }
        public int DirectionalFrameCount { get; set; }
        public string DirectionOrder { get; set; }
        public int DirectionalFrameSetOffset { get; set; }
        public int DirectionalFrameSetWidth { get; set; }
        public int DirectionalFrameSetSpacing { get; set; }
        public int Columns { get; set; }
    }
}
