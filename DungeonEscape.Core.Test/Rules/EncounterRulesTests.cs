using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class EncounterRulesTests
    {
        [Fact]
        public void FilterRandomMonstersAppliesBiomeAndLevelBounds()
        {
            var cave = CreateRandomMonster("Cave", Biome.Cave, 3, Rarity.Common);
            var forest = CreateRandomMonster("Forest", Biome.Forest, 3, Rarity.Common);
            var tooLow = CreateRandomMonster("Low", Biome.Cave, 1, Rarity.Common);
            var tooHigh = CreateRandomMonster("High", Biome.Cave, 9, Rarity.Common);

            var filtered = EncounterRules.FilterRandomMonsters(
                new[] { cave, forest, tooLow, tooHigh },
                new BiomeInfo { Type = Biome.Cave, MinMonsterLevel = 2, MaxMonsterLevel = 8 }).ToList();

            Assert.Equal(new[] { cave }, filtered);
        }

        [Theory]
        [InlineData(Rarity.Common, 20)]
        [InlineData(Rarity.Uncommon, 5)]
        [InlineData(Rarity.Rare, 2)]
        [InlineData(Rarity.Epic, 1)]
        public void GetMonsterProbabilityReturnsRarityWeights(Rarity rarity, int expected)
        {
            Assert.Equal(expected, EncounterRules.GetMonsterProbability(rarity, () => 20));
        }

        [Fact]
        public void LegendaryProbabilityDependsOnD20Roll()
        {
            Assert.Equal(0, EncounterRules.GetMonsterProbability(Rarity.Legendary, () => 14));
            Assert.Equal(1, EncounterRules.GetMonsterProbability(Rarity.Legendary, () => 15));
        }

        [Fact]
        public void BuildRandomEncounterLimitsMonsterCountAndGroupsByPartyScale()
        {
            var randoms = new[]
            {
                CreateRandomMonster("Slime", Biome.Cave, 1, Rarity.Common, 4),
                CreateRandomMonster("Bat", Biome.Cave, 1, Rarity.Common, 4),
                CreateRandomMonster("Ghost", Biome.Cave, 1, Rarity.Common, 4),
                CreateRandomMonster("Dragon", Biome.Cave, 1, Rarity.Common, 4)
            };
            var rolls = new Queue<int>(new[] { 9, 0, 0, 1, 0, 1 });

            var monsters = EncounterRules.BuildRandomEncounter(
                randoms,
                new BiomeInfo { Type = Biome.Cave },
                40,
                4,
                false,
                0,
                max => rolls.Count == 0 ? 0 : rolls.Dequeue() % max,
                () => 20,
                monster => 999);

            Assert.True(monsters.Count <= EncounterRules.MaxMonstersToFight);
            Assert.True(monsters.Select(monster => monster.Name).Distinct().Count() <= EncounterRules.MaxMonsterGroups);
        }

        [Fact]
        public void ApplyRepelRemovesMonstersBelowPartyMaxHealth()
        {
            var weak = CreateMonster("Weak", Biome.Cave, 1, Rarity.Common);
            var strong = CreateMonster("Strong", Biome.Cave, 1, Rarity.Common);
            var monsters = new List<Monster> { weak, strong };

            EncounterRules.ApplyRepel(
                monsters,
                true,
                20,
                monster => monster.Name == "Weak" ? 10 : 25);

            Assert.Equal(new[] { strong }, monsters);
        }

        private static RandomMonster CreateRandomMonster(string name, Biome biome, int minLevel, Rarity rarity, int groupSize = 1)
        {
            return new RandomMonster
            {
                Name = name,
                Rarity = rarity,
                Data = CreateMonster(name, biome, minLevel, rarity, groupSize),
                IsOverworld = true
            };
        }

        private static Monster CreateMonster(string name, Biome biome, int minLevel, Rarity rarity, int groupSize = 1)
        {
            return new Monster
            {
                Name = name,
                MinLevel = minLevel,
                Rarity = rarity,
                GroupSize = groupSize,
                Biomes = new List<Biome> { biome },
                HealthConst = 1,
                HealthTimes = 1
            };
        }
    }
}
