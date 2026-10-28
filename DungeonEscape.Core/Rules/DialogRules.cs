using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class DialogRules
    {
        public static bool HasConditions(DialogHead head) => head.QuestCompleted.HasValue || head.RequiredObjectives?.Any() == true || head.MissingObjectives?.Any() == true;

        public static DialogHead SelectHead(Dialog dialog, Party party)
        {
            var heads = dialog.Dialogs ?? new List<DialogHead>();
            var conditional = heads.FirstOrDefault(head => HasConditions(head) && Matches(head, party));
            if (conditional != null) return conditional;
            var start = heads.FirstOrDefault(head => !HasConditions(head) && head.StartQuest && party != null && party.ActiveQuests.All(quest => quest.Id != head.Quest));
            if (start != null) return start;
            foreach (var active in party?.ActiveQuests ?? new List<ActiveQuest>())
            {
                if (active.Completed) continue;
                var matching = heads.FirstOrDefault(head => !HasConditions(head) && head.Quest == active.Id && Stages(head).Contains(active.CurrentStage));
                if (matching != null) return matching;
            }
            return heads.FirstOrDefault(head => !HasConditions(head) && !head.StartQuest && string.IsNullOrEmpty(head.Quest) && !Stages(head).Any()) ?? heads.FirstOrDefault(head => !HasConditions(head));
        }

        private static bool Matches(DialogHead head, Party party)
        {
            var active = party?.ActiveQuests?.FirstOrDefault(quest => quest.Id == head.Quest);
            if (active == null || (head.QuestCompleted.HasValue && head.QuestCompleted.Value != active.Completed)) return false;
            if (Stages(head).Any() && !Stages(head).Contains(active.CurrentStage)) return false;
            return (head.RequiredObjectives ?? new List<string>()).All(id => QuestObjectiveRules.IsComplete(active, id)) &&
                (head.MissingObjectives ?? new List<string>()).All(id => !QuestObjectiveRules.IsComplete(active, id));
        }

        private static IEnumerable<int> Stages(DialogHead head) => head.QuestStage ?? head.ForQuestStage ?? Enumerable.Empty<int>();
    }
}
