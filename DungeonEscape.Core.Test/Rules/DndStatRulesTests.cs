using System.Collections.Generic;
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
                Constitution = 14
            };
            var weapon = CreateWeapon("Longsword", 1, 8, 1);
            hero.Items.Add(weapon);
            hero.Equip(weapon);
            var classStats = new ClassStats { Class = "Paladin", HitDie = 10 };

            DndStatRules.RefreshHeroDerivedStats(hero);

            Assert.Equal(2, hero.ProficiencyBonus);
            Assert.Equal(6, DndStatRules.GetAttackBonus(hero));
            Assert.Equal(4, DndStatRules.GetDamageBonus(hero));
            Assert.Equal(28, DndStatRules.GetHeroHitPointsForLevel(hero, classStats, hero.Level));
        }

        [Fact]
        public void SelectedWeaponOverridesEquippedWeaponForAttackAndDamage()
        {
            var hero = new Hero
            {
                Class = Class.Fighter,
                Level = 1,
                Strength = 16
            };
            var club = CreateWeapon("Club", 1, 4, 0);
            var greatsword = CreateWeapon("Greatsword", 2, 6, 1);
            hero.Items.Add(club);
            hero.Items.Add(greatsword);
            hero.Equip(club);

            Assert.Equal(5, DndStatRules.GetAttackBonus(hero));
            Assert.Equal(4, DndStatRules.GetDamageBonus(hero, greatsword));
            Assert.Equal(2, DndStatRules.GetDamageDice(hero, greatsword));
            Assert.Equal(6, DndStatRules.GetDamageDie(hero, greatsword));
        }

        [Fact]
        public void HeroArmorClassUsesBaseDexterityAndEquippedArmorBonuses()
        {
            var hero = new Hero
            {
                Class = Class.Paladin,
                Dexterity = 12
            };
            var chainMail = CreateArmor("Chain Mail", Slot.Chest, 3);
            var shield = CreateArmor("Shield", Slot.OffHand, 2);
            hero.Items.Add(chainMail);
            hero.Items.Add(shield);
            hero.Equip(chainMail);
            hero.Equip(shield);

            Assert.Equal(16, DndStatRules.GetArmorClass(hero));
        }

        [Fact]
        public void HeroExplicitArmorClassIsBaseNotFinalArmorClass()
        {
            var hero = new Hero
            {
                ArmorClass = 10,
                Dexterity = 14
            };
            var leatherArmor = CreateArmor("Leather Armor", Slot.Chest, 1);
            hero.Items.Add(leatherArmor);
            hero.Equip(leatherArmor);

            Assert.Equal(13, DndStatRules.GetArmorClass(hero));
        }

        [Fact]
        public void SavingThrowsUseAbilityModifierPlusProficiencyWhenTrained()
        {
            var hero = new Hero
            {
                Class = Class.Fighter,
                Level = 5,
                Strength = 16,
                Dexterity = 12,
                Constitution = 14,
                Wisdom = 10,
                Charisma = 12
            };

            Assert.Equal(6, DndStatRules.GetSavingThrowModifier(hero, DndStatRules.AbilityScore.Strength, true));
            Assert.Equal(5, DndStatRules.GetSavingThrowModifier(hero, DndStatRules.AbilityScore.Constitution, true));
            Assert.Equal(0, DndStatRules.GetSavingThrowModifier(hero, DndStatRules.AbilityScore.Wisdom, false));
        }

        [Fact]
        public void SkillChecksUseAbilityModifierPlusProficiencyWhenApplicable()
        {
            var hero = new Hero
            {
                Class = Class.Rogue,
                Level = 4,
                Dexterity = 16,
                Wisdom = 12,
                Intelligence = 14,
                Charisma = 10
            };

            Assert.Equal(5, DndStatRules.GetSkillCheckModifier(hero, "Acrobatics", true));
            Assert.Equal(1, DndStatRules.GetSkillCheckModifier(hero, "Perception", false));
            Assert.Equal(2, DndStatRules.GetSkillCheckModifier(hero, "Investigation", false));
        }

        [Fact]
        public void SavingThrowAndSkillCheckResolutionUseDndFormulaAndDifficultyClass()
        {
            var hero = new Hero
            {
                Class = Class.Rogue,
                Level = 5,
                Dexterity = 16,
                Wisdom = 12,
                Intelligence = 14,
                Charisma = 10,
                Strength = 12
            };

            Assert.True(DndStatRules.RollSavingThrow(hero, DndStatRules.AbilityScore.Dexterity, true, 10, _ => 9));
            Assert.False(DndStatRules.RollSavingThrow(hero, DndStatRules.AbilityScore.Wisdom, false, 15, _ => 4));
            Assert.True(DndStatRules.RollSkillCheck(hero, "Acrobatics", true, 13, _ => 8));
            Assert.False(DndStatRules.RollSkillCheck(hero, "Perception", false, 12, _ => 3));
        }

        [Fact]
        public void ConcentrationTracksTheActiveSpellAndStopsWhenConditionsBreakIt()
        {
            var hero = new Hero
            {
                Name = "Ada",
                Level = 4,
                Strength = 14,
                Dexterity = 16,
                Constitution = 14,
                Health = 12,
                MaxHealth = 12
            };

            DndStatRules.StartConcentration(hero, "Faerie Fire");

            Assert.True(DndStatRules.IsConcentrating(hero));
            Assert.True(DndStatRules.CanConcentrate(hero));

            hero.AddEffect(new StatusEffect { Type = EffectType.Stunned, Name = "Stunned" });
            Assert.False(DndStatRules.CanConcentrate(hero));

            DndStatRules.EndConcentration(hero);
            Assert.False(DndStatRules.IsConcentrating(hero));
        }

        [Fact]
        public void ConcentrationSpellsCannotBeCastWhenTheCasterCannotConcentrate()
        {
            var hero = new Hero
            {
                Name = "Ada",
                Level = 4,
                Strength = 14,
                Dexterity = 16,
                Constitution = 14,
                Health = 12,
                MaxHealth = 12,
                IsActive = true
            };

            var concentrationSpell = new Spell
            {
                Name = "Slow",
                DndSpell = "Slow",
                SpellLevel = 3,
                School = "Transmutation"
            };

            Assert.True(concentrationSpell.RequiresConcentration);
            Assert.True(DndStatRules.CanConcentrate(hero));

            DndStatRules.StartConcentration(hero, "Faerie Fire");
            Assert.True(concentrationSpell.CanBeCastBy(hero));

            DndStatRules.EndConcentration(hero);
            hero.AddEffect(new StatusEffect { Type = EffectType.Stunned, Name = "Stunned" });
            Assert.False(concentrationSpell.CanBeCastBy(hero));
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

        private static ItemInstance CreateArmor(string name, Slot slot, int defenceBonus)
        {
            return new ItemInstance(new Item
            {
                Name = name,
                Type = ItemType.Armor,
                Category = ItemCategory.Armor,
                Slots = new List<Slot> { slot },
                Stats = new List<StatValue>
                {
                    new StatValue { Type = StatType.Defence, Value = defenceBonus }
                }
            });
        }

        private static ItemInstance CreateWeapon(string name, int damageDice, int damageDie, int damageBonus)
        {
            return new ItemInstance(new Item
            {
                Name = name,
                Type = ItemType.Weapon,
                Category = ItemCategory.Weapon,
                Slots = new List<Slot> { Slot.PrimaryHand },
                DamageDice = damageDice,
                DamageDie = damageDie,
                DamageBonus = damageBonus
            });
        }
    }
}
