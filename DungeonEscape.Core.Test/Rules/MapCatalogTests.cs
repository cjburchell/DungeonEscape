using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Xunit;

namespace DungeonEscape.Core.Test.Rules;

public class MapCatalogTests
{
    [Fact]
    public void MapIdentitySurvivesDisplayNameAndFileChanges()
    {
        var definition = CreateMap();
        var originalId = definition.Id;
        definition.Name = "Renamed Town";
        definition.AssetPath = "Assets/DungeonEscape/Maps/new-folder/renamed.tmx";
        var catalog = new MapCatalog(new[] { definition });

        Assert.Equal(definition.AssetPath, catalog.Get(originalId).AssetPath);
        Assert.Equal(originalId, catalog.Get(definition.AssetPath).Id);
        Assert.Throws<KeyNotFoundException>(() => catalog.Get("map/other/town"));
    }

    [Theory]
    [InlineData("Assets/DungeonEscape/Maps/../town.tmx")]
    [InlineData("Assets/DungeonEscape/Maps/./town.tmx")]
    [InlineData("Assets/DungeonEscape/Other/town.tmx")]
    public void CatalogRejectsPathsOutsideMapRoot(string path)
    {
        var map = CreateMap();
        map.AssetPath = path;
        Assert.Throws<ArgumentException>(() => new MapCatalog(new[] { map }));
    }

    [Fact]
    public void CatalogRejectsDuplicateIdsAndDuplicateFiles()
    {
        var first = CreateMap();
        var second = CreateMap();
        second.AssetPath = "Assets/DungeonEscape/Maps/other.tmx";
        Assert.Throws<ArgumentException>(() => new MapCatalog(new[] { first, second }));
        second.Id = "map/original/other";
        second.AssetPath = first.AssetPath;
        Assert.Throws<ArgumentException>(() => new MapCatalog(new[] { first, second }));
    }

    private static MapDefinition CreateMap() => new()
    {
        Id = "map/original/town",
        Name = "Town",
        AssetPath = "Assets/DungeonEscape/Maps/town.tmx"
    };
}
