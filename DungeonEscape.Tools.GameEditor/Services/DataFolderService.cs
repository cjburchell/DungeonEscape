#nullable enable
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;

namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// Owns the single in-memory dataset for the currently open Data folder. All
/// editor tabs (Monsters / Spells / Skills / Items) bind to the lists exposed
/// here so cross-references (e.g. a newly added item appearing in a monster's
/// drop list) stay live without reloading. Saving writes every known file back
/// using the game's own Newtonsoft conventions so the produced JSON matches the
/// game data exactly.
/// </summary>
public sealed class DataFolderService
{
    private const string MonstersFileName = "allmonsters.json";
    private const string SpellsFileName = "spells.json";
    private const string SkillsFileName = "skills.json";
    private const string ItemsFileName = "customitems.json";
    private const string MagicItemsFileName = "magicitems.json";
    private const string NonMagicItemsFileName = "nonmagicitems.json";
    private const string WeaponItemsFileName = "weaponitems.json";
    private const string ArmorItemsFileName = "armoritems.json";
    private const string QuestsFileName = "quests.json";
    private const string DialogsFileName = "dialog.json";
    private const string NamesFileName = "names.json";
    private const string ClassesFileName = "class.json";
    private const string SpeciesFileName = "species.json";
    private const string BackgroundsFileName = "backgrounds.json";

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    private readonly AssetContext assetContext;
    private readonly MapDocumentService maps;
    private readonly CampaignProjectService projects;
    private Dictionary<string, string> manifestPaths = new(StringComparer.Ordinal);

    public DataFolderService(AssetContext assetContext, MapDocumentService maps, CampaignProjectService projects)
    {
        this.assetContext = assetContext;
        this.maps = maps;
        this.projects = projects;
    }

    /// <summary>Raised whenever the folder, selection or dirty state changes.</summary>
    public event Action? StateChanged;

    /// <summary>
    /// Raised whenever the underlying data lists change (add / remove / edit).
    /// Used by the data-source catalog and dropdowns so they refresh live.
    /// </summary>
    public event Action? DataChanged;

    public List<Monster> Monsters { get; private set; } = new();
    public List<Spell> Spells { get; private set; } = new();
    public List<Skill> Skills { get; private set; } = new();
    public List<Item> Items { get; private set; } = new();
    public List<Item> CustomItems { get; private set; } = new();
    public List<Item> MagicItems { get; private set; } = new();
    public List<Item> NonMagicItems { get; private set; } = new();
    public List<Item> WeaponItems { get; private set; } = new();
    public List<Item> ArmorItems { get; private set; } = new();
    public List<Quest> Quests { get; private set; } = new();
    public List<Dialog> Dialogs { get; private set; } = new();
    public List<ClassStats> ClassLevels { get; private set; } = new();
    public List<SpeciesDefinition> Species { get; private set; } = new();
    public List<BackgroundDefinition> Backgrounds { get; private set; } = new();
    public Names Names { get; private set; } = new() { Male = new List<string>(), Female = new List<string>() };
    public List<MapDocument> Maps { get; private set; } = new();

    public string? FolderPath { get; private set; }

    public bool IsDirty { get; private set; }

    public bool HasDocument { get; private set; }

    public IReadOnlyList<CampaignDefinition> Campaigns { get; private set; } = Array.Empty<CampaignDefinition>();
    public CampaignDefinition? SelectedCampaign { get; private set; }
    public string EditScope => SelectedCampaign == null ? "base" : CampaignCatalog.GetScope(SelectedCampaign);
    public string DisplayName => SelectedCampaign?.Name ?? "Shared Library";

    public bool CanEditKind(string kind) => SelectedCampaign == null
        ? kind != "quest" && kind != "dialog" && kind != "map"
        : kind == "monster" || kind == "item" || kind == "quest" || kind == "dialog" || kind == "map";

    public bool CanEdit(object? value) => value switch
    {
        Monster item => Owns(item.Id), Item item => Owns(item.Id), Quest item => Owns(item.Id), Dialog item => Owns(item.Id),
        Spell item => SelectedCampaign == null && Owns(item.Id), Skill item => SelectedCampaign == null && Owns(item.Id),
        BackgroundDefinition item => SelectedCampaign == null && Owns(item.Id), MapDocument map => SelectedCampaign != null && Maps.Contains(map),
        null => false, _ => SelectedCampaign == null
    };

    private bool Owns(string? id) => id != null && id.Split('/').Length == 3 && id.Split('/')[1] == EditScope;

    public void Load(string folderPath)
    {
        if (!assetContext.TryDetectFrom(folderPath) || assetContext.DataDirectory == null)
            throw new DirectoryNotFoundException("Choose a Dungeon Escape project or campaign directory.");
        FolderPath = assetContext.DataDirectory;
        RefreshCampaigns();
        var index = LoadObject(Path.Combine(FolderPath, "campaigns.json"), new CampaignIndex());
        var requested = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar);
        var campaignAtPath = Campaigns.FirstOrDefault(campaign =>
        {
            var root = Path.GetDirectoryName(assetContext.ResolveAssetPath(manifestPaths[campaign.Id]))!;
            return requested.Equals(root, StringComparison.OrdinalIgnoreCase) || requested.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        });
        SelectCampaign(campaignAtPath?.Id ?? index.DefaultCampaignId, true);
    }

    private void RefreshCampaigns()
    {
        var index = LoadObject(Path.Combine(FolderPath!, "campaigns.json"), new CampaignIndex());
        if (index.SchemaVersion != 1 || index.Manifests == null) throw new InvalidDataException("Unsupported campaign index.");
        manifestPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        var list = new List<CampaignDefinition>();
        foreach (var path in index.Manifests)
        {
            if (!CampaignCatalog.IsDataAssetPath(path)) throw new InvalidDataException("Invalid campaign manifest path.");
            var campaign = JsonConvert.DeserializeObject<CampaignDefinition>(File.ReadAllText(assetContext.ResolveAssetPath(path)))
                ?? throw new InvalidDataException("Invalid campaign manifest.");
            CampaignCatalog.Validate(campaign);
            if (!ContentPaths.IsCampaignPath(path, CampaignCatalog.GetScope(campaign), ".json"))
                throw new InvalidDataException("The manifest must be inside its campaign directory.");
            manifestPaths.Add(campaign.Id, path);
            list.Add(campaign);
        }
        _ = new CampaignCatalog(list, index.DefaultCampaignId);
        Campaigns = list;
    }

    public void SelectCampaign(string? id, bool discardChanges = false)
    {
        if (IsDirty && !discardChanges) throw new InvalidOperationException("Save or discard changes before switching campaigns.");
        RefreshCampaigns();
        var campaign = id == null ? null : Campaigns.FirstOrDefault(value => value.Id == id)
            ?? (id == null ? null : throw new InvalidOperationException("Campaign is not installed."));
        var shared = ReadShared();
        var ownItems = campaign == null ? new List<Item>() : LoadList<Item>(assetContext.ResolveAssetPath(campaign.ItemsAssetPath));
        var ownMonsters = campaign == null || string.IsNullOrEmpty(campaign.MonstersAssetPath) ? new List<Monster>() : LoadList<Monster>(assetContext.ResolveAssetPath(campaign.MonstersAssetPath));
        var mapDocuments = maps.LoadMaps(campaign);
        Monsters = shared.Monsters.Concat(ownMonsters).ToList();
        Items = shared.CustomItems.Concat(ownItems).ToList();
        Skills = shared.Skills; Spells = shared.Spells; ClassLevels = shared.ClassLevels; Species = shared.Species;
        Backgrounds = shared.Backgrounds; Names = shared.Names;
        Quests = campaign == null ? new List<Quest>() : LoadList<Quest>(assetContext.ResolveAssetPath(campaign.QuestsAssetPath));
        Dialogs = campaign == null ? new List<Dialog>() : LoadList<Dialog>(assetContext.ResolveAssetPath(campaign.DialogsAssetPath));
        SelectedCampaign = campaign; Maps = mapDocuments;
        HasDocument = true; IsDirty = false;
        NotifyDataChanged(); NotifyChanged();
    }

    private DungeonEscapeDataSet ReadShared()
    {
        var folder = FolderPath!;
        return CampaignContentResolver.ExtractBase(new DungeonEscapeDataSet
        {
            Monsters = LoadList<Monster>(Path.Combine(folder, MonstersFileName)), Spells = LoadList<Spell>(Path.Combine(folder, SpellsFileName)),
            Skills = LoadList<Skill>(Path.Combine(folder, SkillsFileName)),
            CustomItems = MergeItemCatalogs(LoadList<Item>(Path.Combine(folder, ItemsFileName)), LoadList<Item>(Path.Combine(folder, MagicItemsFileName)),
                LoadList<Item>(Path.Combine(folder, NonMagicItemsFileName)), LoadList<Item>(Path.Combine(folder, WeaponItemsFileName)), LoadList<Item>(Path.Combine(folder, ArmorItemsFileName))),
            ClassLevels = LoadList<ClassStats>(Path.Combine(folder, ClassesFileName)), Species = LoadList<SpeciesDefinition>(Path.Combine(folder, SpeciesFileName)),
            Backgrounds = LoadList<BackgroundDefinition>(Path.Combine(folder, BackgroundsFileName)), Names = LoadObject(Path.Combine(folder, NamesFileName), new Names { Male = new(), Female = new() })
        });
    }

    public void CreateCampaign(string key, string name, string description)
    {
        if (IsDirty) throw new InvalidOperationException("Save or discard changes before creating a campaign.");
        var campaign = projects.Create(key, name, description, ReadShared());
        SelectCampaign(campaign.Id);
    }

    public IEnumerable<Monster> GetOverworldMonsters() => SelectedCampaign == null ? Monsters :
        Monsters.Where(monster => SelectedCampaign.OverworldMonsterIds.Contains(monster.Id, StringComparer.Ordinal));

    public void Reload()
    {
        if (FolderPath != null) SelectCampaign(SelectedCampaign?.Id, true);
    }

    public void Save()
    {
        if (FolderPath == null) throw new InvalidOperationException("No project is open.");
        if (SelectedCampaign is CampaignDefinition campaign)
        {
            CampaignCatalog.Validate(campaign);
            if (Quests.Any(item => !Owns(item.Id)) || Dialogs.Any(item => !Owns(item.Id)))
                throw new InvalidDataException("Narrative definitions must belong to the selected campaign.");
            SaveList(assetContext.ResolveAssetPath(campaign.ItemsAssetPath), Items.Where(item => Owns(item.Id)).ToList());
            if (string.IsNullOrEmpty(campaign.MonstersAssetPath))
                campaign.MonstersAssetPath = ContentPaths.AssetPrefix + "Campaigns/" + EditScope + "/Data/monsters.json";
            SaveList(assetContext.ResolveAssetPath(campaign.MonstersAssetPath), Monsters.Where(item => Owns(item.Id)).ToList());
            SaveList(assetContext.ResolveAssetPath(campaign.QuestsAssetPath), Quests);
            SaveList(assetContext.ResolveAssetPath(campaign.DialogsAssetPath), Dialogs);
            File.WriteAllText(assetContext.ResolveAssetPath(manifestPaths[campaign.Id]), JsonConvert.SerializeObject(campaign, SerializerSettings));
            maps.SaveMaps(Maps);
        }
        else
        {
            SaveList(Path.Combine(FolderPath, MonstersFileName), Monsters);
            SaveList(Path.Combine(FolderPath, SpellsFileName), Spells); SaveList(Path.Combine(FolderPath, SkillsFileName), Skills);
            CustomItems = Items.Where(item => !item.IsMagicItem && item.Type != ItemType.Weapon && item.Type != ItemType.Armor && item.Type != ItemType.OneUse && item.Type != ItemType.RepeatableUse && item.Type != ItemType.Gold).ToList();
            MagicItems = Items.Where(item => item.IsMagicItem).ToList(); NonMagicItems = Items.Where(item => !item.IsMagicItem && (item.Type == ItemType.OneUse || item.Type == ItemType.RepeatableUse || item.Type == ItemType.Gold)).ToList();
            WeaponItems = Items.Where(item => item.Type == ItemType.Weapon).ToList(); ArmorItems = Items.Where(item => item.Type == ItemType.Armor).ToList();
            SaveList(Path.Combine(FolderPath, ItemsFileName), CustomItems); SaveList(Path.Combine(FolderPath, MagicItemsFileName), MagicItems);
            SaveList(Path.Combine(FolderPath, NonMagicItemsFileName), NonMagicItems); SaveList(Path.Combine(FolderPath, WeaponItemsFileName), WeaponItems); SaveList(Path.Combine(FolderPath, ArmorItemsFileName), ArmorItems);
            SaveList(Path.Combine(FolderPath, ClassesFileName), ClassLevels); SaveList(Path.Combine(FolderPath, SpeciesFileName), Species); SaveList(Path.Combine(FolderPath, BackgroundsFileName), Backgrounds);
            SaveObject(Path.Combine(FolderPath, NamesFileName), Names);
        }
        IsDirty = false; NotifyChanged();
    }

    /// <summary>Flag the project as having unsaved changes and notify listeners.</summary>
    public void MarkDirty()
    {
        IsDirty = true;
        foreach (var map in Maps) map.IsOverworld = map.Id == SelectedCampaign?.OverworldMapId;
        NotifyDataChanged();
        NotifyChanged();
    }

    private static List<Item> MergeItemCatalogs(params List<Item>[] itemLists)
    {
        return itemLists
            .SelectMany(list => list ?? new List<Item>())
            .Where(item => item != null)
            .GroupBy(item => item.Id, StringComparer.Ordinal)
                .SelectMany(group => string.IsNullOrWhiteSpace(group.Key) ? group : group.Take(1))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<T> LoadList<T>(string path)
    {
        if (!File.Exists(path))
        {
            return new List<T>();
        }

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
    }

    private static void SaveList<T>(string path, List<T> items)
    {
        var json = SerializeData(items);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    private static T LoadObject<T>(string path, T fallback)
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json) ?? fallback;
    }

    private static void SaveObject<T>(string path, T item)
    {
        var json = SerializeData(item);
        File.WriteAllText(path, json);
    }

    private static string SerializeData<T>(T item)
    {
        var json = JsonConvert.SerializeObject(item, SerializerSettings);
        var token = JToken.Parse(json);
        PruneDefaultValues(token, true);
        return token.ToString(Formatting.Indented);
    }

    private static bool PruneDefaultValues(JToken token, bool isRoot = false)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var property in token.Children<JProperty>().ToList())
                {
                    // These nullable values distinguish an explicit false/zero from an omitted condition/stage.
                    if ((property.Name == nameof(DialogHead.QuestCompleted) && property.Value.Type == JTokenType.Boolean) ||
                        (property.Name == nameof(Quest.ObjectivesCompleteStage) && property.Value.Type == JTokenType.Integer)) continue;
                    if (PruneDefaultValues(property.Value))
                    {
                        property.Remove();
                    }
                }

                return !isRoot && !token.Children<JProperty>().Any();

            case JTokenType.Array:
                foreach (var child in token.Children().ToList())
                {
                    if (PruneDefaultValues(child))
                    {
                        child.Remove();
                    }
                }

                return !isRoot && !token.Children().Any();

            case JTokenType.Null:
            case JTokenType.Undefined:
                return !isRoot;

            case JTokenType.String:
                return !isRoot && string.IsNullOrEmpty(token.Value<string>());

            case JTokenType.Boolean:
                return !isRoot && token.Value<bool>() == false;

            case JTokenType.Integer:
                return !isRoot && token.Value<long>() == 0;

            case JTokenType.Float:
                return !isRoot && token.Value<double>() == 0d;

            default:
                return false;
        }
    }

    private void NotifyChanged() => StateChanged?.Invoke();

    private void NotifyDataChanged() => DataChanged?.Invoke();
}
