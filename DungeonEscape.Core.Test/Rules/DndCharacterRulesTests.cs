using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class DndCharacterRulesTests
    {
        [Fact]
        public void ClassTypeUsesDndNames()
        {
            Assert.Equal(new[]
            {
                Class.Fighter,
                Class.Paladin,
                Class.Cleric,
                Class.Wizard,
                Class.Monk,
                Class.Warlock,
                Class.Bard,
                Class.Rogue,
                Class.Sorcerer
            }, DndCharacterRules.GetPlayableClasses());
            Assert.Equal("Paladin", DndCharacterRules.GetClassLabel(Class.Paladin));
            Assert.True(DndCharacterRules.IsClassNameMatch(" paladin ", Class.Paladin));
            Assert.False(DndCharacterRules.IsClassNameMatch("Hero", Class.Paladin));
        }

        [Fact]
        public void SpeciesModifiersApplyToStartingAbilityScores()
        {
            var hero = new Hero { Class = Class.Wizard, Species = Species.Elf };

            DndCharacterRules.ApplyStartingAbilityScores(hero);

            Assert.Equal(8, hero.Strength);
            Assert.Equal(16, hero.Dexterity);
            Assert.Equal(12, hero.Constitution);
            Assert.Equal(17, hero.Intelligence);
            Assert.Equal(12, hero.Wisdom);
            Assert.Equal(10, hero.Charisma);
        }

        [Fact]
        public void BackgroundsAddDefaultSkillProficiencies()
        {
            var classStats = new ClassStats
            {
                SkillProficiencies = new System.Collections.Generic.List<string> { "Athletics", "Perception" }
            };

            var background = DndCharacterRules.FindBackground(DndCharacterRules.GetDefaultBackgrounds(), "Criminal");
            var skills = DndCharacterRules.GetStartingSkillProficiencies(classStats, background);

            Assert.Contains("Athletics", skills);
            Assert.Contains("Deception", skills);
            Assert.Contains("Stealth", skills);
            Assert.Equal(skills.Count, new System.Collections.Generic.HashSet<string>(skills, System.StringComparer.OrdinalIgnoreCase).Count);
        }

        [Fact]
        public void BackgroundsAddAbilityBonuses()
        {
            var hero = new Hero { Strength = 10, Dexterity = 10, Constitution = 10, Intelligence = 10, Wisdom = 10, Charisma = 10 };
            var background = DndCharacterRules.FindBackground(DndCharacterRules.GetDefaultBackgrounds(), "Sage");

            DndCharacterRules.ApplyBackgroundAbilityBonuses(hero, background);

            Assert.Equal(12, hero.Intelligence);
            Assert.Equal(11, hero.Wisdom);
        }
    }
}
