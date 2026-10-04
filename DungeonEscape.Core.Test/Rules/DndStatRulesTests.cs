using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class DndStatRulesTests
    {
        [Theory]
        [InlineData(8, -1)]
        [InlineData(10, 0)]
        [InlineData(15, 2)]
        [InlineData(20, 5)]
        public void AbilityModifiersUseDndFormula(int score, int expected)
        {
            Assert.Equal(expected, DndStatRules.GetAbilityModifier(score));
        }

        [Fact]
        public void HeroAttackDamageAndHitPointsUseDndDerivedStats()
        {
            var hero = new Hero
            {
                Class = Class.Paladin,
                Level = 3,
                Strength = 16,
                Dexterity = 12,
                Constitution = 14,
                DamageDice = 1,
                DamageDie = 8,
                DamageBonus = 1
            };
            var classStats = new ClassStats { Class = "Paladin", HitDie = 10 };

            DndStatRules.RefreshHeroDerivedStats(hero);

            Assert.Equal(2, hero.ProficiencyBonus);
            Assert.Equal(5, DndStatRules.GetAttackBonus(hero));
            Assert.Equal(4, DndStatRules.GetDamageBonus(hero));
            Assert.Equal(28, DndStatRules.GetHeroHitPointsForLevel(hero, classStats, hero.Level));
        }

        [Fact]
        public void MonsterDamageBonusUsesExplicitStatBlockValue()
        {
            var monster = new MonsterInstance(
                new Monster
                {
                    Name = "Ogre",
                    HitPoints = 10,
                    MagicTimes = 1,
                    Strength = 18,
                    DamageBonus = 4
                },
                null);

            Assert.Equal(4, DndStatRules.GetDamageBonus(monster));
        }

        [Fact]
        public void MonsterHitPointsUseExplicitHitPointsWhenSet()
        {
            var monster = new Monster
            {
                HitPoints = 7,
                HitDice = "2d6"
            };

            Assert.Equal(7, DndStatRules.GetMonsterHitPoints(monster, _ => 6));
            Assert.Equal(7, DndStatRules.GetMonsterAverageHitPoints(monster));
        }

        [Fact]
        public void MonsterHitPointsRollHitDiceWhenHitPointsAreUnset()
        {
            var monster = new Monster
            {
                HitDice = "2d6+2"
            };

            Assert.Equal(8, DndStatRules.GetMonsterHitPoints(monster, _ => 3));
            Assert.Equal(9, DndStatRules.GetMonsterAverageHitPoints(monster));
        }

        [Fact]
        public void InitiativeUsesD20PlusDexterityModifier()
        {
            var action = new CombatRoundAction
            {
                Source = new Hero
                {
                    Dexterity = 16
                }
            };

            CombatRoundRules.RollInitiative(action, () => 11);

            Assert.Equal(11, action.InitiativeRoll);
            Assert.Equal(14, action.InitiativeTotal);
        }
    }
}
