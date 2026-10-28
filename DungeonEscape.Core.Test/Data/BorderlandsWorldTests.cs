using System.Xml.Linq;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Xunit;

namespace DungeonEscape.Core.Test.Data;

public class BorderlandsWorldTests
{
    private readonly string root = CampaignRoot();

    [Fact]
    public void WorldHasFourteenMapsAndOnlyTheWildernessIsTheOverworld()
    {
        var manifest = JsonConvert.DeserializeObject<CampaignDefinition>(File.ReadAllText(Path.Combine(root, "campaign.json")));
        var maps = Maps();
        Assert.Equal(14, maps.Count);
        Assert.Equal("map/borderlands/keep", manifest.StartMapId);
        Assert.Equal("map/borderlands/overworld", manifest.OverworldMapId);
        Assert.Equal(new[] { "overworld" }, maps.Where(pair => (string)pair.Value.Root.Attribute("class") == "Overworld").Select(pair => pair.Key));
        Assert.Equal(11, maps.Keys.Count(key => key == "cave" || key.StartsWith("cave-") && key != "cave-entrance"));
    }

    [Fact]
    public void EveryEntranceHasAWalkableNamedReturnAndTheWholeWorldIsConnected()
    {
        var maps = Maps();
        var edges = new Dictionary<string, HashSet<string>>();
        foreach (var (key, xml) in maps)
        {
            var info = TiledMapInfo.Parse(xml.ToString());
            var objects = info.ObjectGroups.SelectMany(group => group.Objects).ToList();
            var spawns = objects.Where(item => item.Class == "Spawn").ToList();
            Assert.Single(spawns, item => item.Properties.TryGetValue("DefaultSpawn", out var value) && value == "true");
            Assert.Equal(spawns.Count, spawns.Select(item => item.Name).Distinct().Count());
            var blocked = Blocked(xml, info);
            var start = spawns.Single(item => item.Name == "start");
            var reachable = Flood((int)(start.X / 32), (int)(start.Y / 32), info.Width, info.Height, blocked);
            edges[key] = new();
            foreach (var spawn in spawns)
            {
                var index = (int)(spawn.Y / 32) * info.Width + (int)(spawn.X / 32);
                Assert.Contains(index, reachable);
                Assert.DoesNotContain(objects.Where(item => item.Class == "Warp"), warp => Bounds(warp, info).Contains(index));
            }
            foreach (var warp in objects.Where(item => item.Class == "Warp"))
            {
                Assert.True(Bounds(warp, info).Any(reachable.Contains), key + ": unreachable warp " + warp.Name);
                var target = warp.Properties["WarpMap"].Split('/').Last();
                edges[key].Add(target);
                Assert.True(maps.ContainsKey(target), key + ": missing map " + target);
                var targetInfo = TiledMapInfo.Parse(maps[target].ToString());
                var targetObjects = targetInfo.ObjectGroups.SelectMany(group => group.Objects).ToList();
                Assert.Single(targetObjects, item => item.Class == "Spawn" && item.Name == warp.Properties["SpawnId"]);
                Assert.Contains(targetObjects, item => item.Class == "Warp" && item.Properties["WarpMap"] == "map/borderlands/" + key);
            }
            // Reject accidentally disconnected rooms, not just dangling warp references.
            var walkable = Enumerable.Range(0, info.Width * info.Height).Where(index => !blocked.Contains(index));
            Assert.True(walkable.All(reachable.Contains), key + ": disconnected walkable room or terrain pocket");
        }
        var visited = new HashSet<string>(); var queue = new Queue<string>(); queue.Enqueue("keep");
        while (queue.Count > 0) { var key = queue.Dequeue(); if (visited.Add(key)) foreach (var target in edges[key]) queue.Enqueue(target); }
        Assert.Equal(14, visited.Count);
        Assert.Equal(new[] { "overworld" }, edges["keep"]);
        Assert.Equal(12, edges["cave-entrance"].Count);
    }

    [Fact]
    public void KoboldObjectiveKeepsItsIdentityAndAllElevenLairsHaveDistinctRoomPlans()
    {
        var maps = Maps();
        var caves = maps.Where(pair => pair.Key == "cave" || pair.Key.StartsWith("cave-") && pair.Key != "cave-entrance").ToList();
        Assert.Equal(11, caves.Select(pair => pair.Value.Root.Elements("layer").Single(layer => (string)layer.Attribute("name") == "walls").Value).Distinct().Count());
        var info = TiledMapInfo.Parse(maps["cave"].ToString());
        var groups = info.ObjectGroups.SelectMany(item => item.Objects).Where(item => item.Class == "Monster").ToList();
        Assert.Equal(2, groups.Count);
        var group = groups.Single(item => StaticEncounterRules.Get(item.Properties, "EncounterId") == "encounter/borderlands/entrance-kobolds");
        var encounter = StaticEncounterRules.Read(group.Properties);
        Assert.Equal("encounter/borderlands/entrance-kobolds", encounter.Id);
        Assert.Equal(4, encounter.Count);
        Assert.Null(encounter.VictoryQuestStage); // Objective events, rather than an individual group, now control readiness.
        var wilderness = maps["overworld"].Root.Elements("layer").Single(layer => (string)layer.Attribute("name") == "biomes");
        Assert.Equal("0", (string)wilderness.Attribute("visible"));
        var biomeFirstGid = (int)maps["overworld"].Root.Elements("tileset")
            .Single(tile => ((string)tile.Attribute("source")).EndsWith("/biomes.tsx")).Attribute("firstgid");
        var biomeData = TiledTileData.ParseCsvTileData(wilderness.Element("data").Value);
        Assert.Equal((int)wilderness.Attribute("width") * (int)wilderness.Attribute("height"), biomeData.Count);
        Assert.All(biomeData, gid => Assert.InRange(gid - biomeFirstGid, 0, 5));
        foreach (var biome in new[] { 0, 1, 2, 3, 5 }) Assert.Contains(biomeFirstGid + biome, biomeData);
        // Representative painted regions after the map expansion: forest, grass, fens and river.
        var width = (int)wilderness.Attribute("width");
        Assert.Equal(biomeFirstGid + 1, biomeData[3 * width + 3]);
        Assert.Equal(biomeFirstGid, biomeData[55 * width + 3]);
        Assert.Equal(biomeFirstGid + 5, biomeData[80 * width + 65]);
        Assert.Equal(biomeFirstGid + 2, biomeData[65 * width + 15]);
    }

    private Dictionary<string, XDocument> Maps() => Directory.EnumerateFiles(Path.Combine(root, "Maps"), "*.tmx")
        .ToDictionary(Path.GetFileNameWithoutExtension, XDocument.Load);

    private static HashSet<int> Blocked(XDocument xml, TiledMapInfo info)
    {
        var blocked = new HashSet<int>();
        foreach (var layer in xml.Root.Elements("layer").Where(layer => layer.Element("properties")?.Elements("property").Any(property => (string)property.Attribute("name") == "Collideable" && (string)property.Attribute("value") == "true") == true))
        {
            var data = TiledTileData.ParseCsvTileData(layer.Element("data").Value);
            Assert.Equal(info.Width * info.Height, data.Count);
            for (var index = 0; index < data.Count; index++) if (data[index] != 0) blocked.Add(index);
        }
        foreach (var item in info.ObjectGroups.SelectMany(group => group.Objects).Where(item => item.Properties.TryGetValue("Collideable", out var value) && value == "true"))
            blocked.UnionWith(Bounds(item, info));
        return blocked;
    }

    private static List<int> Bounds(TiledObjectInfo item, TiledMapInfo info) => TiledTileData.GetObjectBoundsTileIndexes(item, info.TileWidth, info.TileHeight, info.Width, info.Height);

    private static HashSet<int> Flood(int x, int y, int width, int height, HashSet<int> blocked)
    {
        var visited = new HashSet<int>(); var queue = new Queue<(int x, int y)>(); queue.Enqueue((x, y));
        while (queue.Count > 0)
        {
            var position = queue.Dequeue(); var index = position.y * width + position.x;
            if (position.x < 0 || position.y < 0 || position.x >= width || position.y >= height || blocked.Contains(index) || !visited.Add(index)) continue;
            queue.Enqueue((position.x - 1, position.y)); queue.Enqueue((position.x + 1, position.y));
            queue.Enqueue((position.x, position.y - 1)); queue.Enqueue((position.x, position.y + 1));
        }
        return visited;
    }

    private static string CampaignRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "DungeonEscape.sln"))) directory = directory.Parent;
        return Path.Combine(directory.FullName, "DungeonEscape.Unity/Assets/DungeonEscape/Campaigns/borderlands");
    }
}
