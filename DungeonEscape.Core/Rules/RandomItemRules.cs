using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class RandomItemRules
    {
        public static Item CreateRandomItem(
            int maxLevel,
            int minLevel,
            Rarity? rarity,
            IEnumerable<Item> customItems,
            IEnumerable<Skill> skills,
            Func<double> nextDouble,
            Func<int, int> nextInt,
            Func<string> newId)
        {
            maxLevel = Math.Max(maxLevel, 1);
            minLevel = Math.Max(minLevel, 1);

            var staticMagicItems = (customItems ?? new List<Item>())
                .Where(item => item != null &&
                               item.IsMagicItem &&
                               !item.IsKey &&
                               item.Type != ItemType.Gold &&
                               item.Type != ItemType.Quest &&
                               item.Type != ItemType.Unknown &&
                               item.MinLevel <= maxLevel)
                .ToList();

            if (staticMagicItems.Count > 0 && (nextDouble == null || Chance(0.75d, nextDouble)))
            {
                return staticMagicItems[Next(nextInt, staticMagicItems.Count)];
            }

            var staticConsumables = (customItems ?? new List<Item>())
                .Where(item => item != null &&
                               (item.Type == ItemType.OneUse || item.Type == ItemType.RepeatableUse) &&
                               !item.IsKey &&
                               item.MinLevel <= maxLevel)
                .ToList();

            if (Chance(0.50d, nextDouble) && staticConsumables.Count > 0)
            {
                return staticConsumables[Next(nextInt, staticConsumables.Count)];
            }

            var staticEquipment = (customItems ?? new List<Item>())
                .Where(item => item != null &&
                               !item.IsMagicItem &&
                               !item.IsKey &&
                               item.Type != ItemType.Gold &&
                               item.Type != ItemType.Quest &&
                               item.Type != ItemType.Unknown &&
                               item.MinLevel <= maxLevel &&
                               item.MinLevel >= minLevel)
                .OrderBy(item => item.MinLevel)
                .ThenBy(item => item.Cost)
                .ToList();

            if (staticEquipment.Count > 0)
            {
                return staticEquipment[Next(nextInt, staticEquipment.Count)];
            }

            return CreateGold(0);
        }

        public static Rarity SelectRarity(int roll)
        {
            return roll > 75
                ? roll > 90 ? roll > 98 ? Rarity.Epic : Rarity.Rare : Rarity.Uncommon
                : Rarity.Common;
        }

        private static Item CreateGold(int amount)
        {
            return new Item
            {
                Name = amount + " gold",
                Cost = amount,
                Type = ItemType.Gold
            };
        }

        private static bool Chance(double probability, Func<double> nextDouble)
        {
            return (nextDouble == null ? 0d : nextDouble()) < probability;
        }

        private static int Next(Func<int, int> nextInt, int maxValue)
        {
            if (maxValue <= 1 || nextInt == null)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(maxValue - 1, nextInt(maxValue)));
        }
    }
}
