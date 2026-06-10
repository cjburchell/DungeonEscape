using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redpoint.DungeonEscape.Data;
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
    private static readonly StatType[] RequiredStatNameTypes =
    {
        StatType.Agility,
        StatType.Defence,
        StatType.Health,
        StatType.Attack,
        StatType.Magic,
        StatType.MagicDefence
    };

    private const string MonstersFileName = "allmonsters.json";
    private const string SpellsFileName = "spells.json";
    private const string SkillsFileName = "skills.json";
    private const string ItemsFileName = "customitems.json";
    private const string ItemDefinitionsFileName = "itemdef.json";
    private const string QuestsFileName = "quests.json";
    private const string DialogsFileName = "dialog.json";
    private const string StatNamesFileName = "statnames.json";
    private const string NamesFileName = "names.json";
    private const string ClassLevelsFileName = "classlevels.json";

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    private readonly AssetContext assetContext;
    private readonly MapDocumentService maps;

    public DataFolderService(AssetContext assetContext, MapDocumentService maps)
    {
        this.assetContext = assetContext;
        this.maps = maps;
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
    public List<ItemDefinition> ItemDefinitions { get; private set; } = new();
    public List<Quest> Quests { get; private set; } = new();
    public List<Dialog> Dialogs { get; private set; } = new();
    public List<StatName> StatNames { get; private set; } = new();
    public List<ClassStats> ClassLevels { get; private set; } = new();
    public Names Names { get; private set; } = new() { Male = new List<string>(), Female = new List<string>() };
    public List<MapDocument> Maps { get; private set; } = new();

    public string? FolderPath { get; private set; }

    public bool IsDirty { get; private set; }

    public bool HasDocument { get; private set; }

    public string DisplayName =>
        FolderPath == null ? "No folder" : new DirectoryInfo(FolderPath).Name;

    public void Load(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        Monsters = LoadList<Monster>(Path.Combine(folderPath, MonstersFileName));
        Spells = LoadList<Spell>(Path.Combine(folderPath, SpellsFileName));
        Skills = LoadList<Skill>(Path.Combine(folderPath, SkillsFileName));
        Items = LoadList<Item>(Path.Combine(folderPath, ItemsFileName));
        ItemDefinitions = LoadList<ItemDefinition>(Path.Combine(folderPath, ItemDefinitionsFileName));
        Quests = LoadList<Quest>(Path.Combine(folderPath, QuestsFileName));
        Dialogs = LoadList<Dialog>(Path.Combine(folderPath, DialogsFileName));
        StatNames = LoadList<StatName>(Path.Combine(folderPath, StatNamesFileName));
        NormalizeStatNames();
        ClassLevels = LoadList<ClassStats>(Path.Combine(folderPath, ClassLevelsFileName));
        Names = LoadObject(Path.Combine(folderPath, NamesFileName), new Names());
        Names.Male ??= new List<string>();
        Names.Female ??= new List<string>();

        FolderPath = folderPath;
        IsDirty = false;
        HasDocument = true;

        // Resolve images/tilesets relative to the opened folder.
        assetContext.TryDetectFrom(folderPath);
        Maps = maps.LoadMaps(assetContext.MapsDirectory, assetContext.MapDataDirectory);

        NotifyDataChanged();
        NotifyChanged();
    }

    public void Reload()
    {
        if (FolderPath != null)
        {
            Load(FolderPath);
        }
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(FolderPath))
        {
            throw new InvalidOperationException("No folder open to save to.");
        }

        SaveList(Path.Combine(FolderPath, MonstersFileName), Monsters);
        SaveList(Path.Combine(FolderPath, SpellsFileName), Spells);
        SaveList(Path.Combine(FolderPath, SkillsFileName), Skills);
        SaveList(Path.Combine(FolderPath, ItemsFileName), Items);
        SaveList(Path.Combine(FolderPath, ItemDefinitionsFileName), ItemDefinitions);
        SaveList(Path.Combine(FolderPath, QuestsFileName), Quests);
        SaveList(Path.Combine(FolderPath, DialogsFileName), Dialogs);
        NormalizeStatNames();
        SaveList(Path.Combine(FolderPath, StatNamesFileName), StatNames);
        SaveList(Path.Combine(FolderPath, ClassLevelsFileName), ClassLevels);
        SaveObject(Path.Combine(FolderPath, NamesFileName), Names);
        maps.SaveMaps(Maps, assetContext.MapDataDirectory);

        IsDirty = false;
        NotifyChanged();
    }

    /// <summary>Flag the project as having unsaved changes and notify listeners.</summary>
    public void MarkDirty()
    {
        IsDirty = true;
        NotifyDataChanged();
        NotifyChanged();
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

    private void NormalizeStatNames()
    {
        var byType = StatNames
            .Where(statName => statName != null)
            .GroupBy(statName => statName.Type)
            .ToDictionary(group => group.Key, group => group.First());

        StatNames.Clear();
        foreach (var statType in RequiredStatNameTypes)
        {
            var statName = byType.TryGetValue(statType, out var existing)
                ? existing
                : new StatName { Type = statType, Prefix = new List<string>(), Suffix = new List<string>() };

            statName.Type = statType;
            statName.Prefix ??= new List<string>();
            statName.Suffix ??= new List<string>();
            StatNames.Add(statName);
        }
    }

    private void NotifyChanged() => StateChanged?.Invoke();

    private void NotifyDataChanged() => DataChanged?.Invoke();
}
