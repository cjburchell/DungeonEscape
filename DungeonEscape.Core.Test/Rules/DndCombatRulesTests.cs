using System.Collections.Generic;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class DndCombatRulesTests
    {
        [Fact]
        public void ResolveWeaponAttackHitsArmorClassWithD20PlusAttackBonus()
        {
            var attacker = new Hero { Name = "Hero", Level = 1, Strength = 16, DamageDice = 1, DamageDie = 8 };
            var target = new Hero { Name = "Target", ArmorClass = 15 };

            var result = DndCombatRules.ResolveWeaponAttack(attacker, target, () => 10, sides => 4);

            Assert.True(result.Hit);
            Assert.False(result.Critical);
            Assert.Equal(15, result.Total);
            Assert.Equal(15, result.TargetArmorClass);
            Assert.Equal(7, result.Damage);
        }

        [Fact]
        public void ResolveWeaponAttackMissesWhenTotalIsBelowArmorClass()
        {
            var attacker = new Hero { Name = "Hero", Level = 1, Strength = 14, DamageDice = 1, DamageDie = 8 };
            var target = new Hero { Name = "Target", ArmorClass = 16 };

            var result = DndCombatRules.ResolveWeaponAttack(attacker, target, () => 11, sides => 8);

            Assert.False(result.Hit);
            Assert.False(result.Critical);
            Assert.Equal(15, result.Total);
            Assert.Equal(0, result.Damage);
        }

        [Fact]
        public void NaturalTwentyHitsAndDoublesDamageDice()
        {
            var attacker = new Hero { Name = "Hero", Strength = 10, DamageDice = 1, DamageDie = 6 };
            var target = new Hero { Name = "Target", ArmorClass = 30 };
            var rollCount = 0;

            var result = DndCombatRules.ResolveWeaponAttack(
                attacker,
                target,
                () => 20,
                sides =>
                {
                    rollCount++;
                    return 3;
                });

            Assert.True(result.Hit);
            Assert.True(result.Critical);
            Assert.Equal(2, rollCount);
            Assert.Equal(6, result.Damage);
        }

        [Fact]
        public void ResolveWeaponAttackUsesSelectedWeaponStats()
        {
            var attacker = new Hero
            {
                Name = "Hero",
                Level = 1,
                Strength = 16
            };
            var target = new Hero { Name = "Target", ArmorClass = 15 };
            var weapon = new ItemInstance(new Item
            {
                Name = "Greatsword",
                Type = ItemType.Weapon,
                Slots = new List<Slot> { Slot.PrimaryHand },
                DamageDice = 2,
                DamageDie = 6,
                DamageBonus = 1
            });
            var damageRolls = new Queue<int>(new[] { 4, 5 });

            var result = DndCombatRules.ResolveWeaponAttack(
                attacker,
                weapon,
                target,
                () => 9,
                _ => damageRolls.Dequeue());

            Assert.True(result.Hit);
            Assert.Equal(15, result.Total);
            Assert.Equal(13, result.Damage);
        }

        [Fact]
        public void LegacyStatsDeriveArmorClassAndAttackBonus()
        {
            var attacker = new Hero
            {
                Name = "Hero",
                Level = 5,
                Attack = 12,
                Agility = 4
            };
            var target = new Hero
            {
                Name = "Target",
                Defence = 10,
                Agility = 8
            };

            Assert.Equal(6, DndCombatRules.GetAttackBonus(attacker));
            Assert.Equal(14, DndCombatRules.GetArmorClass(target));
        }
    }
}
