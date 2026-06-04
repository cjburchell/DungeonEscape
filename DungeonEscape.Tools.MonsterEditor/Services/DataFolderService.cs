using System.IO;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;

namespace DungeonEscape.Tools.MonsterEditor.Services;

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
    private const string ItemDefinitionsFileName = "itemdef.json";
    private const string QuestsFileName = "quests.json";
    private const string StatNamesFileName = "statnames.json";
    private const string NamesFileName = "names.json";

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    private readonly AssetContext assetContext;

    public DataFolderService(AssetContext assetContext)
    {
        this.assetContext = assetContext;
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
    public List<StatName> StatNames { get; private set; } = new();
    public Names Names { get; private set; } = new() { Male = new List<string>(), Female = new List<string>() };

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
        StatNames = LoadList<StatName>(Path.Combine(folderPath, StatNamesFileName));
        Names = LoadObject(Path.Combine(folderPath, NamesFileName), new Names()) ?? new Names();
        Names.Male ??= new List<string>();
        Names.Female ??= new List<string>();

        FolderPath = folderPath;
        IsDirty = false;
        HasDocument = true;

        // Resolve images/tilesets relative to the opened folder.
        assetContext.TryDetectFrom(folderPath);

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
        SaveList(Path.Combine(FolderPath, StatNamesFileName), StatNames);
        SaveObject(Path.Combine(FolderPath, NamesFileName), Names);

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
        var json = JsonConvert.SerializeObject(items, SerializerSettings);
        File.WriteAllText(path, json);
    }

    private static T? LoadObject<T>(string path, T fallback)
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
        var json = JsonConvert.SerializeObject(item, SerializerSettings);
        File.WriteAllText(path, json);
    }

    private void NotifyChanged() => StateChanged?.Invoke();

    private void NotifyDataChanged() => DataChanged?.Invoke();
}
