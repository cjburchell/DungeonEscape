using System.Collections.Generic;
using System.IO;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class TiledTilesetSprites
    {
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
                var texture = LoadTexture(texturePath);
                spriteSets.Add(new TiledTilesetSpriteSet
                {
                    FirstGid = tileset.FirstGid,
                    Sprites = SliceTexture(texture, tileWidth, tileHeight)
                });
            }

            return spriteSets.OrderBy(set => set.FirstGid).ToList();
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

        private static Dictionary<int, Sprite> SliceTexture(Texture2D texture, int tileWidth, int tileHeight)
        {
            var sprites = new Dictionary<int, Sprite>();
            var columns = texture.width / tileWidth;
            var rows = texture.height / tileHeight;

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var tileId = row * columns + column;
                    var rect = new Rect(
                        column * tileWidth,
                        texture.height - ((row + 1) * tileHeight),
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
