using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;

namespace Redpoint.DungeonEscape.Rules
{
    public static class CampaignContentLoader
    {
        // The reader returns null for absent optional files and throws for other IO failures.
        public static CampaignContentContext Load(CampaignDefinition campaign, DungeonEscapeDataSet shared, Func<string, string> readAsset)
        {
            CampaignCatalog.Validate(campaign);
            var own = new DungeonEscapeDataSet
            {
                CustomItems = ReadList<Item>(campaign.ItemsAssetPath, readAsset),
                Monsters = string.IsNullOrEmpty(campaign.MonstersAssetPath) ? new List<Monster>() : ReadList<Monster>(campaign.MonstersAssetPath, readAsset),
                Quests = ReadList<Quest>(campaign.QuestsAssetPath, readAsset),
                Dialogs = ReadList<Dialog>(campaign.DialogsAssetPath, readAsset)
            };
            var context = CampaignContentResolver.Resolve(campaign, shared, own, ReadList<MapDefinition>(campaign.MapsAssetPath, readAsset));
            ValidateMaps(context, readAsset);
            return context;
        }

        private static List<T> ReadList<T>(string path, Func<string, string> readAsset)
        {
            var json = readAsset(path) ?? throw new ArgumentException("Campaign content file is missing: " + path);
            return JsonConvert.DeserializeObject<List<T>>(json) ?? throw new ArgumentException("Campaign content is not an array: " + path);
        }

        private static void ValidateMaps(CampaignContentContext context, Func<string, string> readAsset)
        {
            var fields = new Dictionary<string, HashSet<string>>
            {
                ["ItemId"] = Ids(context.Data.CustomItems.Select(item => item.Id).Concat(new[] { "#Random#" })),
                ["KeyItemId"] = Ids(context.Data.CustomItems.Select(item => item.Id)),
                ["Dialog"] = Ids(context.Data.Dialogs.Select(item => item.Id)),
                ["QuestId"] = Ids(context.Data.Quests.Select(item => item.Id)),
                ["WarpMap"] = Ids(context.Maps.Maps.Select(item => item.Id)),
                ["Background"] = Ids(context.Data.Backgrounds.Select(item => item.Id))
            };
            var monsterIds = Ids(context.Data.Monsters.Select(item => item.Id));
            var placements = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var definition in context.Maps.Maps)
            {
                var xml = readAsset(definition.AssetPath) ?? throw new ArgumentException("Campaign map is missing: " + definition.AssetPath);
                var map = TiledMapInfo.Parse(xml);
                placements[definition.Id] = Ids(map.ObjectGroups.SelectMany(group => group.Objects)
                    .Where(item => StaticEncounterRules.IsEncounter(item.Class)).Select(item => StaticEncounterRules.Get(item.Properties, "EncounterId")));
                StaticEncounterRules.ValidateMap(definition.Id,
                    map.ObjectGroups.SelectMany(group => group.Objects).Where(item => StaticEncounterRules.IsEncounter(item.Class)).Select(item => (IReadOnlyDictionary<string, string>)item.Properties),
                    context.Data.Monsters, context.Data.Quests);
                if (map.Width <= 0 || map.Height <= 0 || map.TileWidth <= 0 || map.TileHeight <= 0)
                    throw new ArgumentException("Campaign map has invalid dimensions: " + definition.Id);
                if (definition.Id == context.Campaign.StartMapId)
                {
                    if (context.Campaign.StartColumn >= map.Width || context.Campaign.StartRow >= map.Height)
                        throw new ArgumentException("Campaign starting coordinates are outside the map.");
                    if (!string.IsNullOrEmpty(context.Campaign.StartSpawnId) && !map.ObjectGroups.SelectMany(group => group.Objects)
                        .Any(item => item.Class == "Spawn" && item.Name == context.Campaign.StartSpawnId))
                        throw new ArgumentException("Campaign starting spawn is missing: " + context.Campaign.StartSpawnId);
                }
                if (definition.Id == context.Campaign.OverworldMapId && map.Class != "Overworld")
                    throw new ArgumentException("Campaign overworld map must have class Overworld.");
                foreach (var item in map.ObjectGroups.SelectMany(group => group.Objects))
                    foreach (var property in item.Properties)
                    {
                        HashSet<string> ids;
                        if (fields.TryGetValue(property.Key, out ids) && !string.IsNullOrEmpty(property.Value) && !ids.Contains(property.Value))
                            throw new ArgumentException("Unresolved reference in " + definition.Id + ": " + property.Value);
                    }
                if (!string.IsNullOrEmpty(definition.EncounterAssetPath))
                {
                    var encounters = readAsset(definition.EncounterAssetPath);
                    if (encounters == null) continue; // Some registered maps intentionally have no encounter file yet.
                    foreach (var monster in JsonConvert.DeserializeObject<List<RandomMonster>>(encounters) ?? new List<RandomMonster>())
                        if (monster.MonsterId == null || !monsterIds.Contains(monster.MonsterId))
                            throw new ArgumentException("Unknown map encounter monster in " + definition.Id + ": " + monster.MonsterId);
                }
            }
            QuestObjectiveRules.ValidatePlacements(context.Data.Quests,
                (mapId, encounterId) => placements.ContainsKey(mapId) && placements[mapId].Contains(encounterId));
        }

        private static HashSet<string> Ids(IEnumerable<string> values) => new HashSet<string>(values, StringComparer.Ordinal);
    }
}
