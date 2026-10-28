using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules;

public class QuestObjectiveRulesTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EitherOrderSurvivesReloadAndAllowsOnlyOneReturnReward(bool reverse)
    {
        var quest = Quest(); var dialog = Dialog(quest); var party = Party();
        QuestObjectiveRules.Validate(new[] { quest }, new[] { dialog });
        Assert.Equal("Offer", DialogRules.SelectHead(dialog, party).Text);
        QuestRules.StartQuest(party, quest, out _);
        QuestRules.AdvanceQuest(party, quest, 1, null, null, null, out _);
        Assert.Equal("Neither", DialogRules.SelectHead(dialog, party).Text);
        var first = quest.Objectives[reverse ? 1 : 0]; var second = quest.Objectives[reverse ? 0 : 1];
        Assert.False(QuestObjectiveRules.RecordEncounterVictory(party, new[] { quest }, "map/test/other", first.EncounterId));
        Assert.True(QuestObjectiveRules.RecordEncounterVictory(party, new[] { quest }, first.MapId, first.EncounterId));
        Assert.False(QuestObjectiveRules.RecordEncounterVictory(party, new[] { quest }, first.MapId, first.EncounterId));
        Assert.Equal(reverse ? "Second only" : "First only", DialogRules.SelectHead(dialog, party).Text);
        party = JsonConvert.DeserializeObject<Party>(JsonConvert.SerializeObject(party));
        var active = Assert.Single(party.ActiveQuests);
        Assert.Single(active.CompletedObjectiveIds);
        QuestRules.AdvanceQuest(party, quest, 3, null, null, null, out var earlyReward);
        Assert.False(earlyReward); Assert.Equal(1, active.CurrentStage); Assert.Equal(10, party.Gold); Assert.Equal(0UL, party.Members[0].Xp);
        QuestRules.AdvanceQuest(party, quest, 2, null, null, null, out var earlyReady);
        Assert.False(earlyReady);
        Assert.True(QuestObjectiveRules.RecordEncounterVictory(party, new[] { quest }, second.MapId, second.EncounterId));
        Assert.Equal(2, active.CurrentStage); Assert.False(active.Completed);
        Assert.Equal("Ready", DialogRules.SelectHead(dialog, party).Text);
        Assert.Equal(10, party.Gold);
        var items = 0;
        QuestRules.AdvanceQuest(party, quest, 3, null, null, _ => { items++; return "item"; }, out var collected);
        Assert.True(collected); Assert.True(active.Completed); Assert.Equal(30, party.Gold); Assert.Equal(50UL, party.Members[0].Xp); Assert.Equal(1, items);
        QuestRules.AdvanceQuest(party, quest, 3, null, null, _ => { items++; return "item"; }, out var repeated);
        Assert.False(repeated); Assert.Equal(30, party.Gold); Assert.Equal(50UL, party.Members[0].Xp); Assert.Equal(1, items);
        party = JsonConvert.DeserializeObject<Party>(JsonConvert.SerializeObject(party));
        Assert.Equal("Done", DialogRules.SelectHead(dialog, party).Text);
        Assert.False(QuestObjectiveRules.RecordEncounterVictory(party, new[] { quest }, first.MapId, first.EncounterId));
    }

    [Fact]
    public void GroupsClearedBeforeAcceptanceKeepProgressWithoutAutomaticallyPaying()
    {
        var quest = Quest(); var party = Party();
        foreach (var objective in quest.Objectives)
            QuestObjectiveRules.RecordEncounterVictory(party, new[] { quest }, objective.MapId, objective.EncounterId);
        QuestRules.StartQuest(party, quest, out var startedAgain);
        Assert.False(startedAgain); Assert.Equal(2, Assert.Single(party.ActiveQuests).CompletedObjectiveIds.Count);
        Assert.Equal("Ready", DialogRules.SelectHead(Dialog(quest), party).Text);
        Assert.Equal(10, party.Gold); Assert.Equal(0UL, party.Members[0].Xp);
    }

    [Theory]
    [InlineData("duplicate-id")]
    [InlineData("duplicate-placement")]
    [InlineData("wrong-scope")]
    [InlineData("ready-is-complete")]
    [InlineData("missing-ready")]
    [InlineData("missing-id")]
    [InlineData("invalid-quest-id")]
    public void InvalidObjectiveDefinitionsAreRejected(string invalid)
    {
        var quest = Quest();
        switch (invalid)
        {
            case "duplicate-id": quest.Objectives[1].Id = quest.Objectives[0].Id; break;
            case "duplicate-placement": quest.Objectives[1].EncounterId = quest.Objectives[0].EncounterId; break;
            case "wrong-scope": quest.Objectives[0].MapId = "map/other/cave"; break;
            case "ready-is-complete": quest.Stages.Single(stage => stage.Number == 2).CompleteQuest = true; break;
            case "missing-ready": quest.ObjectivesCompleteStage = null; break;
            case "missing-id": quest.Objectives[0].Id = ""; break;
            case "invalid-quest-id": quest.Id = ""; break;
        }
        Assert.Throws<ArgumentException>(() => QuestObjectiveRules.Validate(new[] { quest }, Array.Empty<Dialog>()));
    }

    [Fact]
    public void MissingPlacementsAndInvalidDialogueConditionsAreRejected()
    {
        var quest = Quest(); var dialog = Dialog(quest);
        Assert.Throws<ArgumentException>(() => QuestObjectiveRules.ValidatePlacements(new[] { quest }, (_, _) => false));
        QuestObjectiveRules.ValidatePlacements(new[] { quest }, (map, encounter) => quest.Objectives.Any(objective => objective.MapId == map && objective.EncounterId == encounter));
        dialog.Dialogs[1].RequiredObjectives.Add("unknown");
        Assert.Throws<ArgumentException>(() => QuestObjectiveRules.Validate(new[] { quest }, new[] { dialog }));
        dialog.Dialogs[1].RequiredObjectives.Remove("unknown"); dialog.Dialogs[1].MissingObjectives.Add("first");
        Assert.Throws<ArgumentException>(() => QuestObjectiveRules.Validate(new[] { quest }, new[] { dialog }));
    }

    [Fact]
    public void PrematureTurnInDoesNotCreateAQuestOrGrantRewards()
    {
        var party = Party(); QuestRules.AdvanceQuest(party, Quest(), 3, null, null, null, out var changed);
        Assert.False(changed); Assert.Empty(party.ActiveQuests); Assert.Equal(10, party.Gold);
    }

    private static Party Party()
    {
        var party = new Party { Gold = 10 };
        party.Members.Add(new Hero { Name = "Hero", IsActive = true, Health = 10, MaxHealth = 10, Level = 1 });
        return party;
    }

    private static Quest Quest() => new()
    {
        Id = "quest/test/clear", Name = "Clear both groups", Xp = 50, Gold = 20, Items = new() { "item/test/reward" },
        ObjectivesCompleteStage = 2,
        Objectives = new() {
            new() { Id = "first", Description = "First group", MapId = "map/test/cave", EncounterId = "encounter/test/first" },
            new() { Id = "second", Description = "Second group", MapId = "map/test/cave", EncounterId = "encounter/test/second" } },
        Stages = new() { new() { Number = 0 }, new() { Number = 1 }, new() { Number = 2 }, new() { Number = 3, CompleteQuest = true } }
    };

    private static Dialog Dialog(Quest quest) => new()
    {
        Id = "dialog/test/warden", Dialogs = new() {
            new() { Quest = quest.Id, StartQuest = true, Text = "Offer" },
            new() { Quest = quest.Id, QuestCompleted = false, RequiredObjectives = new() { "first" }, MissingObjectives = new() { "second" }, Text = "First only" },
            new() { Quest = quest.Id, QuestCompleted = false, RequiredObjectives = new() { "second" }, MissingObjectives = new() { "first" }, Text = "Second only" },
            new() { Quest = quest.Id, QuestCompleted = false, RequiredObjectives = new() { "first", "second" }, Text = "Ready" },
            new() { Quest = quest.Id, QuestCompleted = false, MissingObjectives = new() { "first", "second" }, Text = "Neither" },
            new() { Quest = quest.Id, QuestCompleted = true, Text = "Done" },
            new() { Text = "Default" } }
    };
}
