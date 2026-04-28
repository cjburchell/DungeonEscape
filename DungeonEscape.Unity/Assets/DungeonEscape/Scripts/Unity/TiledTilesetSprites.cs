using System.Collections.Generic;
using System.IO;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class TiledTilesetSprites
    {
        private static readonly Dictionary<string, Dictionary<int, Sprite>> SpriteCache = new Dictionary<string, Dictionary<int, Sprite>>();

        public static List<TiledTilesetSpriteSet> LoadSpriteSets(
            IEnumerable<TiledTilesetInfo> tilesets,
            int fallbackTileWidth,
            int fallbackTileHeight)
        {
            var spriteSets = new List<TiledTilesetSpriteSet>();

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

                spriteSets.Add(new TiledTilesetSpriteSet
                {
                    FirstGid = tileset.FirstGid,
                    Sprites = sprites
                });
            }

            return spriteSets.OrderBy(set => set.FirstGid).ToList();
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
        }

        public static bool TryGetSprite(int gid, IList<TiledTilesetSpriteSet> spriteSets, out Sprite sprite)
        {
            sprite = null;

            TiledTilesetSpriteSet selected = null;
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

            if (selected == null)
            {
                return false;
            }

            return selected.Sprites.TryGetValue(gid - selected.FirstGid, out sprite);
        }

        private static Texture2D LoadTexture(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.LoadImage(bytes);
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

                    sprites[tileId] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), tileWidth);
                }
            }

            return sprites;
        }

        private static string ToFullAssetPath(string assetPath)
        {
            return Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
        }
    }

    public sealed class TiledTilesetSpriteSet
    {
        public int FirstGid { get; set; }
        public Dictionary<int, Sprite> Sprites { get; set; }
    }
}
