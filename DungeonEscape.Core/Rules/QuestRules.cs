using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class QuestRules
    {
        public static string StartQuest(Party party, Quest quest, out bool changed)
        {
            changed = false;
            if (party == null || quest == null)
            {
                return "";
            }

            if (party.ActiveQuests.Any(activeQuest => activeQuest.Id == quest.Id))
            {
                return "";
            }

            party.ActiveQuests.Add(CreateActiveQuest(quest));
            changed = true;
            return "Started quest: " + quest.Name;
        }

        public static string AdvanceQuest(
            Party party,
            Quest quest,
            int? nextStage,
            IEnumerable<ClassStats> classLevels,
            IEnumerable<Spell> spells,
            Func<string, string> giveItem,
            out bool changed)
        {
            changed = false;
            if (party == null || quest == null)
            {
                return "";
            }

            var activeQuest = party.ActiveQuests.FirstOrDefault(item => item.Id == quest.Id);
            if (activeQuest == null)
            {
                activeQuest = CreateActiveQuest(quest);
                party.ActiveQuests.Add(activeQuest);
                changed = true;
            }

            if (nextStage.HasValue)
            {
                activeQuest.CurrentStage = nextStage.Value;
                changed = true;
            }

            var activeStage = activeQuest.Stages.FirstOrDefault(item => item.Number == activeQuest.CurrentStage);
            if (activeStage != null)
            {
                activeStage.Completed = true;
                changed = true;
            }

            var currentStage = quest.Stages == null
                ? null
                : quest.Stages.FirstOrDefault(item => item.Number == activeQuest.CurrentStage);

            if (currentStage == null || !currentStage.CompleteQuest || activeQuest.Completed)
            {
                return "";
            }

            activeQuest.Completed = true;
            changed = true;
            var message = new StringBuilder();
            message.AppendLine("You have completed the quest " + quest.Name);

            if (quest.Xp != 0)
            {
                AppendQuestXpReward(message, party, quest.Xp, classLevels, spells);
            }

            if (quest.Gold != 0)
            {
                party.Gold += quest.Gold;
                message.AppendLine("The party got " + quest.Gold + " gold.");
            }

            if (quest.Items != null && giveItem != null)
            {
                foreach (var itemId in quest.Items)
                {
                    message.Append(giveItem(itemId));
                }
            }

            return message.ToString().TrimEnd();
        }

        public static ActiveQuest CreateActiveQuest(Quest quest)
        {
            return new ActiveQuest
            {
                Id = quest.Id,
                CurrentStage = 0,
                Stages = quest.Stages == null
                    ? new List<QuestStageState>()
                    : quest.Stages.Select(stage => new QuestStageState
                    {
                        Number = stage.Number,
                        Completed = false
                    }).ToList()
            };
        }

        private static void AppendQuestXpReward(
            StringBuilder message,
            Party party,
            int xp,
            IEnumerable<ClassStats> classLevels,
            IEnumerable<Spell> spells)
        {
            var activeMembers = party.ActiveMembers.ToList();
            if (activeMembers.Count == 0)
            {
                return;
            }

            var xpReward = (ulong)Math.Max(0, xp);
            foreach (var hero in activeMembers)
            {
                hero.Xp += xpReward;
            }

            message.AppendLine("The party got " + xp + " XP.");
            foreach (var hero in activeMembers)
            {
                AppendLevelUpMessages(message, hero, classLevels, spells);
            }
        }

        private static void AppendLevelUpMessages(
            StringBuilder message,
            Hero hero,
            IEnumerable<ClassStats> classLevels,
            IEnumerable<Spell> spells)
        {
            if (hero == null || classLevels == null)
            {
                return;
            }

            while (true)
            {
                string levelUpMessage;
                if (!hero.CheckLevelUp(classLevels, spells, out levelUpMessage))
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(levelUpMessage))
                {
                    message.AppendLine(levelUpMessage.TrimEnd());
                }
            }
        }
    }
}
