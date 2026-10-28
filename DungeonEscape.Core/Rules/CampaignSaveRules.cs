using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class CampaignSaveRules
    {
        public static GameSave GetQuickSave(GameFile file, string campaignId = null)
        {
            return file?.Saves?.Where(save => save != null && save.IsQuick && GameSaveFormatter.IsUsableSave(save) &&
                (campaignId == null || save.CampaignId == campaignId))
                .OrderByDescending(save => save.Time ?? DateTime.MinValue).FirstOrDefault();
        }

        public static void UpsertQuickSave(GameFile file, GameSave save)
        {
            if (!ContentId.IsValid(save?.CampaignId, "campaign")) throw new ArgumentException("A quick save must identify its campaign.");
            if (file.Saves == null) file.Saves = new List<GameSave>();
            file.Saves.RemoveAll(existing => existing != null && existing.IsQuick && existing.CampaignId == save.CampaignId);
            save.IsQuick = true;
            file.Saves.Add(save);
        }

        public static void Validate(GameSave save, CampaignContentContext context)
        {
            if (!GameSaveFormatter.IsUsableSave(save) || save.CampaignId != context.Campaign.Id)
                throw new ArgumentException("The save does not belong to this campaign.");
            if (save.CampaignContentVersion != context.Campaign.ContentVersion)
                throw new ArgumentException("This save requires campaign content version " + save.CampaignContentVersion +
                    "; installed version is " + context.Campaign.ContentVersion + ".");
            var scope = CampaignCatalog.GetScope(context.Campaign);
            if (!ContentId.IsValid(save.Party.CurrentMapId, "map")) throw new ArgumentException("Invalid current map identity in save.");
            context.Maps.Get(save.Party.CurrentMapId);
            foreach (var map in save.MapStates ?? new List<MapState>())
            {
                if (!ContentId.IsValid(map.Id, "map")) throw new ArgumentException("Invalid saved map identity.");
                context.Maps.Get(map.Id);
                foreach (var encounterId in map.DefeatedEncounterIds ?? new List<string>())
                    if (!ContentId.IsValid(encounterId, "encounter") || encounterId.Split('/')[1] != scope)
                        throw new ArgumentException("Invalid defeated encounter identity in save: " + encounterId);
            }
            var sets = new Dictionary<string, HashSet<string>>
            {
                ["quest"] = Ids(context.Data.Quests.Select(item => item.Id)),
                ["skill"] = Ids(context.Data.Skills.Select(item => item.Id)),
                ["spell"] = Ids(context.Data.Spells.Select(item => item.Id)),
                ["monster"] = Ids(context.Data.Monsters.Select(item => item.Id)),
                ["background"] = Ids(context.Data.Backgrounds.Select(item => item.Id))
            };
            foreach (var active in save.Party.ActiveQuests ?? new List<ActiveQuest>())
            {
                var quest = context.Data.Quests.FirstOrDefault(item => item.Id == active.Id);
                var completed = active.CompletedObjectiveIds ?? new List<string>();
                if (completed.Count != completed.Distinct(StringComparer.Ordinal).Count() || completed.Any(id => quest?.Objectives?.Any(objective => objective.Id == id) != true))
                    throw new ArgumentException("Invalid saved quest objective progress: " + active.Id);
                if (quest?.Objectives?.Count > 0 && active.Completed && !QuestObjectiveRules.AllComplete(quest, active))
                    throw new ArgumentException("Completed quest is missing required objectives: " + active.Id);
            }
            foreach (var property in JObject.FromObject(save).Descendants().OfType<JProperty>())
            {
                if (property.Value.Type == JTokenType.String &&
                    (property.Name == "Id" || property.Name == "Skill" || property.Name == "QuestId" || property.Name == "Background"))
                    ValidateId((string)property.Value, scope, sets, context,
                        property.Name == "Skill" ? "skill" : property.Name == "QuestId" ? "quest" : property.Name == "Background" ? "background" : null);
                else if ((property.Name == "PreparedSpells" || property.Name == "Skills") && property.Value is JArray array)
                    foreach (var value in array.Where(value => value.Type == JTokenType.String)) ValidateId((string)value, scope, sets, context, property.Name == "PreparedSpells" ? "spell" : "skill");
                else if ((property.Name == "MapId" || property.Name == "SavedMapId") && property.Value.Type == JTokenType.String &&
                         !string.IsNullOrEmpty((string)property.Value))
                {
                    if (!ContentId.IsValid((string)property.Value, "map")) throw new ArgumentException("Invalid saved map reference.");
                    context.Maps.Get((string)property.Value);
                }
            }
        }

        private static void ValidateId(string id, string scope, Dictionary<string, HashSet<string>> sets, CampaignContentContext context, string expectedKind = null)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (expectedKind != null && !ContentId.IsValid(id, expectedKind)) throw new ArgumentException("Invalid saved " + expectedKind + " reference: " + id);
            if (!id.Contains("/")) return; // Instance GUIDs and local object identities have separate contracts.
            if (!ContentId.IsValid(id)) throw new ArgumentException("Invalid content reference in save: " + id);
            var parts = id.Split('/');
            if (parts[1] != "base" && parts[1] != scope) throw new ArgumentException("The save references another campaign: " + id);
            if (parts[0] == "map") context.Maps.Get(id);
            HashSet<string> ids;
            if (sets.TryGetValue(parts[0], out ids) && !ids.Contains(id)) throw new ArgumentException("Missing content required by save: " + id);
            // Items store embedded definitions, including generated starting gear absent from static catalogs.
        }

        private static HashSet<string> Ids(IEnumerable<string> ids) => new HashSet<string>(ids, StringComparer.Ordinal);
    }
}
