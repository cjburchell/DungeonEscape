using Redpoint.DungeonEscape.Data;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class StoreRulesTests
    {
        [Fact]
        public void StoreMetadataUsesDefaultsForRandomStoreObject()
        {
            var store = new TiledObjectInfo { Name = "#Random#" };

            Assert.Equal("Store", StoreRules.GetStoreName(store));
            Assert.Equal("Welcome to my store. I buy and sell items.", StoreRules.GetStoreText(store));
            Assert.True(StoreRules.StoreWillBuyItems(store));
        }

        [Fact]
        public void KeyStoreDoesNotBuyItemsAndUsesKeyText()
        {
            var store = new TiledObjectInfo { Class = "NpcKey" };

            Assert.True(StoreRules.IsKeyStoreObject(store));
            Assert.False(StoreRules.StoreWillBuyItems(store));
            Assert.Equal("Would you like to buy a key?", StoreRules.GetStoreText(store));
        }

        [Fact]
        public void GetBuyRecipientsReturnsAliveMembersWithRoom()
        {
            var party = new Party();
            var aliveWithRoom = CreateHero("Able", 1, 0);
            var aliveFull = CreateHero("Full", 1, Party.MaxItems);
            var deadWithRoom = CreateHero("Dead", 0, 0);
            deadWithRoom.Health = 0;
            party.Members.Add(aliveWithRoom);
            party.Members.Add(aliveFull);
            party.Members.Add(deadWithRoom);

            var recipients = StoreRules.GetBuyRecipients(party);

            Assert.Equal(new[] { aliveWithRoom }, recipients);
        }

        [Fact]
        public void GetSellableItemsFiltersQuestGoldAndInvalidItems()
        {
            var hero = CreateHero("Able", 1, 0);
            var regular = new ItemInstance(CreateItem("Sword", ItemType.Weapon, 100));
            hero.Items.Add(regular);
            hero.Items.Add(new ItemInstance(CreateItem("Quest", ItemType.Quest, 100)));
            hero.Items.Add(new ItemInstance(CreateItem("Gold", ItemType.Gold, 100)));
            hero.Items.Add(null);

            var sellable = StoreRules.GetSellableItems(hero).ToList();

            Assert.Equal(new[] { regular }, sellable);
        }

        [Theory]
        [InlineData(100, 75)]
        [InlineData(1, 1)]
        public void GetSalePriceReturnsThreeQuarterValueWithMinimumOne(int cost, int expected)
        {
            Assert.Equal(expected, StoreRules.GetSalePrice(new ItemInstance(CreateItem("Item", ItemType.Weapon, cost))));
        }

        [Fact]
        public void CreateInitialStoreInventoryReturnsKeysForKeyStore()
        {
            var key = CreateItem("Key", ItemType.OneUse, 10);
            key.Skill = new Skill { Type = SkillType.Open };
            var regular = CreateItem("Potion", ItemType.OneUse, 5);

            var inventory = StoreRules.CreateInitialStoreInventory(
                new TiledObjectInfo { Class = "NpcKey" },
                new[] { regular, key },
                id => null,
                () => null);

            Assert.Equal(new[] { key }, inventory);
        }

        [Fact]
        public void CreateInitialStoreInventoryUsesFixedItemList()
        {
            var cheap = CreateItem("Cheap", ItemType.OneUse, 5);
            var expensive = CreateItem("Expensive", ItemType.OneUse, 50);
            var store = new TiledObjectInfo
            {
                Properties = new Dictionary<string, string> { { "Items", "Expensive, Cheap" } }
            };

            var inventory = StoreRules.CreateInitialStoreInventory(
                store,
                null,
                id => id == "Cheap" ? cheap : id == "Expensive" ? expensive : null,
                () => null);

            Assert.Equal(new[] { cheap, expensive }, inventory);
        }

        [Fact]
        public void BuyStoreItemTransfersGoldAndItem()
        {
            var party = new Party { Gold = 100 };
            var hero = CreateHero("Able", 1, 0);
            party.Members.Add(hero);
            var item = CreateItem("Potion", ItemType.OneUse, 40);
            var inventory = new List<Item> { item };

            ItemInstance purchased;
            var message = StoreRules.BuyStoreItem(party, item, hero, inventory, out purchased);

            Assert.Equal("Able bought Potion for 40 gold.", message);
            Assert.Equal(60, party.Gold);
            Assert.Same(purchased, hero.Items.Single());
            Assert.Empty(inventory);
        }

        [Fact]
        public void SellHeroItemTransfersGoldAndRestocksStore()
        {
            var party = new Party { Gold = 10 };
            var hero = CreateHero("Able", 1, 0);
            party.Members.Add(hero);
            var instance = new ItemInstance(CreateItem("Sword", ItemType.Weapon, 100));
            hero.Items.Add(instance);
            var inventory = new List<Item>();

            var message = StoreRules.SellHeroItem(party, hero, instance, inventory);

            Assert.Equal("Able sold Sword for 75 gold.", message);
            Assert.Equal(85, party.Gold);
            Assert.Empty(hero.Items);
            Assert.Equal(new[] { instance.Item }, inventory);
        }

        private static Hero CreateHero(string name, int health, int itemCount)
        {
            var hero = new Hero
            {
                Name = name,
                IsActive = true,
                Health = health,
                Items = new List<ItemInstance>()
            };

            for (var i = 0; i < itemCount; i++)
            {
                hero.Items.Add(new ItemInstance(CreateItem("Item " + i, ItemType.OneUse, 1)));
            }

            return hero;
        }

        private static Item CreateItem(string name, ItemType type, int cost)
        {
            return new Item
            {
                Id = name,
                Name = name,
                Type = type,
                Cost = cost,
                Slots = new List<Slot>()
            };
        }
    }
}
