using System.Collections.Generic;
using System.IO;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.Rendering
{
    public static class HeroSpriteResolver
    {
        public const string HeroTextureAssetPath = "Assets/DungeonEscape/Images/sprites/hero.png";
        public const int HeroWidth = 32;
        public const int HeroHeight = 48;

        private static readonly Dictionary<string, DirectionalSpriteSet> SpriteSetCache = new Dictionary<string, DirectionalSpriteSet>();
        private static readonly Dictionary<string, IList<TilesetSpriteSet>> TilesetCache = new Dictionary<string, IList<TilesetSpriteSet>>();

        public static int GetDefaultFrameIndex(Class heroClass, Gender gender)
        {
            return DirectionalSpriteSheet.GetHeroBaseFrameIndex(heroClass, gender);
        }

        public static int GetHeroCharacterCount()
        {
            var path = UnityAssetPath.ToRuntimePath(HeroTextureAssetPath);
            if (!File.Exists(path))
            {
                return 1;
            }

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(bytes))
            {
                return 1;
            }

            var frameCount = (texture.width / HeroWidth) * (texture.height / HeroHeight);
            return Mathf.Max(1, frameCount / DirectionalSpriteSheet.CharacterFrameCount);
        }

        public static int GetBaseFrameIndexForCharacter(int characterIndex)
        {
            return Mathf.Max(0, characterIndex) * DirectionalSpriteSheet.CharacterFrameCount;
        }

        public static bool TryGetIdleSprite(Hero hero, Sprite fallback, out Sprite sprite)
        {
            sprite = null;
            var set = GetSpriteSet(hero, fallback);
            if (set == null)
            {
                return false;
            }

            sprite = set.GetIdle(Direction.Down);
            return sprite != null;
        }

        public static DirectionalSpriteSet GetSpriteSet(Hero hero, Sprite fallback)
        {
            if (hero == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(hero.SpriteTilesetPath) && hero.SpriteTileId.HasValue)
            {
                return GetTilesetSpriteSet(hero.SpriteTilesetPath, hero.SpriteTileId.Value, fallback);
            }

            var frameIndex = hero.SpriteFrameIndex ?? GetDefaultFrameIndex(hero.Class, hero.Gender);
            return GetHeroSpriteSet(frameIndex, fallback);
        }

        public static DirectionalSpriteSet GetHeroSpriteSet(int baseFrameIndex, Sprite fallback)
        {
            var cacheKey = "hero:" + baseFrameIndex;
            DirectionalSpriteSet spriteSet;
            if (SpriteSetCache.TryGetValue(cacheKey, out spriteSet))
            {
                return spriteSet;
            }

            spriteSet = DirectionalSpriteSheet.LoadCharacterSet(
                HeroTextureAssetPath,
                HeroWidth,
                HeroHeight,
                baseFrameIndex,
                fallback);
            SpriteSetCache[cacheKey] = spriteSet;
            return spriteSet;
        }

        private static DirectionalSpriteSet GetTilesetSpriteSet(string tilesetAssetPath, int tileId, Sprite fallback)
        {
            var cacheKey = "tileset:" + tilesetAssetPath + ":" + tileId;
            DirectionalSpriteSet spriteSet;
            if (SpriteSetCache.TryGetValue(cacheKey, out spriteSet))
            {
                return spriteSet;
            }

            var spriteSets = GetTilesetSpriteSets(tilesetAssetPath);
            if (spriteSets == null || spriteSets.Count == 0)
            {
                return DirectionalSpriteSheet.CreateFallbackSet(fallback);
            }

            var sprites = new Dictionary<Direction, Sprite[]>();
            AddTilesetDirection(sprites, spriteSets, tileId, Direction.Up);
            AddTilesetDirection(sprites, spriteSets, tileId, Direction.Right);
            AddTilesetDirection(sprites, spriteSets, tileId, Direction.Down);
            AddTilesetDirection(sprites, spriteSets, tileId, Direction.Left);

            Sprite idleSprite;
            if (sprites.Count == 0 && TilesetSprites.TryGetSprite(tileId, spriteSets, out idleSprite))
            {
                sprites[Direction.Up] = new[] { idleSprite, idleSprite };
                sprites[Direction.Right] = new[] { idleSprite, idleSprite };
                sprites[Direction.Down] = new[] { idleSprite, idleSprite };
                sprites[Direction.Left] = new[] { idleSprite, idleSprite };
            }

            spriteSet = sprites.Count == 0
                ? DirectionalSpriteSheet.CreateFallbackSet(fallback)
                : new DirectionalSpriteSet(sprites, fallback);
            SpriteSetCache[cacheKey] = spriteSet;
            return spriteSet;
        }

        private static void AddTilesetDirection(
            IDictionary<Direction, Sprite[]> sprites,
            IList<TilesetSpriteSet> spriteSets,
            int tileId,
            Direction direction)
        {
            List<SpriteAnimationFrame> frames;
            if (!TilesetSprites.TryGetDirectionalAnimation(tileId, spriteSets, direction, out frames) &&
                !TilesetSprites.TryGetDirectionalAnimation(tileId, spriteSets, out frames))
            {
                return;
            }

            var selected = new List<Sprite>();
            foreach (var frame in frames)
            {
                if (frame != null && frame.Sprite != null)
                {
                    selected.Add(frame.Sprite);
                }
            }

            if (selected.Count > 0)
            {
                sprites[direction] = selected.ToArray();
            }
        }

        private static IList<TilesetSpriteSet> GetTilesetSpriteSets(string tilesetAssetPath)
        {
            IList<TilesetSpriteSet> spriteSets;
            if (TilesetCache.TryGetValue(tilesetAssetPath, out spriteSets))
            {
                return spriteSets;
            }

            var fullPath = UnityAssetPath.ToRuntimePath(tilesetAssetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Hero sprite tileset not found: " + tilesetAssetPath);
                spriteSets = new List<TilesetSpriteSet>();
                TilesetCache[tilesetAssetPath] = spriteSets;
                return spriteSets;
            }

            var document = TiledTilesetDocumentInfo.Parse(File.ReadAllText(fullPath));
            var imageAssetPath = ResolveTilesetImageAssetPath(document.ImageSource);
            if (string.IsNullOrEmpty(imageAssetPath))
            {
                spriteSets = new List<TilesetSpriteSet>();
                TilesetCache[tilesetAssetPath] = spriteSets;
                return spriteSets;
            }

            var imagePath = UnityAssetPath.ToRuntimePath(imageAssetPath);
            if (!File.Exists(imagePath))
            {
                Debug.LogWarning("Hero sprite tileset image not found: " + imageAssetPath);
                spriteSets = new List<TilesetSpriteSet>();
                TilesetCache[tilesetAssetPath] = spriteSets;
                return spriteSets;
            }

            var texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.LoadImage(File.ReadAllBytes(imagePath));

            spriteSets = new List<TilesetSpriteSet>
            {
                new TilesetSpriteSet
                {
                    FirstGid = 0,
                    Name = document.Name,
                    Sprites = SlicePartyCharacterSprites(texture, document),
                    DirectionalFrameCount = GetIntProperty(document, "DirectionalFrameCount", 0),
                    DirectionOrder = GetStringProperty(document, "DirectionOrder", null),
                    DirectionalFrameSetOffset = GetIntProperty(document, "DirectionalFrameSetOffset", 0),
                    DirectionalFrameSetWidth = GetIntProperty(document, "DirectionalFrameSetWidth", 0),
                    DirectionalFrameSetSpacing = GetIntProperty(document, "DirectionalFrameSetSpacing", 0),
                    Columns = document.Columns
                }
            };
            TilesetCache[tilesetAssetPath] = spriteSets;
            return spriteSets;
        }

        private static Dictionary<int, Sprite> SlicePartyCharacterSprites(Texture2D texture, TiledTilesetDocumentInfo document)
        {
            var sprites = new Dictionary<int, Sprite>();
            if (texture == null || document == null || document.TileWidth <= 0 || document.TileHeight <= 0)
            {
                return sprites;
            }

            var columns = document.Columns > 0
                ? document.Columns
                : (texture.width - document.Margin + document.Spacing) / (document.TileWidth + document.Spacing);
            if (columns <= 0)
            {
                return sprites;
            }

            var rows = document.TileCount > 0
                ? (document.TileCount + columns - 1) / columns
                : (texture.height - document.Margin + document.Spacing) / (document.TileHeight + document.Spacing);

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var tileId = row * columns + column;
                    if (document.TileCount > 0 && tileId >= document.TileCount)
                    {
                        break;
                    }

                    var sourceX = document.Margin + column * (document.TileWidth + document.Spacing);
                    var sourceY = document.Margin + row * (document.TileHeight + document.Spacing);
                    var rect = new Rect(
                        sourceX,
                        texture.height - sourceY - document.TileHeight,
                        document.TileWidth,
                        document.TileHeight);

                    sprites[tileId] = Sprite.Create(
                        texture,
                        rect,
                        new Vector2(0.5f, 0.33f),
                        document.TileWidth,
                        1,
                        SpriteMeshType.FullRect);
                }
            }

            return sprites;
        }

        private static int GetIntProperty(TiledTilesetDocumentInfo document, string name, int defaultValue)
        {
            string value;
            int result;
            return document.Properties != null &&
                   document.Properties.TryGetValue(name, out value) &&
                   int.TryParse(value, out result)
                ? result
                : defaultValue;
        }

        private static string GetStringProperty(TiledTilesetDocumentInfo document, string name, string defaultValue)
        {
            string value;
            return document.Properties != null && document.Properties.TryGetValue(name, out value)
                ? value
                : defaultValue;
        }

        private static string ResolveTilesetImageAssetPath(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            var normalized = source.Replace('\\', '/');
            while (normalized.StartsWith("../", System.StringComparison.Ordinal))
            {
                normalized = normalized.Substring(3);
            }

            return "Assets/DungeonEscape/" + normalized;
        }
    }
}
