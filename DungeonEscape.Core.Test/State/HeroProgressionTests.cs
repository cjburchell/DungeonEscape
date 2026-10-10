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
            Assert.Equal((ulong)300, hero.NextLevel);
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
            Assert.Equal((ulong)900, hero.NextLevel);
            Assert.Equal(16, hero.MaxHealth);
            Assert.Equal(hero.MaxHealth, hero.Health);
            Assert.Equal(2, hero.Attack);
            Assert.Equal(3, hero.Defence);
            Assert.Equal(2, hero.MagicDefence);
            Assert.Equal(0, hero.MaxMagic);
            Assert.Equal(0, hero.Magic);
            Assert.Contains(hero.SpellSlots, slot => slot > 0);
            Assert.Equal(2, hero.Agility);
            Assert.Contains("Test Hero has advanced to level 2", message);
            Assert.Contains("Health +6", message);
            Assert.Contains("Spell slots:", message);
            Assert.Contains("Has learned the Cure Wounds Spell", message);
            Assert.DoesNotContain("Has learned the Lightning Bolt Spell", message);
            Assert.DoesNotContain("Attack +", message);
            Assert.DoesNotContain("Agility +", message);
        }

        [Fact]
        public void LevelUpPreservesExistingHeroEffectSkillsAndUnlocksSpellsByLevel()
        {
            var hero = CreateHero();
            hero.Xp = hero.NextLevel;

            hero.CheckLevelUp(CreateClassLevels(), CreateSpells(), out _);

            Assert.Equal(new[] { "Knock" }, hero.Skills);
            Assert.Equal(new[] { "Knock" }, hero.GetSkills(CreateSkills()).Select(skill => skill.Name).ToArray());
            Assert.Equal(new[] { "Cure Wounds" }, hero.GetSpells(CreateSpells()).Select(spell => spell.Name).ToArray());
        }

        [Fact]
        public void SetupCopiesDndSkillProficienciesWithoutGrantingLegacyClassSkills()
        {
            var hero = CreateHero();
            hero.Skills = new List<string> { "Knock" };

            hero.Setup(new TestGame(CreateClassLevels()), generateItems: false);

            Assert.Empty(hero.Skills);
            Assert.True(hero.HasSkillProficiency("Persuasion"));
            Assert.True(hero.HasSkillProficiency("athletics"));
            Assert.False(hero.HasSkillProficiency("Knock"));
        }

        [Fact]
        public void RogueSleightOfHandProficiencyGrantsStealCombatAction()
        {
            var hero = new Hero
            {
                Class = Class.Rogue,
                SkillProficiencies = new List<string> { "Sleight of Hand" }
            };

            Assert.Equal(new[] { "Sleight of Hand" }, hero.GetSkills(CreateSkills()).Select(skill => skill.Name).ToArray());
        }

        [Fact]
        public void PreparedSpellsCanBeAddedAndRemovedFromKnownSpells()
        {
            var hero = CreateHero();
            hero.Level = 5;
            var spells = CreateSpells();
            var heal = spells.First(spell => spell.Name == "Cure Wounds");
            var lightning = spells.First(spell => spell.Name == "Lightning Bolt");

            hero.PreparedSpells = new List<string> { "Cure Wounds" };

            Assert.True(hero.IsSpellPrepared(heal));
            Assert.False(hero.IsSpellPrepared(lightning));
            Assert.True(hero.PrepareSpell(lightning, spells));
            Assert.Contains(lightning, hero.GetSpells(spells));
            Assert.True(hero.UnprepareSpell(heal));
            Assert.DoesNotContain(heal, hero.GetSpells(spells));
        }

        [Fact]
        public void CantripsAreKnownAndCastableWithoutPreparationOrSlots()
        {
            var hero = CreateHero();
            hero.Level = 1;
            hero.SpellSlots = new List<int>();
            hero.UsedSpellSlots = new List<int>();
            var cantrip = new Spell
            {
                Name = "Fire Bolt",
                SpellLevel = 0,
                MinLevel = 1,
                Classes = new List<string> { "Paladin" }
            };

            Assert.Contains(cantrip, hero.GetKnownSpells(new[] { cantrip }));
            Assert.True(hero.IsSpellPrepared(cantrip));
            Assert.False(hero.CanPrepareSpell(cantrip, new[] { cantrip }));
            Assert.True(hero.HasAvailableSpellSlot(cantrip.SpellLevel));
            Assert.True(hero.UseSpellSlot(cantrip.SpellLevel));
        }

        private static Hero CreateHero()
        {
            return new Hero
            {
                Name = "Test Hero",
                Class = Class.Paladin,
                Gender = Gender.Male,
                Level = 1,
                Xp = 9,
                NextLevel = 300,
                MaxHealth = 10,
                Health = 10,
                Attack = 2,
                Defence = 3,
                MagicDefence = 2,
                MaxMagic = 4,
                Magic = 4,
                Agility = 2,
                Skills = new List<string> { "Knock" },
                Items = new List<ItemInstance>()
            };
        }

        private static List<ClassStats> CreateClassLevels()
        {
            return new List<ClassStats>
            {
                new ClassStats
                {
                    Class = "Paladin",
                    HitDie = 10,
                    SkillProficiencies = new List<string> { "Athletics", "Persuasion" }
                }
            };
        }

        private static List<Skill> CreateSkills()
        {
            return new List<Skill>
            {
                new Skill { Name = "Knock" },
                new Skill { Name = "Teleportation Circle" },
                new Skill { Name = "Sleight of Hand" }
            };
        }

        private static List<Spell> CreateSpells()
        {
            return new List<Spell>
            {
                new Spell
                {
                    Name = "Cure Wounds",
                    MinLevel = 2,
                    Classes = new List<string> { "Paladin" }
                },
                new Spell
                {
                    Name = "Lightning Bolt",
                    MinLevel = 3,
                    Classes = new List<string> { "Paladin" }
                },
                new Spell
                {
                    Name = "Shield of Faith",
                    MinLevel = 2,
                    Classes = new List<string> { "Wizard" }
                }
            };
        }

        private sealed class TestGame : IGame
        {
            public TestGame(List<ClassStats> classLevels)
            {
                ClassLevelStats = classLevels;
            }

            public Party Party { get; } = new Party();
            public List<ClassStats> ClassLevelStats { get; }
            public ISounds Sounds { get; }

            public void SetMap(string mapId = null, string spawnId = null, WorldPosition? point = null)
            {
            }

            public Item CreateChestItem(int level, Rarity? rarity = null)
            {
                return null;
            }

            public Item CreateGold(int gold)
            {
                return null;
            }

            public Item GetCustomItem(string itemId)
            {
                return null;
            }
        }
    }
}
