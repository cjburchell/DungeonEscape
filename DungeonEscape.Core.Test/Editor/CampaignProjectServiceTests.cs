#nullable enable
using DungeonEscape.Tools.GameEditor.Services;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Xunit;

namespace DungeonEscape.Core.Test.Editor;

public sealed class CampaignProjectServiceTests : IDisposable
{
    private readonly string sandbox;
    private readonly AssetContext assets;
    private readonly DataFolderService data;

    public CampaignProjectServiceTests()
    {
        var repo = new DirectoryInfo(AppContext.BaseDirectory);
        while (repo != null && !File.Exists(Path.Combine(repo.FullName, "DungeonEscape.sln"))) repo = repo.Parent;
        if (repo == null) throw new DirectoryNotFoundException("Repository root not found.");
        var source = Path.Combine(repo.FullName, "DungeonEscape.Unity", "Assets", "DungeonEscape");
        sandbox = Path.Combine(Path.GetTempPath(), "DungeonEscape.CampaignTests", Guid.NewGuid().ToString("N"));
        Copy(Path.Combine(source, "Data"), Path.Combine(sandbox, "Data"));
        Copy(Path.Combine(source, "Tilesets"), Path.Combine(sandbox, "Tilesets"));
        Copy(Path.Combine(source, "Campaigns"), Path.Combine(sandbox, "Campaigns"));
        Directory.CreateDirectory(Path.Combine(sandbox, "Images", "monsters"));
        assets = new AssetContext();
        data = new DataFolderService(assets, new MapDocumentService(assets), new CampaignProjectService(assets));
        data.Load(Path.Combine(sandbox, "Data"));
    }

    [Fact]
    public void CreationRegistersAPlayableProjectWithoutChangingSharedOrOriginalFiles()
    {
        var sharedBefore = Fingerprints(Path.Combine(sandbox, "Data"));
        var originalBefore = Fingerprints(Path.Combine(sandbox, "Campaigns", "original"));
        var campaignCount = data.Campaigns.Count;
        data.CreateCampaign("new-realm", "New Realm", "Fresh story");
        Assert.Equal("campaign/new-realm/main", data.SelectedCampaign!.Id);
        Assert.Equal(campaignCount + 1, data.Campaigns.Count);
        Assert.True(Assert.Single(data.Maps).IsOverworld);
        Assert.Empty(data.GetOverworldMonsters());
        Assert.False(data.CanEdit(data.Monsters[0]));
        var clone = JsonConvert.DeserializeObject<Monster>(JsonConvert.SerializeObject(data.Monsters[0]))!;
        clone.Id = "monster/new-realm/guardian";
        data.Monsters.Add(clone);
        data.Items.Add(new Item { Id = "item/new-realm/token", Name = "Token" });
        data.SelectedCampaign.OverworldMonsterIds.Add(clone.Id);
        data.SelectedCampaign.MaxStartingPartyMembers = 3;
        data.MarkDirty();
        data.Save();
        var savedCampaign = JsonConvert.DeserializeObject<CampaignDefinition>(File.ReadAllText(
            Path.Combine(sandbox, "Campaigns", "new-realm", "campaign.json")))!;
        Assert.Equal(3, savedCampaign.MaxStartingPartyMembers);
        var shared = CampaignContentResolver.ExtractBase(new DungeonEscapeDataSet
        {
            CustomItems = data.Items, Monsters = data.Monsters, Skills = data.Skills, Spells = data.Spells, Backgrounds = data.Backgrounds
        });
        var context = CampaignContentLoader.Load(data.SelectedCampaign, shared, Read);
        Assert.Contains(context.Data.Monsters, monster => monster.Id == clone.Id);
        Assert.Single(context.OverworldMonsters);
        Assert.Single(JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText(assets.ResolveAssetPath(data.SelectedCampaign.ItemsAssetPath)))!);
        Assert.Single(JsonConvert.DeserializeObject<List<Monster>>(File.ReadAllText(assets.ResolveAssetPath(data.SelectedCampaign.MonstersAssetPath)))!);
        foreach (var pair in sharedBefore.Where(pair => pair.Key != "campaigns.json")) Assert.Equal(pair.Value, Fingerprints(Path.Combine(sandbox, "Data"))[pair.Key]);
        Assert.Equal(originalBefore, Fingerprints(Path.Combine(sandbox, "Campaigns", "original")));
        var index = JsonConvert.DeserializeObject<CampaignIndex>(File.ReadAllText(Path.Combine(sandbox, "Data", "campaigns.json")))!;
        Assert.Equal("campaign/original/main", index.DefaultCampaignId);
    }

    [Fact]
    public void CampaignSavingIgnoresSharedEditsAndSwitchingRequiresADirtyDecision()
    {
        var before = Fingerprints(Path.Combine(sandbox, "Data"));
        data.Monsters[0].Name = "Shared edit should never be written";
        data.Quests[0].Name = "Edited campaign quest";
        data.MarkDirty();
        Assert.Throws<InvalidOperationException>(() => data.SelectCampaign(null));
        data.Save();
        Assert.Equal(before, Fingerprints(Path.Combine(sandbox, "Data")));
        var quests = JsonConvert.DeserializeObject<List<Quest>>(File.ReadAllText(assets.ResolveAssetPath(data.SelectedCampaign!.QuestsAssetPath)))!;
        Assert.Equal("Edited campaign quest", quests[0].Name);
        data.SelectCampaign(null);
        Assert.Null(data.SelectedCampaign);
        Assert.True(data.CanEdit(data.Monsters[0]));
        Assert.DoesNotContain(data.Monsters, monster => monster.Name == "Shared edit should never be written");
        Assert.Empty(data.Maps);
    }

    [Fact]
    public void DuplicateAndInvalidCreationCannotOverwriteFiles()
    {
        var before = Fingerprints(sandbox);
        Assert.Throws<InvalidOperationException>(() => data.CreateCampaign("original", "Duplicate", ""));
        Assert.Throws<ArgumentException>(() => data.CreateCampaign("../escape", "Invalid", ""));
        Assert.Equal(before, Fingerprints(sandbox));
    }

    [Fact]
    public void DiscardingChangesOnSwitchDoesNotSaveAnotherCampaign()
    {
        var before = Fingerprints(Path.Combine(sandbox, "Campaigns", "original"));
        data.SelectedCampaign!.Name = "Unsaved title";
        data.MarkDirty();
        data.SelectCampaign(null, true);
        Assert.False(data.IsDirty);
        Assert.Equal(before, Fingerprints(Path.Combine(sandbox, "Campaigns", "original")));
        Assert.Equal("Dungeon Escape", data.Campaigns[0].Name);
    }

    [Fact]
    public void MapCatalogCannotGiveTheEditorWriteAccessToAnotherCampaign()
    {
        data.CreateCampaign("other-realm", "Other", "");
        var otherBefore = Fingerprints(Path.Combine(sandbox, "Campaigns", "other-realm"));
        var registry = Path.Combine(sandbox, "Campaigns", "original", "Data", "maps.json");
        var maps = JsonConvert.DeserializeObject<List<MapDefinition>>(File.ReadAllText(registry))!;
        maps[0].Id = "map/other-realm/overworld";
        maps[0].AssetPath = "Assets/DungeonEscape/Campaigns/other-realm/Maps/overworld.tmx";
        maps[0].EncounterAssetPath = "Assets/DungeonEscape/Campaigns/other-realm/Data/Encounters/overworld_monsters.json";
        File.WriteAllText(registry, JsonConvert.SerializeObject(maps));
        Assert.Throws<InvalidDataException>(() => data.SelectCampaign("campaign/original/main"));
        Assert.Equal("campaign/other-realm/main", data.SelectedCampaign!.Id);
        Assert.Equal(otherBefore, Fingerprints(Path.Combine(sandbox, "Campaigns", "other-realm")));
    }

    [Fact]
    public void FailedIndexRegistrationRollsBackTheNewDirectoryOnWindows()
    {
        if (!OperatingSystem.IsWindows()) return; // Windows locks forbid replacing an open index file.
        var index = Path.Combine(sandbox, "Data", "campaigns.json");
        var before = File.ReadAllText(index);
        using (var locked = new FileStream(index, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var failure = Record.Exception(() => data.CreateCampaign("rollback-test", "Rollback", ""));
            Assert.True(failure is IOException || failure is UnauthorizedAccessException, failure?.ToString());
        }
        Assert.Equal(before, File.ReadAllText(index));
        Assert.False(Directory.Exists(Path.Combine(sandbox, "Campaigns", "rollback-test")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(sandbox, "Campaigns"), ".creating-*"));
        Assert.Empty(Directory.GetFiles(Path.Combine(sandbox, "Data"), "campaigns.json.tmp-*"));
    }

    private string Read(string path) => File.Exists(assets.ResolveAssetPath(path)) ? File.ReadAllText(assets.ResolveAssetPath(path)) : null!;

    private static Dictionary<string, string> Fingerprints(string root) => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .OrderBy(path => path, StringComparer.Ordinal).ToDictionary(path => Path.GetRelativePath(root, path), path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));

    private static void Copy(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(source)) Copy(dir, Path.Combine(target, Path.GetFileName(dir)));
    }

    public void Dispose()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DungeonEscape.CampaignTests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(sandbox).StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid test cleanup path.");
        if (Directory.Exists(sandbox)) Directory.Delete(sandbox, true);
    }
}
