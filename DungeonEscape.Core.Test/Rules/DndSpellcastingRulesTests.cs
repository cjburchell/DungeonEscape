using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class DndSpellcastingRulesTests
    {
        [Fact]
        public void FullCastersUseDndSpellSlotProgression()
        {
            var slots = DndSpellcastingRules.GetMaxSpellSlots(Class.Wizard, 5);

            Assert.Equal(4, slots[0]);
            Assert.Equal(3, slots[1]);
            Assert.Equal(2, slots[2]);
            Assert.Equal(0, slots[3]);
        }

        [Fact]
        public void PaladinsUseHalfCasterSpellSlotProgression()
        {
            var levelOne = DndSpellcastingRules.GetMaxSpellSlots(Class.Paladin, 1);
            var levelFive = DndSpellcastingRules.GetMaxSpellSlots(Class.Paladin, 5);

            Assert.Equal(0, levelOne[0]);
            Assert.Equal(4, levelFive[0]);
            Assert.Equal(2, levelFive[1]);
        }

        [Fact]
        public void WarlocksUsePactSlotProgression()
        {
            var slots = DndSpellcastingRules.GetMaxSpellSlots(Class.Warlock, 9);

            Assert.Equal(0, slots[0]);
            Assert.Equal(0, slots[3]);
            Assert.Equal(2, slots[4]);
        }

        [Fact]
        public void PreparedSpellLimitUsesClassAndSpellcastingAbility()
        {
            var wizard = new Hero { Class = Class.Wizard, Level = 5, Intelligence = 16 };
            var paladin = new Hero { Class = Class.Paladin, Level = 5, Charisma = 14 };
            var fighter = new Hero { Class = Class.Fighter, Level = 5 };

            Assert.Equal(8, DndSpellcastingRules.GetPreparedSpellLimit(wizard));
            Assert.Equal(4, DndSpellcastingRules.GetPreparedSpellLimit(paladin));
            Assert.Equal(0, DndSpellcastingRules.GetPreparedSpellLimit(fighter));
        }
    }
}
