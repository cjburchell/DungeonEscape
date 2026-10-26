using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redpoint.DungeonEscape.Data;

namespace Redpoint.DungeonEscape.Rules
{
    public static class CampaignContentResolver
    {
        public static DungeonEscapeDataSet ExtractBase(DungeonEscapeDataSet source)
        {
            return new DungeonEscapeDataSet
            {
                CustomItems = source.CustomItems.Where(item => IsBase(item.Id, "item")).ToList(),
                Monsters = source.Monsters.Where(item => IsBase(item.Id, "monster")).ToList(),
                Spells = source.Spells.Where(item => IsBase(item.Id, "spell")).ToList(),
                Skills = source.Skills.Where(item => IsBase(item.Id, "skill")).ToList(),
                Backgrounds = source.Backgrounds.Where(item => IsBase(item.Id, "background")).ToList(),
                ClassLevels = source.ClassLevels,
                Species = source.Species,
                Names = source.Names
            };
        }

        public static CampaignContentContext Resolve(CampaignDefinition campaign, DungeonEscapeDataSet shared, DungeonEscapeDataSet own, IEnumerable<MapDefinition> maps)
        {
            CampaignCatalog.Validate(campaign);
            var scope = CampaignCatalog.GetScope(campaign);
            if (shared.Quests.Any() || shared.Dialogs.Any()) throw new ArgumentException("Narrative content must belong to a campaign.");
            var data = new DungeonEscapeDataSet
            {
                CustomItems = Merge(shared.CustomItems, own.CustomItems, item => item.Id, "item", scope),
                Monsters = Merge(shared.Monsters, own.Monsters, item => item.Id, "monster", scope),
                Spells = Merge(shared.Spells, own.Spells, item => item.Id, "spell", scope),
                Skills = Merge(shared.Skills, own.Skills, item => item.Id, "skill", scope),
                Backgrounds = Merge(shared.Backgrounds, own.Backgrounds, item => item.Id, "background", scope),
                Quests = Merge(shared.Quests, own.Quests, item => item.Id, "quest", scope),
                Dialogs = Merge(shared.Dialogs, own.Dialogs, item => item.Id, "dialog", scope),
                ClassLevels = shared.ClassLevels,
                Species = shared.Species,
                Names = shared.Names
            };
            // Linking effects must not mutate definitions held by another context.
            data = JsonConvert.DeserializeObject<DungeonEscapeDataSet>(JsonConvert.SerializeObject(data));
            var catalog = new MapCatalog(maps);
            foreach (var map in catalog.Maps)
                if (!map.Id.StartsWith("map/" + scope + "/", StringComparison.Ordinal) ||
                    !ContentPaths.IsCampaignPath(map.AssetPath, scope, ".tmx") ||
                    (!string.IsNullOrEmpty(map.EncounterAssetPath) && !ContentPaths.IsCampaignPath(map.EncounterAssetPath, scope, ".json")))
                    throw new ArgumentException("Map belongs to another campaign: " + map.Id);
            catalog.Get(campaign.StartMapId);
            catalog.Get(campaign.OverworldMapId);
            ValidateReferences(data, catalog);
            var roster = campaign.OverworldMonsterIds.Select(id => data.Monsters.SingleOrDefault(monster => monster.Id == id) ??
                throw new ArgumentException("Unknown campaign encounter monster: " + id)).ToList();
            data.Link();
            return new CampaignContentContext(campaign, data, catalog, roster);
        }

        private static bool IsBase(string id, string kind) => ContentId.IsValid(id, kind) && id.StartsWith(kind + "/base/", StringComparison.Ordinal);

        private static List<T> Merge<T>(IEnumerable<T> shared, IEnumerable<T> own, Func<T, string> getId, string kind, string scope)
        {
            var result = new List<T>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in new[] { (Values: shared, Scope: "base"), (Values: own, Scope: scope) })
                foreach (var value in group.Values)
                {
                    var id = getId(value);
                    if (!ContentId.IsValid(id, kind) || !id.StartsWith(kind + "/" + group.Scope + "/", StringComparison.Ordinal) || !ids.Add(id))
                        throw new ArgumentException("Invalid scope or duplicate content ID: " + id);
                    result.Add(value);
                }
            return result;
        }

        private static void ValidateReferences(DungeonEscapeDataSet data, MapCatalog maps)
        {
            var sets = new Dictionary<string, HashSet<string>>
            {
                ["item"] = new HashSet<string>(data.CustomItems.Select(item => item.Id), StringComparer.Ordinal),
                ["monster"] = new HashSet<string>(data.Monsters.Select(item => item.Id), StringComparer.Ordinal),
                ["spell"] = new HashSet<string>(data.Spells.Select(item => item.Id), StringComparer.Ordinal),
                ["skill"] = new HashSet<string>(data.Skills.Select(item => item.Id), StringComparer.Ordinal),
                ["quest"] = new HashSet<string>(data.Quests.Select(item => item.Id), StringComparer.Ordinal),
                ["map"] = new HashSet<string>(maps.Maps.Select(item => item.Id), StringComparer.Ordinal)
            };
            var fields = new Dictionary<string, string>
            {
                ["Quest"] = "quest", ["QuestId"] = "quest", ["ItemId"] = "item", ["Item"] = "item",
                ["MapId"] = "map", ["Monster"] = "monster", ["Skill"] = "skill"
            };
            foreach (var property in JObject.FromObject(data).Descendants().OfType<JProperty>())
            {
                string kind;
                if (property.Value.Type == JTokenType.String && fields.TryGetValue(property.Name, out kind))
                    Check(sets[kind], (string)property.Value);
                else if ((property.Name == "Items" || property.Name == "Spells") && property.Value is JArray array)
                    foreach (var value in array.Where(value => value.Type == JTokenType.String))
                        Check(sets[property.Name == "Items" ? "item" : "spell"], (string)value);
            }
        }

        private static void Check(HashSet<string> ids, string id)
        {
            if (!string.IsNullOrEmpty(id) && id != "#Random#" && !ids.Contains(id))
                throw new ArgumentException("Unresolved campaign content reference: " + id);
        }
    }
}
