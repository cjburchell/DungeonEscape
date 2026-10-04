using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class DndItemRules
    {
        public static ItemCategory GetCategory(Item item)
        {
            if (item == null)
            {
                return ItemCategory.Unknown;
            }

            if (item.Category != ItemCategory.Unknown)
            {
                return item.Category;
            }

            switch (item.Type)
            {
                case ItemType.Weapon:
                    return ItemCategory.Weapon;
                case ItemType.Armor:
                    return ItemCategory.Armor;
                case ItemType.OneUse:
                case ItemType.RepeatableUse:
                    return ItemCategory.Consumable;
                case ItemType.Gold:
                    return ItemCategory.AdventuringGear;
                case ItemType.Quest:
                    return ItemCategory.Gem;
                default:
                    return ItemCategory.Unknown;
            }
        }

        public static bool IsMagicItem(Item item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.IsMagicItem)
            {
                return true;
            }

            switch (item.Category)
            {
                case ItemCategory.Ring:
                case ItemCategory.Rod:
                case ItemCategory.Staff:
                case ItemCategory.Wand:
                case ItemCategory.WondrousItem:
                    return true;
            }

            return item.Rarity >= Rarity.Uncommon &&
                   item.Type != ItemType.Gold &&
                   item.Type != ItemType.Quest &&
                   item.Type != ItemType.Unknown;
        }
    }
}
