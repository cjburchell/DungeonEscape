using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class RandomItemRules
    {
        public static Item CreateChestItem(
            int level,
            Rarity? rarity,
            IEnumerable<Item> customItems,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<StatName> statNames,
            IEnumerable<Skill> skills,
            Func<double> nextDouble,
            Func<int, int> nextInt,
            Func<int, int, int, int> roll,
            Func<string> newId)
        {
            return Chance(0.25d, nextDouble)
                ? CreateRandomItem(level, 1, rarity, customItems, itemDefinitions, statNames, skills, nextDouble, nextInt, newId)
                : CreateGold(roll == null ? 0 : roll(5, Math.Max(1, level) * 3, 1));
        }

        public static Item CreateRandomItem(
            int maxLevel,
            int minLevel,
            Rarity? rarity,
            IEnumerable<Item> customItems,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<StatName> statNames,
            IEnumerable<Skill> skills,
            Func<double> nextDouble,
            Func<int, int> nextInt,
            Func<string> newId)
        {
            maxLevel = Math.Max(maxLevel, 1);
            minLevel = Math.Max(minLevel, 1);

            if (Chance(0.50d, nextDouble))
            {
                var staticItems = (customItems ?? new List<Item>())
                    .Where(item => item != null &&
                                   (item.Type == ItemType.OneUse || item.Type == ItemType.RepeatableUse) &&
                                   !item.IsKey &&
                                   item.MinLevel < maxLevel)
                    .ToList();

                if (staticItems.Count > 0)
                {
                    return staticItems[Next(nextInt, staticItems.Count)];
                }
            }

            return CreateRandomEquipment(maxLevel, minLevel, rarity, null, null, null, itemDefinitions, statNames, skills, nextInt, newId);
        }

        public static Item CreateRandomEquipment(
            int maxLevel,
            int minLevel,
            Rarity? rarity,
            ItemType? type,
            Class? itemClass,
            Slot? slot,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<StatName> statNames,
            IEnumerable<Skill> skills,
            Func<int, int> nextInt,
            Func<string> newId)
        {
            maxLevel = Math.Max(maxLevel, 1);
            minLevel = Math.Max(Math.Min(minLevel, maxLevel), 1);
            var statNameList = statNames as IList<StatName> ?? statNames?.ToList();

            if (!rarity.HasValue)
            {
                rarity = SelectRarity(Next(nextInt, 100));
            }

            if (!type.HasValue)
            {
                var types = Item.EquippableItems;
                type = types[Next(nextInt, types.Count)];
            }

            var item = new Item
            {
                Rarity = rarity.Value,
                Type = type.Value,
                ImageId = 202,
                Id = newId == null ? Guid.NewGuid().ToString() : newId()
            };

            var availableItemDefinitions = GetAvailableItemDefinitions(itemDefinitions, type.Value, itemClass, slot).ToArray();
            if (availableItemDefinitions.Length == 0)
            {
                return null;
            }

            var itemDefinition = availableItemDefinitions[Next(nextInt, availableItemDefinitions.Length)];
            List<StatType> availableStats;
            switch (type.Value)
            {
                case ItemType.Weapon:
                    availableStats = new List<StatType> { StatType.Agility, StatType.Attack, StatType.Health, StatType.Magic };
                    item.MinLevel = RandomLevel(maxLevel, minLevel, nextInt);
                    item.Stats.Add(new StatValue
                    {
                        Type = StatType.Attack,
                        Value = Math.Max(item.MinLevel - 5 + Next(nextInt, 6) + itemDefinition.BaseStat, Math.Max(itemDefinition.BaseStat, 1))
                    });
                    break;
                case ItemType.Armor:
                    availableStats = new List<StatType>
                    {
                        StatType.Agility,
                        StatType.Defence,
                        StatType.Health,
                        StatType.Magic,
                        StatType.MagicDefence
                    };
                    item.MinLevel = RandomLevel(maxLevel, minLevel, nextInt);
                    item.Stats.Add(new StatValue
                    {
                        Type = StatType.Defence,
                        Value = Math.Max(item.MinLevel - 5 + Next(nextInt, 6) + itemDefinition.BaseStat, Math.Max(itemDefinition.BaseStat, 1))
                    });
                    break;
                default:
                    return null;
            }

            if (itemDefinition.Names == null || itemDefinition.Names.Count == 0)
            {
                return null;
            }

            var baseStatLevel = Math.Min((int)(item.MinLevel / 25.0f * itemDefinition.Names.Count), itemDefinition.Names.Count - 1);
            var baseName = itemDefinition.Names[baseStatLevel];
            if (itemDefinition.Classes != null)
            {
                item.Classes = itemDefinition.Classes.ToList();
            }

            item.ImageId = baseName.ImageId;
            item.Slots = itemDefinition.Slots;

            var prefix = string.Empty;
            var suffix = string.Empty;
            var statCount = Math.Min((int)rarity.Value, availableStats.Count);
            var chosenStats = new List<StatType>();
            for (var i = 0; i < statCount; i++)
            {
                var index = Next(nextInt, availableStats.Count);
                var stat = availableStats[index];
                availableStats.Remove(stat);
                chosenStats.Add(stat);
            }

            foreach (var stat in chosenStats.OrderBy(value => (int)value))
            {
                var itemLevel = RandomLevel(maxLevel, minLevel, nextInt);
                item.MinLevel = Math.Max(itemLevel, item.MinLevel);
                var statValue = Math.Max(itemLevel - 5 + Next(nextInt, 6), 1);
                item.Stats.Add(new StatValue
                {
                    Type = stat,
                    Value = statValue
                });

                ApplyStatName(statNameList, stat, itemLevel, ref prefix, ref suffix);
            }

            item.Name = BuildEquipmentName(baseName.Name, prefix, suffix);
            item.Cost = item.MinLevel * ((int)rarity.Value + 1) * 100;
            if (item.Cost > 0)
            {
                item.Cost += Next(nextInt, Math.Max(1, item.Cost / 3));
            }

            item.Setup(skills);
            return item;
        }

        public static Rarity SelectRarity(int roll)
        {
            return roll > 75
                ? roll > 90 ? roll > 98 ? Rarity.Epic : Rarity.Rare : Rarity.Uncommon
                : Rarity.Common;
        }

        public static IEnumerable<ItemDefinition> GetAvailableItemDefinitions(
            IEnumerable<ItemDefinition> itemDefinitions,
            ItemType type,
            Class? itemClass,
            Slot? slot)
        {
            return (itemDefinitions ?? new List<ItemDefinition>()).Where(definition =>
            {
                if (definition == null)
                {
                    return false;
                }

                if (itemClass.HasValue && slot.HasValue)
                {
                    return definition.Slots != null &&
                           definition.Classes != null &&
                           definition.Type == type &&
                           definition.Classes.Contains(itemClass.Value) &&
                           definition.Slots.Contains(slot.Value);
                }

                if (itemClass.HasValue)
                {
                    return definition.Classes != null &&
                           definition.Type == type &&
                           definition.Classes.Contains(itemClass.Value);
                }

                if (slot.HasValue)
                {
                    return definition.Slots != null &&
                           definition.Type == type &&
                           definition.Slots.Contains(slot.Value);
                }

                return definition.Type == type;
            });
        }

        public static string BuildEquipmentName(string baseName, string prefix, string suffix)
        {
            var name = baseName;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                name = prefix + " " + name;
            }

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                name = name + " of " + suffix;
            }

            return name;
        }

        public static int RandomLevel(int maxLevel, int minLevel, Func<int, int> nextInt)
        {
            maxLevel = Math.Max(maxLevel, 1);
            minLevel = Math.Max(Math.Min(minLevel, maxLevel), 1);
            return maxLevel <= minLevel ? minLevel : Next(nextInt, maxLevel - minLevel) + minLevel;
        }

        private static void ApplyStatName(IEnumerable<StatName> statNames, StatType stat, int itemLevel, ref string prefix, ref string suffix)
        {
            var statName = (statNames ?? new List<StatName>()).FirstOrDefault(item => item.Type == stat);
            if (statName == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(suffix) && statName.Suffix != null && statName.Suffix.Count > 0)
            {
                var statLevel = Math.Min((int)(itemLevel / 25.0f * statName.Suffix.Count), statName.Suffix.Count - 1);
                suffix = statName.Suffix[statLevel];
            }
            else if (statName.Prefix != null && statName.Prefix.Count > 0)
            {
                var statLevel = Math.Min((int)(itemLevel / 25.0f * statName.Prefix.Count), statName.Prefix.Count - 1);
                prefix = string.IsNullOrEmpty(prefix)
                    ? statName.Prefix[statLevel]
                    : prefix + " " + statName.Prefix[statLevel];
            }
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
            return maxValue <= 1 || nextInt == null ? 0 : nextInt(maxValue);
        }
    }
}
