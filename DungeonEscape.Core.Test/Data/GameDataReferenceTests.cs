using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using System.Xml.Linq;
using Xunit;

namespace DungeonEscape.Core.Test.Data;

public class GameDataReferenceTests
{
    [Fact]
    public void OriginalCampaignLoadsAndValidatesAllItsMapsAndContent()
    {
        var repo = FindRepositoryRoot();
        var folder = Path.Combine(repo, "DungeonEscape.Unity", "Assets", "DungeonEscape", "Data");
        var index = ReadJson<CampaignIndex>(Path.Combine(folder, "campaigns.json"));
        string ReadAsset(string assetPath)
        {
            var path = Path.Combine(repo, "DungeonEscape.Unity", assetPath);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        var catalog = new CampaignCatalog(index.Manifests.Select(path => JsonConvert.DeserializeObject<CampaignDefinition>(ReadAsset(path))), index.DefaultCampaignId);
        var source = new DungeonEscapeDataSet
        {
            CustomItems = new[] { "customitems", "magicitems", "weaponitems", "armoritems", "nonmagicitems" }
                .SelectMany(name => ReadJson<List<Item>>(Path.Combine(folder, name + ".json"))).GroupBy(item => item.Id).Select(group => group.First()).ToList(),
            Monsters = ReadJson<List<Monster>>(Path.Combine(folder, "allmonsters.json")),
            Skills = ReadJson<List<Skill>>(Path.Combine(folder, "skills.json")),
            Spells = ReadJson<List<Spell>>(Path.Combine(folder, "spells.json")),
            Backgrounds = ReadJson<List<BackgroundDefinition>>(Path.Combine(folder, "backgrounds.json"))
        };
        var context = CampaignContentLoader.Load(catalog.Default, CampaignContentResolver.ExtractBase(source), ReadAsset);
        Assert.Equal("campaign/original/main", context.Campaign.Id);
        Assert.Equal(23, context.Maps.Maps.Count);
        Assert.Equal(3, context.Data.Quests.Count);
        Assert.Equal(24, context.Data.Dialogs.Count);
        Assert.Equal(48, context.OverworldMonsters.Count);
        Assert.Contains(context.Data.CustomItems, item => item.Id == "item/base/iron-key");
        Assert.Contains(context.Data.CustomItems, item => item.Id == "item/original/deed-to-the-ship");
    }

    [Fact]
    public void AuthoredContentGraphUsesCanonicalIdsAndExistingMaps()
    {
        var assetRoot = Path.Combine(FindRepositoryRoot(), "DungeonEscape.Unity", "Assets", "DungeonEscape");
        var folder = Path.Combine(assetRoot, "Data");
        string Physical(string path) => Path.Combine(assetRoot, ContentPaths.RelativeToRoot(path));
        var index = ReadJson<CampaignIndex>(Path.Combine(folder, "campaigns.json"));
        var campaigns = index.Manifests.Select(path => ReadJson<CampaignDefinition>(Physical(path))).ToList();
        var groups = new Dictionary<string, List<string>>
        {
            ["item"] = new[] { "customitems", "magicitems", "nonmagicitems", "weaponitems", "armoritems" }.Select(name => Path.Combine(folder, name + ".json")).ToList(),
            ["spell"] = new() { Path.Combine(folder, "spells.json") }, ["skill"] = new() { Path.Combine(folder, "skills.json") },
            ["monster"] = new() { Path.Combine(folder, "allmonsters.json") }, ["background"] = new() { Path.Combine(folder, "backgrounds.json") },
            ["quest"] = new(), ["dialog"] = new(), ["map"] = new()
        };
        foreach (var campaign in campaigns)
        {
            groups["item"].Add(Physical(campaign.ItemsAssetPath));
            if (!string.IsNullOrEmpty(campaign.MonstersAssetPath)) groups["monster"].Add(Physical(campaign.MonstersAssetPath));
            groups["quest"].Add(Physical(campaign.QuestsAssetPath)); groups["dialog"].Add(Physical(campaign.DialogsAssetPath)); groups["map"].Add(Physical(campaign.MapsAssetPath));
        }
        var sets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            sets[group.Key] = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in group.Value)
            {
                var rows = JArray.Parse(File.ReadAllText(file));
                var ids = rows.Select(row => (string)row["Id"]!).ToList();
                Assert.All(ids, id => Assert.True(ContentId.IsValid(id, group.Key), "Invalid " + group.Key + " ID: " + id));
                Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
                sets[group.Key].UnionWith(ids);
            }
        }
        var referenceKinds = new Dictionary<string, string>
        {
            ["Quest"] = "quest", ["QuestId"] = "quest", ["Dialog"] = "dialog", ["DialogId"] = "dialog",
            ["ItemId"] = "item", ["KeyItemId"] = "item", ["MapId"] = "map", ["WarpMap"] = "map",
            ["Background"] = "background", ["Skill"] = "skill", ["Monster"] = "monster", ["MonsterId"] = "monster"
        };
        void Check(string kind, string reference)
        {
            if (string.IsNullOrEmpty(reference) || reference == "#Random#") return;
            Assert.True(sets[kind].Contains(reference), "Unresolved " + kind + " reference: " + reference);
        }
        foreach (var path in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(Path.Combine(assetRoot, "Campaigns"), "*.json", SearchOption.AllDirectories)))
        foreach (var property in ((JContainer)JToken.Parse(File.ReadAllText(path))).Descendants().OfType<JProperty>())
        {
            if (property.Value.Type == JTokenType.String && referenceKinds.TryGetValue(property.Name, out var kind)) Check(kind, (string)property.Value);
            if ((property.Name == "Items" || property.Name == "Spells") && property.Value is JArray refs)
                foreach (var reference in refs.Where(value => value.Type == JTokenType.String)) Check(property.Name == "Items" ? "item" : "spell", (string)reference);
        }
        var maps = groups["map"].SelectMany(path => ReadJson<List<MapDefinition>>(path)).ToList();
        var catalog = new MapCatalog(maps);
        Assert.Equal(Directory.GetFiles(Path.Combine(assetRoot, "Campaigns"), "*.tmx", SearchOption.AllDirectories).Length, maps.Count);
        foreach (var map in maps)
        {
            Assert.True(ContentPaths.IsCampaignPath(map.AssetPath, map.Id.Split('/')[1], ".tmx"));
            var path = Physical(map.AssetPath);
            Assert.True(File.Exists(path), map.AssetPath);
            Assert.Equal(map.Id, catalog.Get(map.AssetPath).Id);
            var xml = XDocument.Load(path);
            foreach (var property in xml.Descendants("property"))
                if (referenceKinds.TryGetValue((string)property.Attribute("name") ?? "", out var kind)) Check(kind, (string)property.Attribute("value"));
            foreach (var source in xml.Descendants().Attributes("source"))
                Assert.True(File.Exists(Physical(ContentPaths.ResolveRelative(map.AssetPath, source.Value))), "Missing relative map asset: " + source.Value);
        }
    }

    [Fact]
    public void EveryAuthoredItemAndSpellEffectResolvesByStableSkillId()
    {
        var dataFolder = Path.Combine(FindRepositoryRoot(), "DungeonEscape.Unity", "Assets", "DungeonEscape", "Data");
        var skills = ReadJson<List<Skill>>(Path.Combine(dataFolder, "skills.json"));
        Assert.Equal(skills.Count, skills.Select(skill => skill.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(skills, skill => Assert.Matches(@"^skill/[a-z0-9]+(?:-[a-z0-9]+)*/[a-z0-9]+(?:-[a-z0-9]+)*$", skill.Id));
        foreach (var spell in ReadJson<List<Spell>>(Path.Combine(dataFolder, "spells.json")))
        {
            spell.Setup(skills);
            Assert.Equal(Assert.Single(skills, skill => skill.Id == spell.SkillId).Type, spell.Type);
        }
        foreach (var file in new[] { "customitems.json", "magicitems.json", "nonmagicitems.json", "weaponitems.json", "armoritems.json" })
        {
            foreach (var item in ReadJson<List<Item>>(Path.Combine(dataFolder, file)).Where(item => !string.IsNullOrEmpty(item.SkillId)))
            {
                item.Setup(skills);
                Assert.NotNull(item.Skill);
            }
        }
    }

    [Fact]
    public void DataFilesContainDndNamedSkillsAndIronKey()
    {
        var repoRoot = FindRepositoryRoot();
        var dataFolder = Path.Combine(repoRoot, "DungeonEscape.Unity", "Assets", "DungeonEscape", "Data");

        var skills = ReadJson<List<Skill>>(Path.Combine(dataFolder, "skills.json"));
        var spells = ReadJson<List<Spell>>(Path.Combine(dataFolder, "spells.json"));
        var items = ReadJson<List<Item>>(Path.Combine(dataFolder, "customitems.json"));

        Assert.True(skills.Exists(skill => skill.Name == "Protection from Evil and Good"),
            "Protection from Evil and Good should exist in the skill catalog.");

        Assert.True(spells.Exists(spell =>
            spell.Name == "Protection from Evil and Good" && spell.SkillId == "skill/base/protection-from-evil-and-good"),
            "Protection from Evil and Good should reference the matching D&D-named skill.");

        Assert.True(items.Exists(item => item.Name == "Iron Key" && item.SkillId == "skill/base/knock"),
            "Iron Key should exist as a key item that satisfies the chest lock through Knock.");
    }

    private static T ReadJson<T>(string path)
    {
        using var reader = File.OpenText(path);
        var serializer = new JsonSerializer();
        return (T)serializer.Deserialize(reader, typeof(T))!;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DungeonEscape.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find DungeonEscape.sln above the test output directory.");
    }
}
