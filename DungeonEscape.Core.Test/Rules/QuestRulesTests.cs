using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class QuestRulesTests
    {
        [Fact]
        public void StartQuestAddsActiveQuestOnce()
        {
            var party = CreateParty();
            var quest = CreateQuest(false);

            bool changed;
            var message = QuestRules.StartQuest(party, quest, out changed);
            var duplicate = QuestRules.StartQuest(party, quest, out var duplicateChanged);

            Assert.True(changed);
            Assert.False(duplicateChanged);
            Assert.Equal("Started quest: Find Gem", message);
            Assert.Equal("", duplicate);
            Assert.Single(party.ActiveQuests);
        }

        [Fact]
        public void AdvanceQuestCompletesStageAndGrantsRewardsOnce()
        {
            var party = CreateParty();
            var quest = CreateQuest(true);
            var grantedItems = new List<string>();

            bool changed;
            var message = QuestRules.AdvanceQuest(
                party,
                quest,
                1,
                CreateClassLevels(),
                CreateSpells(),
                itemId =>
                {
                    grantedItems.Add(itemId);
                    party.AddItem(new ItemInstance(new Item { Id = itemId, Name = "Gem", Type = ItemType.Quest }));
                    return "Able got Gem.\n";
                },
                out changed);

            var repeat = QuestRules.AdvanceQuest(party, quest, 1, CreateClassLevels(), CreateSpells(), id => "", out var repeatChanged);

            Assert.True(changed);
            Assert.Equal(60, party.Gold);
            Assert.Equal((ulong)10, party.ActiveMembers.Single().Xp);
            Assert.Contains("You have completed the quest Find Gem", message);
            Assert.Contains("The party got 10 XP.", message);
            Assert.Contains("The party got 50 gold.", message);
            Assert.Equal(new[] { "gem" }, grantedItems);
            Assert.Equal("", repeat);
            Assert.False(repeatChanged);
            Assert.True(party.ActiveQuests.Single().Completed);
        }

        [Fact]
        public void AdvanceQuestDoesNotChangeCompletedQuestStage()
        {
            var party = CreateParty();
            var quest = CreateQuest(true);

            QuestRules.AdvanceQuest(
                party,
                quest,
                1,
                CreateClassLevels(),
                CreateSpells(),
                _ => "",
                out _);

            var activeQuest = party.ActiveQuests.Single();
            var message = QuestRules.AdvanceQuest(
                party,
                quest,
                0,
                CreateClassLevels(),
                CreateSpells(),
                _ => "",
                out var changed);

            Assert.Equal("", message);
            Assert.False(changed);
            Assert.True(activeQuest.Completed);
            Assert.Equal(1, activeQuest.CurrentStage);
        }

        private static Party CreateParty()
        {
            var party = new Party { Gold = 10 };
            party.Members.Add(new Hero
            {
                Name = "Able",
                IsActive = true,
                Class = Class.Hero,
                Level = 1,
                NextLevel = 100,
                Health = 10,
                MaxHealth = 10,
                Items = new List<ItemInstance>()
            });
            return party;
        }

        private static Quest CreateQuest(bool complete)
        {
            return new Quest
            {
                Id = "find-gem",
                Name = "Find Gem",
                Xp = 10,
                Gold = 50,
                Items = new List<string> { "gem" },
                Stages = new List<QuestStage>
                {
                    new QuestStage { Number = 0 },
                    new QuestStage { Number = 1, CompleteQuest = complete }
                }
            };
        }

        private static List<ClassStats> CreateClassLevels()
        {
            return new List<ClassStats>
            {
                new ClassStats
                {
                    Class = Class.Hero,
                    FirstLevel = 100,
                    Skills = new List<string>(),
                    Stats = new List<Stats>
                    {
                        new Stats { Type = StatType.Health },
                        new Stats { Type = StatType.Attack },
                        new Stats { Type = StatType.Defence },
                        new Stats { Type = StatType.MagicDefence },
                        new Stats { Type = StatType.Magic },
                        new Stats { Type = StatType.Agility }
                    }
                }
            };
        }

        private static List<Spell> CreateSpells()
        {
            return new List<Spell>();
        }
    }
}
