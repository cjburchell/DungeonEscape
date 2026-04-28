using System.Collections.Generic;
using System.IO;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class DungeonEscapeUiAssetResolver
    {
        private const string ItemsTilesetAssetPath = "Assets/DungeonEscape/Tilesets/items2.tsx";
        private const string HeroTextureAssetPath = "Assets/DungeonEscape/Images/sprites/hero.png";
        private const int HeroWidth = 32;
        private const int HeroHeight = 48;

        private static IList<TiledTilesetSpriteSet> itemSpriteSets;
        private static readonly Dictionary<string, Sprite> HeroSprites = new Dictionary<string, Sprite>();

        public static bool TryGetItemSprite(ItemInstance item, out Sprite sprite)
        {
            sprite = null;
            return item != null && TryGetItemSprite(item.Item, out sprite);
        }

        public static bool TryGetItemSprite(Item item, out Sprite sprite)
        {
            sprite = null;
            if (item == null || item.ImageId < 0)
            {
                return false;
            }

            EnsureItemSpriteSets();
            return itemSpriteSets != null && TiledTilesetSprites.TryGetSprite(item.ImageId, itemSpriteSets, out sprite);
        }

        public static bool TryGetHeroSprite(Hero hero, out Sprite sprite)
        {
            sprite = null;
            if (hero == null)
            {
                return false;
            }

            return TryGetHeroSprite(hero.Class, hero.Gender, out sprite);
        }

        public static bool TryGetHeroSprite(Class heroClass, Gender gender, out Sprite sprite)
        {
            var key = heroClass + "|" + gender;
            if (HeroSprites.TryGetValue(key, out sprite))
            {
                return sprite != null;
            }

            var set = DirectionalSpriteSheet.LoadCharacterSet(
                HeroTextureAssetPath,
                HeroWidth,
                HeroHeight,
                DirectionalSpriteSheet.GetHeroBaseFrameIndex(heroClass, gender),
                CreateFallbackSprite());
            sprite = set.GetIdle(Direction.Down);
            HeroSprites[key] = sprite;
            return sprite != null;
        }

        private static void EnsureItemSpriteSets()
        {
            if (itemSpriteSets != null)
            {
                return;
            }

            var tilesetPath = ToFullAssetPath(ItemsTilesetAssetPath);
            if (!File.Exists(tilesetPath))
            {
                Debug.LogWarning("Item tileset not found: " + ItemsTilesetAssetPath);
                itemSpriteSets = new List<TiledTilesetSpriteSet>();
                return;
            }

            var document = TiledTilesetDocumentInfo.Parse(File.ReadAllText(tilesetPath));
            var tileset = new TiledTilesetInfo
            {
                FirstGid = 0,
                Name = document.Name,
                Source = ItemsTilesetAssetPath,
                Document = document,
                UnityTilesetPath = ItemsTilesetAssetPath,
                UnityImagePath = ResolveTilesetImageAssetPath(document.ImageSource)
            };

            itemSpriteSets = TiledTilesetSprites.LoadSpriteSets(
                new[] { tileset },
                document.TileWidth,
                document.TileHeight);
        }

        private static string ResolveTilesetImageAssetPath(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            var normalized = source.Replace('\\', '/');
            const string imagesSegment = "/Images/";
            var imagesIndex = normalized.IndexOf(imagesSegment, System.StringComparison.OrdinalIgnoreCase);
            if (imagesIndex >= 0)
            {
                normalized = normalized.Substring(imagesIndex + imagesSegment.Length);
            }

            const string imagesPrefix = "Images/";
            if (normalized.StartsWith(imagesPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(imagesPrefix.Length);
            }

            while (normalized.StartsWith("../", System.StringComparison.Ordinal))
            {
                normalized = normalized.Substring(3);
            }

            return "Assets/DungeonEscape/Images/" + normalized;
        }

        private static Sprite CreateFallbackSprite()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.cyan);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static string ToFullAssetPath(string assetPath)
        {
            return Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
        }
    }
}
