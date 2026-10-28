using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class QuestObjectiveRules
    {
        public static bool IsComplete(ActiveQuest active, string objectiveId) => active?.CompletedObjectiveIds?.Contains(objectiveId) == true;

        public static bool AllComplete(Quest quest, ActiveQuest active) =>
            (quest.Objectives ?? new List<QuestObjective>()).All(objective => IsComplete(active, objective.Id));

        public static bool RecordEncounterVictory(Party party, IEnumerable<Quest> quests, string mapId, string encounterId)
        {
            var changed = false;
            foreach (var quest in quests)
            {
                var matching = (quest.Objectives ?? new List<QuestObjective>()).Where(objective => objective.MapId == mapId && objective.EncounterId == encounterId).ToList();
                if (matching.Count == 0) continue;
                var active = party.ActiveQuests.FirstOrDefault(item => item.Id == quest.Id);
                if (active?.Completed == true) continue;
                if (active == null)
                {
                    active = QuestRules.CreateActiveQuest(quest);
                    party.ActiveQuests.Add(active);
                }
                if (active.CompletedObjectiveIds == null) active.CompletedObjectiveIds = new List<string>();
                foreach (var objective in matching)
                    if (!IsComplete(active, objective.Id)) { active.CompletedObjectiveIds.Add(objective.Id); changed = true; }
                if (AllComplete(quest, active) && quest.ObjectivesCompleteStage.HasValue && active.CurrentStage != quest.ObjectivesCompleteStage.Value)
                {
                    active.CurrentStage = quest.ObjectivesCompleteStage.Value;
                    changed = true;
                }
            }
            return changed;
        }

        public static void Validate(IEnumerable<Quest> quests, IEnumerable<Dialog> dialogs)
        {
            var questList = quests.ToList();
            foreach (var quest in questList)
            {
                var objectives = quest.Objectives ?? new List<QuestObjective>();
                if (objectives.Count > 0 && !ContentId.IsValid(quest.Id, "quest"))
                    throw new ArgumentException("Objective quest needs a valid quest ID: " + quest.Id);
                var ids = new HashSet<string>(StringComparer.Ordinal);
                var placements = new HashSet<string>(StringComparer.Ordinal);
                foreach (var objective in objectives)
                {
                    if (objective == null || !ContentId.IsValid("objective/local/" + objective.Id, "objective") || !ids.Add(objective.Id) || string.IsNullOrWhiteSpace(objective.Description))
                        throw new ArgumentException("Quest objectives need unique stable IDs and descriptions: " + quest.Id);
                    if (!ContentId.IsValid(objective.MapId, "map") || !ContentId.IsValid(objective.EncounterId, "encounter") || objective.MapId.Split('/')[1] != quest.Id.Split('/')[1] || objective.EncounterId.Split('/')[1] != quest.Id.Split('/')[1])
                        throw new ArgumentException("Quest objective must reference its campaign's map and encounter: " + quest.Id);
                    if (!placements.Add(objective.MapId + "|" + objective.EncounterId))
                        throw new ArgumentException("Two quest objectives cannot reference the same encounter placement: " + quest.Id);
                }
                if (objectives.Count > 0 && (!quest.ObjectivesCompleteStage.HasValue || !(quest.Stages ?? new List<QuestStage>()).Any(stage => stage.Number == quest.ObjectivesCompleteStage && !stage.CompleteQuest) || !quest.Stages.Any(stage => stage.CompleteQuest)))
                    throw new ArgumentException("Objective quests need a non-completing ready stage and a separate turn-in stage: " + quest.Id);
                if (objectives.Count == 0 && quest.ObjectivesCompleteStage.HasValue)
                    throw new ArgumentException("ObjectivesCompleteStage requires objectives: " + quest.Id);
            }
            foreach (var head in dialogs.SelectMany(dialog => dialog.Dialogs ?? new List<DialogHead>()).Where(DialogRules.HasConditions))
            {
                var quest = questList.FirstOrDefault(item => item.Id == head.Quest);
                if (quest == null || head.StartQuest) throw new ArgumentException("Conditional dialogue needs an existing quest and cannot start it: " + head.Quest);
                var ids = new HashSet<string>((quest.Objectives ?? new List<QuestObjective>()).Select(objective => objective.Id), StringComparer.Ordinal);
                var required = head.RequiredObjectives ?? new List<string>();
                var missing = head.MissingObjectives ?? new List<string>();
                if (required.Concat(missing).Any(id => !ids.Contains(id)) || required.Intersect(missing).Any())
                    throw new ArgumentException("Conditional dialogue has unknown or contradictory objective requirements: " + head.Quest);
            }
        }

        public static void ValidatePlacements(IEnumerable<Quest> quests, Func<string, string, bool> placementExists)
        {
            foreach (var quest in quests)
                foreach (var objective in quest.Objectives ?? new List<QuestObjective>())
                    if (!placementExists(objective.MapId, objective.EncounterId))
                        throw new ArgumentException("Missing quest objective encounter placement: " + quest.Id + "/" + objective.Id);
        }
    }
}
