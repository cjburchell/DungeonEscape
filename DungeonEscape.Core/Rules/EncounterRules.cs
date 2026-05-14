using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class EncounterRules
    {
        public const int MaxMonstersToFight = 10;
        public const int MaxMonsterGroups = 3;

        public static bool CanRollRandomEncounter(
            Party party,
            BiomeInfo biomeInfo,
            IEnumerable<RandomMonster> randomMonsters,
            bool noMonsters,
            Func<double> nextDouble)
        {
            return party != null &&
                   party.AliveMembers != null &&
                   !noMonsters &&
                   biomeInfo != null &&
                   randomMonsters != null &&
                   randomMonsters.Any(monster => monster != null && monster.Data != null && monster.InBiome(biomeInfo.Type)) &&
                   (nextDouble == null ? 0d : nextDouble()) < 0.1d;
        }

        public static List<RandomMonster> CreateOverworldRandomMonsters(IEnumerable<Monster> monsters)
        {
            return (monsters ?? new List<Monster>())
                .Where(monster => monster != null && monster.Biomes != null && monster.Biomes.Any())
                .Select(monster => new RandomMonster
                {
                    Data = monster,
                    Name = monster.Name,
                    Rarity = monster.Rarity,
                    IsOverworld = true
                })
                .ToList();
        }

        public static void LinkRandomMonsters(IEnumerable<RandomMonster> randomMonsters, IEnumerable<Monster> monsters)
        {
            var monsterList = (monsters ?? new List<Monster>()).ToList();
            foreach (var randomMonster in randomMonsters ?? new List<RandomMonster>())
            {
                if (randomMonster == null || string.IsNullOrEmpty(randomMonster.Name))
                {
                    continue;
                }

                randomMonster.Data = monsterList.FirstOrDefault(monster =>
                    string.Equals(monster.Name, randomMonster.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static List<Monster> BuildRandomEncounter(
            IEnumerable<RandomMonster> randomMonsters,
            BiomeInfo biomeInfo,
            int partyLevel,
            int alivePartyCount,
            bool repelActive,
            int repelMaxHealth,
            Func<int, int> nextInt,
            Func<int> rollD20,
            Func<Monster, int> rollMonsterHealth)
        {
            if (biomeInfo == null)
            {
                return new List<Monster>();
            }

            var weightedMonsters = new List<Monster>();
            foreach (var randomMonster in FilterRandomMonsters(randomMonsters, biomeInfo))
            {
                var probability = GetMonsterProbability(randomMonster.Rarity, rollD20);
                for (var i = 0; i < probability; i++)
                {
                    weightedMonsters.Add(randomMonster.Data);
                }
            }

            if (weightedMonsters.Count == 0)
            {
                return new List<Monster>();
            }

            var maxMonsters = Math.Min(MaxMonstersToFight, Math.Max(1, partyLevel / 4 + alivePartyCount));
            var numberOfMonsters = Next(nextInt, maxMonsters) + 1;
            var monsters = new List<Monster>();
            var totalMonsters = 0;
            var usedMonsters = new List<string>();
            for (var group = 0; group < MaxMonsterGroups - 1 && totalMonsters < numberOfMonsters; group++)
            {
                var available = weightedMonsters.Where(candidate => !usedMonsters.Contains(candidate.Name)).ToArray();
                if (available.Length == 0)
                {
                    break;
                }

                var monster = available[Next(nextInt, available.Length)];
                usedMonsters.Add(monster.Name);
                var groupSize = Math.Max(1, monster.GroupSize);
                var numberInGroup = Next(nextInt, Math.Min(numberOfMonsters - totalMonsters, groupSize)) + 1;
                AddMonsterGroup(monsters, monster, numberInGroup);
                totalMonsters += numberInGroup;
            }

            if (totalMonsters < numberOfMonsters)
            {
                var available = weightedMonsters.Where(monster => !usedMonsters.Contains(monster.Name)).ToArray();
                if (available.Length > 0)
                {
                    var monster = available[Next(nextInt, available.Length)];
                    var numberInGroup = Math.Min(numberOfMonsters - totalMonsters, Math.Max(1, monster.GroupSize));
                    AddMonsterGroup(monsters, monster, numberInGroup);
                }
            }

            ApplyRepel(monsters, repelActive, repelMaxHealth, rollMonsterHealth);
            return monsters;
        }

        public static IEnumerable<RandomMonster> FilterRandomMonsters(IEnumerable<RandomMonster> randomMonsters, BiomeInfo biomeInfo)
        {
            if (biomeInfo == null)
            {
                return new List<RandomMonster>();
            }

            return (randomMonsters ?? new List<RandomMonster>()).Where(monster =>
                monster != null &&
                monster.Data != null &&
                monster.InBiome(biomeInfo.Type) &&
                (biomeInfo.MaxMonsterLevel == 0 || monster.Data.MinLevel < biomeInfo.MaxMonsterLevel) &&
                monster.Data.MinLevel >= biomeInfo.MinMonsterLevel);
        }

        public static int GetMonsterProbability(Rarity rarity, Func<int> rollD20)
        {
            switch (rarity)
            {
                case Rarity.Common:
                    return 20;
                case Rarity.Uncommon:
                    return 5;
                case Rarity.Rare:
                    return 2;
                case Rarity.Epic:
                    return 1;
                case Rarity.Legendary:
                    return (rollD20 == null ? 20 : rollD20()) > 14 ? 1 : 0;
                default:
                    return 0;
            }
        }

        public static void ApplyRepel(ICollection<Monster> monsters, bool repelActive, int maxPartyHealth, Func<Monster, int> rollMonsterHealth)
        {
            if (!repelActive || monsters == null)
            {
                return;
            }

            foreach (var monster in monsters.ToList())
            {
                var monsterHealth = rollMonsterHealth == null ? 0 : rollMonsterHealth(monster);
                if (monsterHealth < maxPartyHealth)
                {
                    monsters.Remove(monster);
                }
            }
        }

        private static void AddMonsterGroup(ICollection<Monster> monsters, Monster monster, int count)
        {
            for (var i = 0; i < count; i++)
            {
                monsters.Add(monster);
            }
        }

        private static int Next(Func<int, int> nextInt, int maxValue)
        {
            return maxValue <= 1 || nextInt == null ? 0 : nextInt(maxValue);
        }
    }
}
