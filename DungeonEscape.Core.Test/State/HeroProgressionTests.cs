using Redpoint.DungeonEscape.Data;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.State
{
    public sealed class HeroProgressionTests
    {
        [Fact]
        public void HeroDoesNotLevelBeforeNextLevelXp()
        {
            var hero = CreateHero();
            string message;

            var leveled = hero.CheckLevelUp(CreateClassLevels(), CreateSpells(), out message);

            Assert.False(leveled);
            Assert.Null(message);
            Assert.Equal(1, hero.Level);
            Assert.Equal((ulong)9, hero.Xp);
        }

        [Fact]
        public void HeroLevelsUpWhenXpReachesNextLevel()
        {
            var hero = CreateHero();
            hero.Xp = hero.NextLevel;
            string message;

            var leveled = hero.CheckLevelUp(CreateClassLevels(), CreateSpells(), out message);

            Assert.True(leveled);
            Assert.Equal(2, hero.Level);
            Assert.Equal((ulong)30, hero.NextLevel);
            Assert.Equal(13, hero.MaxHealth);
            Assert.Equal(hero.MaxHealth, hero.Health);
            Assert.Equal(4, hero.Attack);
            Assert.Equal(4, hero.Defence);
            Assert.Equal(3, hero.MagicDefence);
            Assert.Equal(6, hero.MaxMagic);
            Assert.Equal(hero.MaxMagic, hero.Magic);
            Assert.Equal(3, hero.Agility);
            Assert.Contains("Test Hero has advanced to level 2", message);
            Assert.Contains("Has learned the Heal Spell", message);
            Assert.DoesNotContain("Has learned the Lightning Spell", message);
        }

        [Fact]
        public void LevelUpPreservesClassSkillsAndUnlocksSpellsByLevel()
        {
            var hero = CreateHero();
            hero.Xp = hero.NextLevel;

            hero.CheckLevelUp(CreateClassLevels(), CreateSpells(), out _);

            Assert.Equal(new[] { "Swipe" }, hero.Skills);
            Assert.Equal(new[] { "Swipe" }, hero.GetSkills(CreateSkills()).Select(skill => skill.Name).ToArray());
            Assert.Equal(new[] { "Heal" }, hero.GetSpells(CreateSpells()).Select(spell => spell.Name).ToArray());
        }

        private static Hero CreateHero()
        {
            return new Hero
            {
                Name = "Test Hero",
                Class = Class.Hero,
                Gender = Gender.Male,
                Level = 1,
                Xp = 9,
                NextLevel = 10,
                MaxHealth = 10,
                Health = 10,
                Attack = 2,
                Defence = 3,
                MagicDefence = 2,
                MaxMagic = 4,
                Magic = 4,
                Agility = 2,
                Skills = new List<string> { "Swipe" },
                Items = new List<ItemInstance>()
            };
        }

        private static List<ClassStats> CreateClassLevels()
        {
            return new List<ClassStats>
            {
                new ClassStats
                {
                    Class = Class.Hero,
                    FirstLevel = 10,
                    Skills = new List<string> { "Swipe" },
                    Stats = new List<Stats>
                    {
                        CreateStat(StatType.Health, 3),
                        CreateStat(StatType.Attack, 2),
                        CreateStat(StatType.Defence, 1),
                        CreateStat(StatType.MagicDefence, 1),
                        CreateStat(StatType.Magic, 2),
                        CreateStat(StatType.Agility, 1)
                    }
                }
            };
        }

        private static Stats CreateStat(StatType type, int rollConst)
        {
            return new Stats
            {
                Type = type,
                Roll = 0,
                RollConst = rollConst
            };
        }

        private static List<Skill> CreateSkills()
        {
            return new List<Skill>
            {
                new Skill { Name = "Swipe" },
                new Skill { Name = "Steal" }
            };
        }

        private static List<Spell> CreateSpells()
        {
            return new List<Spell>
            {
                new Spell
                {
                    Name = "Heal",
                    MinLevel = 2,
                    Classes = new List<Class> { Class.Hero }
                },
                new Spell
                {
                    Name = "Lightning",
                    MinLevel = 3,
                    Classes = new List<Class> { Class.Hero }
                },
                new Spell
                {
                    Name = "Upper",
                    MinLevel = 2,
                    Classes = new List<Class> { Class.Wizard }
                }
            };
        }
    }
}
