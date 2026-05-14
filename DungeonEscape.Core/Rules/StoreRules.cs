using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class StoreRules
    {
        public const int MaxStoreInventoryBeforeSellRestock = 15;

        public static bool IsKeyStoreObject(TiledObjectInfo storeObject)
        {
            return storeObject != null &&
                   string.Equals(storeObject.Class, "NpcKey", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetStoreName(TiledObjectInfo storeObject)
        {
            return storeObject == null || string.IsNullOrEmpty(storeObject.Name) ||
                   string.Equals(storeObject.Name, "#Random#", StringComparison.OrdinalIgnoreCase)
                ? "Store"
                : storeObject.Name;
        }

        public static string GetStoreText(TiledObjectInfo storeObject)
        {
            return IsKeyStoreObject(storeObject)
                ? "Would you like to buy a key?"
                : GetStringProperty(storeObject, "Text", "Welcome to my store. I buy and sell items.");
        }

        public static bool StoreWillBuyItems(TiledObjectInfo storeObject)
        {
            return !IsKeyStoreObject(storeObject) && GetBoolProperty(storeObject, "WillBuyItems", true);
        }

        public static string GetStringProperty(TiledObjectInfo storeObject, string propertyName, string defaultValue)
        {
            string value;
            return storeObject != null &&
                   storeObject.Properties != null &&
                   storeObject.Properties.TryGetValue(propertyName, out value) &&
                   !string.IsNullOrEmpty(value)
                ? value
                : defaultValue;
        }

        public static bool GetBoolProperty(TiledObjectInfo storeObject, string propertyName, bool defaultValue)
        {
            string value;
            bool result;
            return storeObject != null &&
                   storeObject.Properties != null &&
                   storeObject.Properties.TryGetValue(propertyName, out value) &&
                   bool.TryParse(value, out result)
                ? result
                : defaultValue;
        }

        public static List<Hero> GetBuyRecipients(Party party)
        {
            return party == null
                ? new List<Hero>()
                : party.AliveMembers.Where(hero => hero.Items.Count < Party.MaxItems).ToList();
        }

        public static IEnumerable<ItemInstance> GetSellableItems(Hero hero)
        {
            return hero == null || hero.Items == null
                ? new List<ItemInstance>()
                : hero.Items.Where(CanSellItem);
        }

        public static bool CanSellItem(ItemInstance item)
        {
            return item != null &&
                   item.Item != null &&
                   item.Item.CanBeSoldInStore &&
                   item.Type != ItemType.Quest;
        }

        public static int GetSalePrice(ItemInstance item)
        {
            return item == null ? 0 : Math.Max(1, item.Gold * 3 / 4);
        }

        public static bool CanBuy(Party party, Item item)
        {
            return item != null &&
                   party != null &&
                   party.Gold >= item.Cost &&
                   GetBuyRecipients(party).Count > 0;
        }

        public static List<Item> CreateInitialStoreInventory(
            TiledObjectInfo storeObject,
            IEnumerable<Item> customItems,
            Func<string, Item> getCustomItem,
            Func<Item> createRandomItem)
        {
            var items = new List<Item>();
            if (storeObject == null)
            {
                return items;
            }

            var availableItems = customItems == null ? new List<Item>() : customItems.Where(item => item != null).ToList();
            if (IsKeyStoreObject(storeObject))
            {
                return availableItems
                    .Where(item => item.IsKey)
                    .OrderBy(item => item.MinLevel)
                    .ThenBy(item => item.Cost)
                    .ToList();
            }

            string itemListString;
            if (storeObject.Properties != null &&
                storeObject.Properties.TryGetValue("Items", out itemListString) &&
                !string.IsNullOrWhiteSpace(itemListString))
            {
                foreach (var itemId in itemListString.Split(',').Select(value => value.Trim()))
                {
                    var item = getCustomItem == null ? null : getCustomItem(itemId);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }

                return items.OrderBy(item => item.Cost).ToList();
            }

            if (createRandomItem == null)
            {
                return items;
            }

            items.AddRange(GetCommonStoreStock(availableItems));

            for (var i = items.Count; i < 10; i++)
            {
                var item = createRandomItem();
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items.OrderBy(item => item.Cost).ToList();
        }

        private static IEnumerable<Item> GetCommonStoreStock(IEnumerable<Item> availableItems)
        {
            return availableItems
                .Where(item =>
                    item != null &&
                    !item.IsKey &&
                    item.Type != ItemType.Gold &&
                    item.Type != ItemType.Quest &&
                    item.Type != ItemType.Unknown &&
                    item.Rarity == Rarity.Common &&
                    item.MinLevel <= 0)
                .OrderBy(item => item.Cost)
                .ThenBy(item => item.Name)
                .Take(2);
        }

        public static bool ContainsInvalidStoreItems(TiledObjectInfo storeObject, List<Item> items)
        {
            if (items == null)
            {
                return true;
            }

            if (IsKeyStoreObject(storeObject))
            {
                return items.Any(item => item == null || !item.IsKey);
            }

            string itemListString;
            var hasFixedInventory = storeObject != null &&
                                    storeObject.Properties != null &&
                                    storeObject.Properties.TryGetValue("Items", out itemListString) &&
                                    !string.IsNullOrWhiteSpace(itemListString);
            if (hasFixedInventory)
            {
                return false;
            }

            return items.Any(item =>
                item == null ||
                item.Type == ItemType.Gold ||
                item.Type == ItemType.Quest ||
                item.Type == ItemType.Unknown);
        }

        public static string BuyStoreItem(
            Party party,
            Item item,
            Hero recipient,
            IList<Item> storeInventory,
            out ItemInstance purchasedItem)
        {
            purchasedItem = null;
            if (item == null)
            {
                return "That item is not available.";
            }

            if (recipient == null || party == null || !party.Members.Contains(recipient) || recipient.Items.Count >= Party.MaxItems)
            {
                return "No one has room to carry that.";
            }

            if (party.Gold < item.Cost)
            {
                return "You do not have enough gold.";
            }

            purchasedItem = new ItemInstance(item);
            recipient.Items.Add(purchasedItem);
            party.Gold -= item.Cost;
            if (storeInventory != null)
            {
                storeInventory.Remove(item);
            }

            return recipient.Name + " bought " + item.Name + " for " + item.Cost + " gold.";
        }

        public static string SellHeroItem(
            Party party,
            Hero hero,
            ItemInstance item,
            IList<Item> storeInventory)
        {
            if (party == null ||
                hero == null ||
                item == null ||
                item.Item == null ||
                !party.Members.Contains(hero) ||
                !hero.Items.Contains(item))
            {
                return "That item cannot be sold.";
            }

            if (!CanSellItem(item))
            {
                return item.Name + " cannot be sold.";
            }

            var salePrice = GetSalePrice(item);
            item.UnEquip(party.Members);
            hero.Items.Remove(item);
            party.Gold += salePrice;
            if (storeInventory != null && storeInventory.Count <= MaxStoreInventoryBeforeSellRestock)
            {
                storeInventory.Add(item.Item);
                SortStoreInventory(storeInventory);
            }

            return hero.Name + " sold " + item.Name + " for " + salePrice + " gold.";
        }

        public static void SortStoreInventory(IList<Item> inventory)
        {
            var list = inventory as List<Item>;
            if (list != null)
            {
                list.Sort((left, right) => left.Cost.CompareTo(right.Cost));
            }
        }
    }
}
