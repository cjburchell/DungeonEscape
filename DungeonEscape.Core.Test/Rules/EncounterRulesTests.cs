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
        public void BuildRandomEncounterRejectsHighLevelThreatForLowLevelParty()
        {
            var randoms = new[]
            {
                CreateRandomMonster("Boss", Biome.Cave, 15, Rarity.Common, 5),
                CreateRandomMonster("Weakling", Biome.Cave, 1, Rarity.Common, 2)
            };

            var monsters = EncounterRules.BuildRandomEncounter(
                randoms,
                new BiomeInfo { Type = Biome.Cave },
                2,
                2,
                false,
                0,
                max => 0,
                () => 20,
                monster => 999);

            Assert.DoesNotContain(monsters, monster => monster.Name == "Boss");
            Assert.Contains(monsters, monster => monster.Name == "Weakling");
        }

        [Fact]
        public void BuildRandomEncounterReducesPackHeavySpawnsForEarlyParties()
        {
            var randoms = new[]
            {
                CreateRandomMonster("Pack", Biome.Cave, 2, Rarity.Uncommon, 6)
            };

            var monsters = EncounterRules.BuildRandomEncounter(
                randoms,
                new BiomeInfo { Type = Biome.Cave },
                2,
                2,
                false,
                0,
                max => 0,
                () => 20,
                monster => 999);

            Assert.True(monsters.Count <= 4);
        }

        [Fact]
        public void ApplyDisengageRemovesMonstersBelowPartyMaxHealth()
        {
            var weak = CreateMonster("Weak", Biome.Cave, 1, Rarity.Common);
            var strong = CreateMonster("Strong", Biome.Cave, 1, Rarity.Common);
            var monsters = new List<Monster> { weak, strong };

            EncounterRules.ApplyDisengage(
                monsters,
                true,
                20,
                monster => monster.Name == "Weak" ? 10 : 25);

            Assert.Equal(new[] { strong }, monsters);
        }

        [Fact]
        public void EncounterAvoidanceAllowsUnnoticedPartyToSneakAwayForPartialXp()
        {
            var party = CreateParty(new Hero
            {
                Dexterity = 16,
                SkillProficiencies = new List<string> { "Stealth" }
            });
            var monster = CreateMonster("Guard", Biome.Cave, 2, Rarity.Common);
            monster.Wisdom = 10;
            monster.ProficiencyBonus = 2;
            monster.Xp = 100;
            var rolls = new Queue<int>(new[] { 16, 12 });

            var context = EncounterAvoidanceRules.CreateContext(party, new[] { monster }, _ => rolls.Dequeue());
            var result = EncounterAvoidanceRules.Resolve(
                party,
                new[] { monster },
                context,
                EncounterAvoidanceMethod.SneakAway,
                _ => rolls.Dequeue());

            Assert.True(context.PartyUnnoticed);
            Assert.True(result.Success);
            Assert.Equal(50, EncounterAvoidanceRules.GetEncounterXp(new[] { monster }, result.XpMultiplier));
        }

        [Fact]
        public void EncounterAvoidanceAllowsTalkingDownIntelligentMonstersForFullXp()
        {
            var party = CreateParty(new Hero
            {
                Charisma = 16,
                SkillProficiencies = new List<string> { "Persuasion" }
            });
            var monster = CreateMonster("Goblin", Biome.Cave, 2, Rarity.Common);
            monster.Intelligence = 10;
            monster.Wisdom = 8;
            monster.Languages = "Common, Goblin";
            monster.Alignment = "neutral";
            monster.CanBeReasonedWith = true;
            monster.Xp = 100;
            var context = EncounterAvoidanceRules.CreateContext(party, new[] { monster }, _ => 1);

            var result = EncounterAvoidanceRules.Resolve(
                party,
                new[] { monster },
                context,
                EncounterAvoidanceMethod.TalkDown,
                _ => 18);

            Assert.True(context.CanTalk);
            Assert.True(result.Success);
            Assert.Equal(100, EncounterAvoidanceRules.GetEncounterXp(new[] { monster }, result.XpMultiplier));
        }

        [Fact]
        public void EncounterAvoidanceRequiresExplicitReasonableMonsterFlag()
        {
            var party = CreateParty(new Hero
            {
                Charisma = 16,
                SkillProficiencies = new List<string> { "Persuasion" }
            });
            var monster = CreateMonster("Silent Guard", Biome.Cave, 2, Rarity.Common);
            monster.Intelligence = 12;
            monster.Languages = "Common";

            var context = EncounterAvoidanceRules.CreateContext(party, new[] { monster }, _ => 20);

            Assert.False(context.CanTalk);
        }

        [Fact]
        public void EncounterAvoidanceLeavesNonAggressiveCreaturesWithoutXp()
        {
            var party = CreateParty(new Hero());
            var monster = CreateMonster("Deer", Biome.Forest, 1, Rarity.Common);
            monster.MonsterType = "beast";
            monster.Alignment = "unaligned";
            monster.NonAggressive = true;
            monster.Xp = 25;
            var context = EncounterAvoidanceRules.CreateContext(party, new[] { monster }, _ => 1);

            var result = EncounterAvoidanceRules.Resolve(
                party,
                new[] { monster },
                context,
                EncounterAvoidanceMethod.LeavePeacefully,
                _ => 1);

            Assert.True(context.NonAggressive);
            Assert.True(result.Success);
            Assert.Equal(0, EncounterAvoidanceRules.GetEncounterXp(new[] { monster }, result.XpMultiplier));
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
                HitPoints = 1
            };
        }

        private static Party CreateParty(params Hero[] heroes)
        {
            var party = new Party();
            foreach (var hero in heroes)
            {
                hero.IsActive = true;
                hero.Health = hero.Health <= 0 ? 10 : hero.Health;
                hero.MaxHealth = hero.MaxHealth <= 0 ? 10 : hero.MaxHealth;
                party.Members.Add(hero);
            }

            return party;
        }
    }
}
