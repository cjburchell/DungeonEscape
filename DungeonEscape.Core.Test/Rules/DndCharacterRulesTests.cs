using Redpoint.DungeonEscape.Rules;
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
    }
}
