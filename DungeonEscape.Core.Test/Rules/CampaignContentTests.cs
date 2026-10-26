using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules;

public class CampaignContentTests
{
    [Fact]
    public void ContextsCombineSharedDefinitionsWithOnlyTheirOwnContent()
    {
        var shared = Shared();
        var first = Resolve("first", shared, Own("first"));
        var second = Resolve("second", shared, Own("second"));

        Assert.Equal(new[] { "item/base/potion", "item/first/key" }, first.Data.CustomItems.Select(item => item.Id));
        Assert.Equal(new[] { "quest/second/find-key" }, second.Data.Quests.Select(item => item.Id));
        Assert.Single(first.OverworldMonsters);
        Assert.Equal("monster/base/guard", first.OverworldMonsters[0].Id);
        Assert.Equal(2, first.Data.Monsters.Count);
        Assert.Null(shared.CustomItems[0].Skill);
        Assert.NotNull(first.Data.CustomItems[0].Skill);
        Assert.NotSame(first.Data.CustomItems[0].Skill, second.Data.CustomItems[0].Skill);
        first.Data.CustomItems[0].Name = "Changed";
        Assert.Equal("Potion", second.Data.CustomItems[0].Name);
    }

    [Fact]
    public void OwnDefinitionsCannotOverrideBaseOrImportAnotherCampaign()
    {
        var own = Own("first");
        own.CustomItems[0].Id = "item/base/potion";
        Assert.Throws<ArgumentException>(() => Resolve("first", Shared(), own));
        own.CustomItems[0].Id = "item/second/key";
        Assert.Throws<ArgumentException>(() => Resolve("first", Shared(), own));
    }

    [Fact]
    public void ContextRejectsDuplicateIdsAndBrokenNarrativeReferences()
    {
        var own = Own("first");
        own.CustomItems.Add(own.CustomItems[0]);
        Assert.Throws<ArgumentException>(() => Resolve("first", Shared(), own));
        own = Own("first");
        own.Quests[0].Items.Add("item/second/key");
        Assert.Throws<ArgumentException>(() => Resolve("first", Shared(), own));
    }

    [Fact]
    public void CatalogRejectsDuplicateScopesUnknownDefaultAndUnsafePaths()
    {
        var first = Campaign("first");
        var duplicate = Campaign("first");
        duplicate.Id = "campaign/first/other";
        Assert.Throws<ArgumentException>(() => new CampaignCatalog(new[] { first, duplicate }, first.Id));
        Assert.Throws<ArgumentException>(() => new CampaignCatalog(new[] { first }, "campaign/missing/main"));
        first.ItemsAssetPath = "Assets/DungeonEscape/Data/../outside.json";
        Assert.Throws<ArgumentException>(() => new CampaignCatalog(new[] { first }, first.Id));
    }

    [Fact]
    public void EncounterRosterMustResolveAndMapsMustBelongToTheCampaign()
    {
        var campaign = Campaign("first");
        campaign.OverworldMonsterIds.Add("monster/second/custom");
        Assert.Throws<ArgumentException>(() => CampaignContentResolver.Resolve(campaign, Shared(), Own("first"), Maps("first")));
        Assert.Throws<ArgumentException>(() => CampaignContentResolver.Resolve(Campaign("first"), Shared(), Own("first"), Maps("second")));
    }

    [Fact]
    public void MissingCampaignFilesFailBeforePublishingAContext()
    {
        Assert.Throws<ArgumentException>(() => CampaignContentLoader.Load(Campaign("first"), Shared(), _ => null));
    }

    private static CampaignContentContext Resolve(string scope, DungeonEscapeDataSet shared, DungeonEscapeDataSet own) =>
        CampaignContentResolver.Resolve(Campaign(scope), shared, own, Maps(scope));

    private static List<MapDefinition> Maps(string scope) => new()
    {
        new MapDefinition { Id = "map/" + scope + "/world", Name = "World", AssetPath = "Assets/DungeonEscape/Campaigns/" + scope + "/Maps/world.tmx" }
    };

    private static CampaignDefinition Campaign(string scope) => new()
    {
        Id = "campaign/" + scope + "/main", Name = scope, ContentVersion = "1.0",
        StartMapId = "map/" + scope + "/world", OverworldMapId = "map/" + scope + "/world",
        ItemsAssetPath = "Assets/DungeonEscape/Campaigns/" + scope + "/Data/items.json", MapsAssetPath = "Assets/DungeonEscape/Campaigns/" + scope + "/Data/maps.json",
        QuestsAssetPath = "Assets/DungeonEscape/Campaigns/" + scope + "/Data/quests.json", DialogsAssetPath = "Assets/DungeonEscape/Campaigns/" + scope + "/Data/dialog.json",
        OverworldMonsterIds = new List<string> { "monster/base/guard" }
    };

    private static DungeonEscapeDataSet Own(string scope) => new()
    {
        CustomItems = new List<Item> { new() { Id = "item/" + scope + "/key", Name = "Key" } },
        Quests = new List<Quest> { new() { Id = "quest/" + scope + "/find-key", Items = new List<string> { "item/" + scope + "/key" } } }
    };

    private static DungeonEscapeDataSet Shared() => new()
    {
        Skills = new List<Skill> { new() { Id = "skill/base/heal", Name = "Heal", Type = SkillType.Heal } },
        CustomItems = new List<Item> { new() { Id = "item/base/potion", Name = "Potion", SkillId = "skill/base/heal" } },
        Monsters = new List<Monster> { new() { Id = "monster/base/guard" }, new() { Id = "monster/base/other" } }
    };
}
