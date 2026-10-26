using System;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Xunit;

namespace DungeonEscape.Core.Test.Rules;

public class CampaignProjectTemplateTests
{
    [Fact]
    public void StarterCampaignIsScopedPlayableAndPeaceful()
    {
        var project = new CampaignProjectTemplate("forest-realm", "Forest Realm", "A new adventure");
        Assert.All(project.Files.Keys, path => Assert.True(ContentPaths.IsCampaignPath(path, "forest-realm")));
        var context = CampaignContentLoader.Load(project.Campaign, new DungeonEscapeDataSet(), path => project.Files.TryGetValue(path, out var text) ? text : null);
        Assert.Single(context.Maps.Maps);
        Assert.Empty(context.OverworldMonsters);
        Assert.Equal("map/forest-realm/overworld", context.Campaign.StartMapId);
        Assert.Equal("start", context.Campaign.StartSpawnId);
    }

    [Theory]
    [InlineData("base")]
    [InlineData("../outside")]
    [InlineData("Uppercase")]
    [InlineData("two words")]
    [InlineData("con")]
    [InlineData("")]
    public void InvalidOrReservedKeysCannotCreateProjects(string key)
    {
        Assert.Throws<ArgumentException>(() => new CampaignProjectTemplate(key, "Name", ""));
    }

    [Fact]
    public void RelativeMapAssetsResolveToSharedOrCampaignAssetsWithoutEscapingRoot()
    {
        const string map = "Assets/DungeonEscape/Campaigns/forest/Maps/world.tmx";
        Assert.Equal("Assets/DungeonEscape/Tilesets/overworld.tsx", ContentPaths.ResolveRelative(map, "../../../Tilesets/overworld.tsx"));
        Assert.Equal("Assets/DungeonEscape/Campaigns/forest/Tilesets/custom.tsx", ContentPaths.ResolveRelative(map, "../Tilesets/custom.tsx"));
        Assert.Throws<ArgumentException>(() => ContentPaths.ResolveRelative(map, "../../../../../../outside.tsx"));
    }
}
