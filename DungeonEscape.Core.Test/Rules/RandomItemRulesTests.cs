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
                null,
                null,
                () => 0.1d,
                max => 0,
                () => "id");

            Assert.Same(potion, item);
        }

        [Fact]
        public void CreateRandomEquipmentHonorsLevelRarityClassSlotAndBuildsName()
        {
            var random = new Queue<int>(new[] { 4, 0, 2, 2, 4, 0, 4, 0, 0 });
            var item = RandomItemRules.CreateRandomEquipment(
                10,
                1,
                Rarity.Rare,
                ItemType.Weapon,
                Class.Paladin,
                Slot.PrimaryHand,
                new[] { CreateWeaponDefinition() },
                CreateStatNames(),
                null,
                max => random.Dequeue(),
                () => "fixed-id");

            Assert.NotNull(item);
            Assert.Equal("fixed-id", item.Id);
            Assert.Equal(Rarity.Rare, item.Rarity);
            Assert.Equal(ItemType.Weapon, item.Type);
            Assert.Equal(5, item.MinLevel);
            Assert.Equal("Sword of Giants", item.Name);
            Assert.Equal(new[] { "Paladin" }, item.Classes);
            Assert.Equal(new[] { Slot.PrimaryHand }, item.Slots);
            Assert.Equal(1, item.DamageDice);
            Assert.Equal(8, item.DamageDie);
            Assert.Equal(1, item.DamageBonus);
            Assert.Equal(3, item.Stats.Count);
            Assert.Contains(item.Stats, stat => stat.Type == StatType.Attack && stat.Value == 5);
            Assert.Contains(item.Stats, stat => stat.Type == StatType.HP && stat.Value == 1);
            Assert.DoesNotContain(item.Stats, stat => stat.Type == StatType.Magic);
        }

        [Fact]
        public void CreateRandomArmorUsesDefinitionArmorClassAsDefenceBridge()
        {
            var random = new Queue<int>(new[] { 0, 0, 0 });
            var item = RandomItemRules.CreateRandomEquipment(
                1,
                1,
                Rarity.Common,
                ItemType.Armor,
                Class.Paladin,
                Slot.Chest,
                new[]
                {
                    new ItemDefinition
                    {
                        Type = ItemType.Armor,
                        ArmorClass = 16,
                        Classes = new List<string> { "Paladin" },
                        Slots = new List<Slot> { Slot.Chest },
                        Names = new List<ItemName> { new ItemName { Name = "Chain Mail", ImageId = 10 } }
                    }
                },
                CreateStatNames(),
                null,
                max => random.Dequeue(),
                () => "armor-id");

            Assert.NotNull(item);
            Assert.Contains(item.Stats, stat => stat.Type == StatType.Defence && stat.Value == 30);
        }

        [Fact]
        public void GetAvailableItemDefinitionsFiltersByTypeClassAndSlot()
        {
            var matching = CreateWeaponDefinition();
            var wrongClass = CreateWeaponDefinition();
            wrongClass.Classes = new List<string> { "Wizard" };
            var wrongSlot = CreateWeaponDefinition();
            wrongSlot.Slots = new List<Slot> { Slot.Chest };

            var definitions = RandomItemRules.GetAvailableItemDefinitions(
                new[] { matching, wrongClass, wrongSlot },
                ItemType.Weapon,
                Class.Paladin,
                Slot.PrimaryHand).ToList();

            Assert.Equal(new[] { matching }, definitions);
        }

        private static ItemDefinition CreateWeaponDefinition()
        {
            return new ItemDefinition
            {
                Type = ItemType.Weapon,
                BaseStat = 5,
                DamageDice = 1,
                DamageDie = 8,
                DamageBonus = 1,
                Classes = new List<string> { "Paladin" },
                Slots = new List<Slot> { Slot.PrimaryHand },
                Names = new List<ItemName>
                {
                    new ItemName { Name = "Sword", ImageId = 10 }
                }
            };
        }

        private static List<StatName> CreateStatNames()
        {
            return new List<StatName>
            {
                new StatName
                {
                    Type = StatType.HP,
                    Suffix = new List<string> { "Giants" }
                },
                new StatName
                {
                    Type = StatType.Magic,
                    Prefix = new List<string> { "Mystic" }
                }
            };
        }
    }
}
