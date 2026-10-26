using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules;

public class StaticEncounterRulesTests
{
    private static Dictionary<string, string> Properties() => new()
    {
        ["EncounterId"] = "encounter/test/guards", ["MonsterId"] = "monster/base/kobold", ["Count"] = "4",
        ["VictoryQuestId"] = "quest/test/clear-cave", ["VictoryQuestStage"] = "2"
    };

    [Fact]
    public void RosterUsesExactlyTheAuthoredCountRegardlessOfRandomGroupSettings()
    {
        var monster = new Monster { Id = "monster/base/kobold", GroupSize = 8, MinLevel = 20 };
        var roster = StaticEncounterRules.Build(StaticEncounterRules.Read(Properties()), new[] { monster });
        Assert.Equal(4, roster.Count);
        Assert.All(roster, item => Assert.Same(monster, item));
        Assert.Throws<ArgumentException>(() => StaticEncounterRules.Build(StaticEncounterRules.Read(Properties()), Array.Empty<Monster>()));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("11")]
    [InlineData("four")]
    public void InvalidCountsCannotStartCombat(string count)
    {
        var properties = Properties();
        properties["Count"] = count;
        Assert.Throws<ArgumentException>(() => StaticEncounterRules.Read(properties));
    }

    [Theory]
    [InlineData("EncounterId", "guards")]
    [InlineData("MonsterId", "Kobold")]
    [InlineData("VictoryQuestId", "")]
    [InlineData("VictoryQuestStage", "")]
    [InlineData("VictoryQuestStage", "-1")]
    public void InvalidIdentityOrPartialQuestOutcomeIsRejected(string field, string value)
    {
        var properties = Properties(); properties[field] = value;
        Assert.Throws<ArgumentException>(() => StaticEncounterRules.Read(properties));
    }

    [Fact]
    public void VictoryPersistsOnceAndOtherPlacementsAndMapsRemainAvailable()
    {
        var encounter = StaticEncounterRules.Read(Properties());
        var map = new MapState { Id = "map/test/cave" };
        Assert.False(StaticEncounterRules.IsDefeated(map, encounter.Id));
        Assert.True(StaticEncounterRules.CompleteVictory(map, encounter));
        Assert.False(StaticEncounterRules.CompleteVictory(map, encounter));
        var restored = JsonConvert.DeserializeObject<MapState>(JsonConvert.SerializeObject(map));
        Assert.True(StaticEncounterRules.IsDefeated(restored, encounter.Id));
        Assert.Single(restored.DefeatedEncounterIds);
        Assert.False(StaticEncounterRules.IsDefeated(restored, "encounter/test/other-guards"));
        Assert.False(StaticEncounterRules.IsDefeated(new MapState { Id = "map/test/other-cave" }, encounter.Id));
    }

    [Fact]
    public void MapValidationRejectsDuplicatesCrossCampaignIdsAndMissingReferences()
    {
        var properties = Properties();
        var monster = new Monster { Id = properties["MonsterId"] };
        var quest = new Quest { Id = properties["VictoryQuestId"], Stages = new() { new() { Number = 2 } } };
        void Validate(params Dictionary<string, string>[] groups) => StaticEncounterRules.ValidateMap("map/test/cave", groups, new[] { monster }, new[] { quest });
        Validate(properties);
        Assert.Throws<ArgumentException>(() => Validate(properties, properties));
        properties["EncounterId"] = "encounter/other/guards";
        Assert.Throws<ArgumentException>(() => Validate(properties));
        properties["EncounterId"] = "encounter/test/guards";
        properties["VictoryQuestStage"] = "3";
        Assert.Throws<ArgumentException>(() => Validate(properties));
        properties["VictoryQuestStage"] = "2";
        properties["MonsterId"] = "monster/base/missing";
        Assert.Throws<ArgumentException>(() => Validate(properties));
    }
}
