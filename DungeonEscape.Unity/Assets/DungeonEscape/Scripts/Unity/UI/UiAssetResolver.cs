using System.Collections.Generic;
using System.IO;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public static class UiAssetResolver
    {
        private const string ItemsTilesetAssetPath = "Assets/DungeonEscape/Tilesets/items2.tsx";
        private const string SpellsTilesetAssetPath = "Assets/DungeonEscape/Tilesets/items.tsx";
        private static IList<TilesetSpriteSet> itemSpriteSets;
        private static IList<TilesetSpriteSet> spellSpriteSets;
        private static readonly Dictionary<string, Sprite> HeroSprites = new Dictionary<string, Sprite>();

        public static void Preload(Party party)
        {
            EnsureItemSpriteSets();
            if (party == null || party.Members == null)
            {
                return;
            }

            foreach (var hero in party.Members)
            {
                Sprite heroSprite;
                TryGetHeroSprite(hero, out heroSprite);

                if (hero.Items == null)
                {
                    continue;
                }

                foreach (var item in hero.Items)
                {
                    Sprite itemSprite;
                    TryGetItemSprite(item, out itemSprite);
                }
            }
        }

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
            return itemSpriteSets != null && TilesetSprites.TryGetSprite(item.ImageId, itemSpriteSets, out sprite);
        }

        public static bool TryGetSpellSprite(Spell spell, out Sprite sprite)
        {
            sprite = null;
            if (spell == null || spell.ImageId < 0)
            {
                return false;
            }

            EnsureSpellSpriteSets();
            return spellSpriteSets != null && TilesetSprites.TryGetSprite(spell.ImageId, spellSpriteSets, out sprite);
        }

        public static bool TryGetHeroSprite(Hero hero, out Sprite sprite)
        {
            sprite = null;
            if (hero == null)
            {
                return false;
            }

            var key = GetHeroSpriteKey(hero);
            if (HeroSprites.TryGetValue(key, out sprite))
            {
                return sprite != null;
            }

            if (!HeroSpriteResolver.TryGetIdleSprite(hero, CreateFallbackSprite(), out sprite))
            {
                return false;
            }

            HeroSprites[key] = sprite;
            return sprite != null;
        }

        public static bool TryGetHeroSprite(Class heroClass, Gender gender, out Sprite sprite)
        {
            return TryGetHeroSprite(HeroSpriteResolver.GetDefaultFrameIndex(heroClass, gender), out sprite);
        }

        public static bool TryGetHeroSprite(int baseFrameIndex, out Sprite sprite)
        {
            var key = "hero:" + baseFrameIndex;
            if (HeroSprites.TryGetValue(key, out sprite))
            {
                return sprite != null;
            }

            var set = HeroSpriteResolver.GetHeroSpriteSet(baseFrameIndex, CreateFallbackSprite());
            sprite = set.GetIdle(Direction.Down);
            HeroSprites[key] = sprite;
            return sprite != null;
        }

        private static string GetHeroSpriteKey(Hero hero)
        {
            if (hero == null)
            {
                return "hero:null";
            }

            if (!string.IsNullOrEmpty(hero.SpriteTilesetPath) && hero.SpriteTileId.HasValue)
            {
                return "tileset:" + hero.SpriteTilesetPath + ":" + hero.SpriteTileId.Value;
            }

            return "hero:" + (hero.SpriteFrameIndex ?? HeroSpriteResolver.GetDefaultFrameIndex(hero.Class, hero.Gender));
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
                itemSpriteSets = new List<TilesetSpriteSet>();
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

            itemSpriteSets = TilesetSprites.LoadSpriteSets(
                new[] { tileset },
                document.TileWidth,
                document.TileHeight);
        }

        private static void EnsureSpellSpriteSets()
        {
            if (spellSpriteSets != null)
            {
                return;
            }

            var tilesetPath = ToFullAssetPath(SpellsTilesetAssetPath);
            if (!File.Exists(tilesetPath))
            {
                Debug.LogWarning("Spell tileset not found: " + SpellsTilesetAssetPath);
                spellSpriteSets = new List<TilesetSpriteSet>();
                return;
            }

            var document = TiledTilesetDocumentInfo.Parse(File.ReadAllText(tilesetPath));
            var tileset = new TiledTilesetInfo
            {
                FirstGid = 0,
                Name = document.Name,
                Source = SpellsTilesetAssetPath,
                Document = document,
                UnityTilesetPath = SpellsTilesetAssetPath,
                UnityImagePath = ResolveTilesetImageAssetPath(document.ImageSource)
            };

            spellSpriteSets = TilesetSprites.LoadSpriteSets(
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
            return UnityAssetPath.ToRuntimePath(assetPath);
        }
    }
}
