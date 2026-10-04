using Redpoint.DungeonEscape.Rules;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class DndLevelProgressionRulesTests
    {
        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 300)]
        [InlineData(3, 900)]
        [InlineData(5, 6500)]
        [InlineData(9, 48000)]
        [InlineData(13, 120000)]
        [InlineData(17, 225000)]
        [InlineData(20, 355000)]
        public void UsesDndCharacterAdvancementXpThresholds(int level, ulong xp)
        {
            Assert.Equal(xp, DndLevelProgressionRules.GetXpForLevel(level));
        }

        [Fact]
        public void MaxLevelCharactersDoNotLevelFurther()
        {
            Assert.False(DndLevelProgressionRules.CanLevelUp(20, 400000));
            Assert.Equal((ulong)355000, DndLevelProgressionRules.GetNextLevelXp(20));
        }
    }
}
