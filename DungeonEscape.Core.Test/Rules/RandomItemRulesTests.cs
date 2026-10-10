using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class RandomItemRulesTests
    {
        [Theory]
        [InlineData(75, Rarity.Common)]
        [InlineData(76, Rarity.Uncommon)]
        [InlineData(91, Rarity.Rare)]
        [InlineData(99, Rarity.Epic)]
        public void SelectRarityUsesExistingThresholds(int roll, Rarity expected)
        {
            Assert.Equal(expected, RandomItemRules.SelectRarity(roll));
        }

        [Fact]
        public void CreateRandomItemCanReturnEligibleStaticConsumable()
        {
            var potion = new Item { Name = "Potion", Type = ItemType.OneUse, MinLevel = 2 };
            var key = new Item { Name = "Key", Type = ItemType.OneUse, MinLevel = 1, Skill = new Skill { Type = SkillType.Open } };

            var item = RandomItemRules.CreateRandomItem(
                5,
                1,
                null,
                new[] { potion, key },
                null,
                () => 0.1d,
                max => 0,
                () => "id");

            Assert.Same(potion, item);
        }

        [Fact]
        public void CreateRandomItemPrefersExistingMagicItemsFromStaticCatalog()
        {
            var ring = new Item
            {
                Name = "Ring of Protection",
                Type = ItemType.Armor,
                Slots = new List<Slot> { Slot.Ring },
                MinLevel = 5,
                IsMagicItem = true,
                Rarity = Rarity.Rare,
                Category = ItemCategory.Ring
            };

            var item = RandomItemRules.CreateRandomItem(
                10,
                1,
                null,
                new[] { ring },
                null,
                () => 0.1d,
                max => 0,
                () => "id");

            Assert.Same(ring, item);
        }

        [Fact]
        public void CreateRandomItemFallsBackToGoldWhenNoStaticLootExists()
        {
            var item = RandomItemRules.CreateRandomItem(
                10,
                1,
                null,
                null,
                null,
                () => 0.9d,
                max => 0,
                () => "id");

            Assert.NotNull(item);
            Assert.Equal(ItemType.Gold, item.Type);
        }

        [Fact]
        public void CreateRandomItemFallsBackToEligibleStaticEquipmentBeforeGold()
        {
            var sword = new Item
            {
                Name = "Longsword",
                Type = ItemType.Weapon,
                MinLevel = 1,
                Cost = 15,
                Rarity = Rarity.Common
            };

            var item = RandomItemRules.CreateRandomItem(
                1,
                1,
                null,
                new[] { sword },
                null,
                () => 0.9d,
                max => 0,
                () => "id");

            Assert.Same(sword, item);
        }

        [Fact]
        public void DndItemRulesClassifiesMagicAndEquipmentByDndCategories()
        {
            var weapon = new Item
            {
                Name = "Longsword +1",
                Type = ItemType.Weapon,
                Category = ItemCategory.Weapon,
                Rarity = Rarity.Uncommon,
                IsMagicItem = true
            };

            var potion = new Item
            {
                Name = "Potion of Healing",
                Type = ItemType.OneUse,
                Category = ItemCategory.Potion,
                Rarity = Rarity.Uncommon,
                IsMagicItem = true
            };

            var gold = new Item
            {
                Name = "Gold",
                Type = ItemType.Gold,
                Category = ItemCategory.AdventuringGear,
                Rarity = Rarity.Common,
                IsMagicItem = false
            };

            Assert.Equal(ItemCategory.Weapon, weapon.Category);
            Assert.True(DndItemRules.IsMagicItem(weapon));
            Assert.Equal(ItemCategory.Potion, potion.Category);
            Assert.True(DndItemRules.IsMagicItem(potion));
            Assert.False(DndItemRules.IsMagicItem(gold));
        }

        [Fact]
        public void DndCharacterRulesUsesClassSpecificStartingGear()
        {
            var gear = DndCharacterRules.GetStartingEquipment(Class.Paladin);

            Assert.Contains(gear, item => item.Name == "Chain Mail" && item.Type == ItemType.Armor && item.Slots.Contains(Slot.Chest));
            Assert.Contains(gear, item => item.Name == "Longsword" && item.Type == ItemType.Weapon && item.Slots.Contains(Slot.PrimaryHand));
            Assert.Contains(gear, item => item.Name == "Shield" && item.Type == ItemType.Armor && item.Slots.Contains(Slot.OffHand));
            Assert.Contains(gear, item => item.Name == "Chain Mail" && item.ImageId == 280 && item.Weight == 55);
            Assert.Contains(gear, item => item.Name == "Longsword" && item.ImageId == 1 && item.Weight == 3);
            Assert.Contains(gear, item => item.Name == "Shield" && item.ImageId == 176 && item.Weight == 6);

            var wizardGear = DndCharacterRules.GetStartingEquipment(Class.Wizard);
            Assert.Contains(wizardGear, item => item.Name == "Robe" && item.Type == ItemType.Armor && item.Slots.Contains(Slot.Chest));
            Assert.Contains(wizardGear, item => item.Name == "Dagger" && item.Type == ItemType.Weapon && item.Slots.Contains(Slot.PrimaryHand));
            Assert.Contains(wizardGear, item => item.Name == "Robe" && item.ImageId == 303 && item.Weight == 4);
            Assert.Contains(wizardGear, item => item.Name == "Dagger" && item.ImageId == 37 && item.Weight == 1);
        }

        [Fact]
        public void HeroRequiresAttunementBeforeEquippingRestrictedMagicItem()
        {
            var hero = new Hero
            {
                Name = "Ari",
                Class = Class.Paladin,
                Level = 5,
                Health = 10,
                MaxHealth = 10,
                IsActive = true
            };

            var item = new ItemInstance(new Item
            {
                Name = "Ring of Protection",
                Type = ItemType.Armor,
                Category = ItemCategory.Ring,
                Rarity = Rarity.Rare,
                IsMagicItem = true,
                RequiresAttunement = true,
                AttunementClasses = new List<Class> { Class.Paladin },
                Slots = new List<Slot> { Slot.Ring }
            });

            Assert.False(hero.CanEquipItem(item));
            Assert.True(hero.CanAttuneItem(item));

            hero.AttuneItem(item);
            Assert.True(item.IsAttuned);
            Assert.True(hero.CanEquipItem(item));
        }
    }
}
