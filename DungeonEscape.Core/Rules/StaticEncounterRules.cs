using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class StaticEncounterRules
    {
        public static bool IsEncounter(string objectClass) => string.Equals(objectClass, "Monster", StringComparison.OrdinalIgnoreCase);

        public static StaticEncounter Read(IReadOnlyDictionary<string, string> properties)
        {
            var id = Get(properties, "EncounterId");
            var monster = Get(properties, "MonsterId");
            int count;
            if (!ContentId.IsValid(id, "encounter")) throw new ArgumentException("EncounterId must use encounter/<scope>/<key>.");
            if (!ContentId.IsValid(monster, "monster")) throw new ArgumentException("MonsterId must be a monster content ID.");
            if (!int.TryParse(Get(properties, "Count"), NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count < 1 || count > EncounterRules.MaxMonstersToFight)
                throw new ArgumentException("Encounter Count must be between 1 and " + EncounterRules.MaxMonstersToFight + ".");
            var quest = Get(properties, "VictoryQuestId");
            var stageText = Get(properties, "VictoryQuestStage");
            int stage;
            int? victoryStage = null;
            if (!string.IsNullOrEmpty(quest) || !string.IsNullOrEmpty(stageText))
            {
                if (!ContentId.IsValid(quest, "quest") || !int.TryParse(stageText, NumberStyles.Integer, CultureInfo.InvariantCulture, out stage) || stage < 0)
                    throw new ArgumentException("VictoryQuestId and a nonnegative VictoryQuestStage must be supplied together.");
                victoryStage = stage;
            }
            return new StaticEncounter { Id = id, MonsterId = monster, Count = count, VictoryQuestId = quest, VictoryQuestStage = victoryStage };
        }

        public static void ValidateMap(string mapId, IEnumerable<IReadOnlyDictionary<string, string>> groups, IEnumerable<Monster> monsters, IEnumerable<Quest> quests)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var monsterIds = new HashSet<string>(monsters.Select(monster => monster.Id), StringComparer.Ordinal);
            var questList = quests.ToList();
            var scope = mapId.Split('/')[1];
            foreach (var properties in groups)
            {
                var encounter = Read(properties);
                if (encounter.Id.Split('/')[1] != scope) throw new ArgumentException("EncounterId must belong to the map's campaign: " + encounter.Id);
                if (!ids.Add(encounter.Id)) throw new ArgumentException("Duplicate EncounterId in " + mapId + ": " + encounter.Id);
                if (!monsterIds.Contains(encounter.MonsterId)) throw new ArgumentException("Unknown encounter monster: " + encounter.MonsterId);
                if (encounter.VictoryQuestStage.HasValue && !questList.Any(quest => quest.Id == encounter.VictoryQuestId && quest.Stages != null && quest.Stages.Any(stage => stage.Number == encounter.VictoryQuestStage)))
                    throw new ArgumentException("Unknown encounter victory quest/stage: " + encounter.VictoryQuestId);
            }
        }

        public static List<Monster> Build(StaticEncounter encounter, IEnumerable<Monster> monsters)
        {
            var monster = monsters.FirstOrDefault(item => item.Id == encounter.MonsterId);
            if (monster == null || encounter.Count < 1 || encounter.Count > EncounterRules.MaxMonstersToFight)
                throw new ArgumentException("Invalid authored encounter roster.");
            return Enumerable.Repeat(monster, encounter.Count).ToList();
        }

        public static bool IsDefeated(MapState map, string encounterId) =>
            !string.IsNullOrEmpty(encounterId) && map?.DefeatedEncounterIds?.Contains(encounterId) == true;

        public static bool CompleteVictory(MapState map, StaticEncounter encounter)
        {
            if (map == null || encounter == null || !ContentId.IsValid(encounter.Id, "encounter") || IsDefeated(map, encounter.Id)) return false;
            if (map.DefeatedEncounterIds == null) map.DefeatedEncounterIds = new List<string>();
            map.DefeatedEncounterIds.Add(encounter.Id);
            return true;
        }

        public static string Get(IReadOnlyDictionary<string, string> properties, string key)
        {
            string value;
            return properties != null && properties.TryGetValue(key, out value) ? value : null;
        }
    }
}
