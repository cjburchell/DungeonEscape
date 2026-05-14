using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Redpoint.DungeonEscape.State;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Core
{
    public sealed class GameDataCache
    {
        public static GameDataCache Current { get; private set; }

        private readonly DungeonEscapeDataSet dataSet;

        private GameDataCache(DungeonEscapeDataSet dataSet)
        {
            this.dataSet = dataSet ?? new DungeonEscapeDataSet();
        }

        public IList<ItemDefinition> ItemDefinitions
        {
            get { return dataSet.ItemDefinitions; }
        }

        public IList<Item> CustomItems
        {
            get { return dataSet.CustomItems; }
        }

        public IList<Skill> Skills
        {
            get { return dataSet.Skills; }
        }

        public IList<Spell> Spells
        {
            get { return dataSet.Spells; }
        }

        public IList<Monster> Monsters
        {
            get { return dataSet.Monsters; }
        }

        public IList<StatName> StatNames
        {
            get { return dataSet.StatNames; }
        }

        public Names Names
        {
            get { return dataSet.Names; }
        }

        public IList<ClassStats> ClassLevels
        {
            get { return dataSet.ClassLevels; }
        }

        public IList<Quest> Quests
        {
            get { return dataSet.Quests; }
        }

        public static void Load(DungeonEscapeDataSet dataSet)
        {
            Current = new GameDataCache(dataSet);
        }

        public bool TryGetDialogText(string dialogId, Party party, out string text)
        {
            text = null;
            DialogText dialogHead;
            if (!TryGetDialog(dialogId, party, out dialogHead))
            {
                return false;
            }

            text = BuildDialogText(dialogHead);
            return true;
        }

        public bool TryGetDialog(string dialogId, Party party, out DialogText dialogText)
        {
            dialogText = null;
            var dialog = dataSet.Dialogs == null
                ? null
                : dataSet.Dialogs.FirstOrDefault(item => string.Equals(item.Id, dialogId, StringComparison.OrdinalIgnoreCase));

            if (dialog == null || dialog.Dialogs == null)
            {
                return false;
            }

            var dialogHead = GetDialogHead(dialog, party);
            if (dialogHead == null || string.IsNullOrEmpty(dialogHead.Text))
            {
                return false;
            }

            dialogText = dialogHead;
            return true;
        }

        public bool TryGetCustomItem(string itemId, out Item item)
        {
            item = dataSet.CustomItems == null
                ? null
                : dataSet.CustomItems.FirstOrDefault(value =>
                    string.Equals(value.Id, itemId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value.Name, itemId, StringComparison.OrdinalIgnoreCase));

            return item != null;
        }

        public bool TryGetQuest(string questId, out Quest quest)
        {
            quest = dataSet.Quests == null
                ? null
                : dataSet.Quests.FirstOrDefault(value =>
                    string.Equals(value.Id, questId, StringComparison.OrdinalIgnoreCase));

            return quest != null;
        }


        private static DialogHead GetDialogHead(Dialog dialog, Party party)
        {
            var dialogs = dialog.Dialogs;
            var startQuestDialog = dialogs.FirstOrDefault(item =>
                item.StartQuest &&
                party != null &&
                party.ActiveQuests.All(quest => item.Quest != quest.Id));

            if (startQuestDialog != null)
            {
                return startQuestDialog;
            }

            if (party != null)
            {
                foreach (var activeQuest in party.ActiveQuests)
                {
                    if (activeQuest.Completed)
                    {
                        continue;
                    }

                    var questDialog = dialogs.FirstOrDefault(item =>
                        item.Quest == activeQuest.Id &&
                        GetDialogQuestStages(item).Contains(activeQuest.CurrentStage));

                    if (questDialog != null)
                    {
                        return questDialog;
                    }
                }
            }

            return GetDefaultDialogHead(dialogs);
        }

        private static DialogHead GetDefaultDialogHead(IEnumerable<DialogHead> dialogs)
        {
            return dialogs.FirstOrDefault(item =>
                       !item.StartQuest &&
                       string.IsNullOrEmpty(item.Quest) &&
                       !GetDialogQuestStages(item).Any()) ??
                   dialogs.FirstOrDefault();
        }

        private static IEnumerable<int> GetDialogQuestStages(DialogHead dialog)
        {
            if (dialog == null)
            {
                return Enumerable.Empty<int>();
            }

            return dialog.QuestStage ?? dialog.ForQuestStage ?? Enumerable.Empty<int>();
        }

        private static string BuildDialogText(DialogText dialog)
        {
            var text = new StringBuilder(dialog.Text);
            if (dialog.Choices == null || dialog.Choices.Count == 0)
            {
                return text.ToString();
            }

            var visibleChoices = dialog.Choices
                .Where(choice => !string.IsNullOrEmpty(choice.Text))
                .Select(choice => choice.Text)
                .ToList();

            if (visibleChoices.Count == 0)
            {
                return text.ToString();
            }

            text.AppendLine();
            text.AppendLine();
            text.Append(string.Join(" / ", visibleChoices.ToArray()));
            return text.ToString();
        }
    }
}
