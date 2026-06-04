using System.IO;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;

namespace DungeonEscape.Tools.MonsterEditor.Services;

/// <summary>
/// Owns the in-memory list of monsters for the currently open file and handles
/// loading / saving using the game's own Newtonsoft conventions so the produced
/// JSON matches the game data exactly.
/// </summary>
public sealed class MonsterFileService
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    private readonly AssetContext assetContext;

    public MonsterFileService(AssetContext assetContext)
    {
        this.assetContext = assetContext;
    }

    /// <summary>Raised whenever the document, selection or dirty state changes.</summary>
    public event Action? StateChanged;

    public List<Monster> Monsters { get; private set; } = new();

    public Monster? Selected { get; private set; }

    public string? CurrentFilePath { get; private set; }

    public bool IsDirty { get; private set; }

    public bool HasDocument { get; private set; }

    public string DisplayName =>
        CurrentFilePath == null
            ? "Untitled"
            : Path.GetFileName(CurrentFilePath);

    public void NewDocument()
    {
        Monsters = new List<Monster>();
        Selected = null;
        CurrentFilePath = null;
        IsDirty = false;
        HasDocument = true;
        NotifyChanged();
    }

    public void Load(string path)
    {
        var json = File.ReadAllText(path);
        var loaded = JsonConvert.DeserializeObject<List<Monster>>(json) ?? new List<Monster>();

        Monsters = loaded;
        CurrentFilePath = path;
        Selected = Monsters.FirstOrDefault();
        IsDirty = false;
        HasDocument = true;

        // Resolve images/dropdowns relative to the opened file.
        assetContext.TryDetectFrom(path);

        NotifyChanged();
    }

    public void Save(string? path = null)
    {
        var target = path ?? CurrentFilePath;
        if (string.IsNullOrEmpty(target))
        {
            throw new InvalidOperationException("No file path specified for save.");
        }

        var json = JsonConvert.SerializeObject(Monsters, SerializerSettings);
        File.WriteAllText(target, json);

        CurrentFilePath = target;
        IsDirty = false;

        if (assetContext.AssetRoot == null)
        {
            assetContext.TryDetectFrom(target);
        }

        NotifyChanged();
    }

    public void Select(Monster? monster)
    {
        Selected = monster;
        NotifyChanged();
    }

    public Monster AddMonster()
    {
        var monster = new Monster { Name = "New Monster" };
        Monsters.Add(monster);
        Selected = monster;
        MarkDirty();
        return monster;
    }

    public Monster? Duplicate(Monster? source)
    {
        if (source == null)
        {
            return null;
        }

        var json = JsonConvert.SerializeObject(source, SerializerSettings);
        var clone = JsonConvert.DeserializeObject<Monster>(json) ?? new Monster();
        clone.Name = string.IsNullOrEmpty(source.Name) ? "Copy" : source.Name + " (Copy)";

        var index = Monsters.IndexOf(source);
        if (index >= 0)
        {
            Monsters.Insert(index + 1, clone);
        }
        else
        {
            Monsters.Add(clone);
        }

        Selected = clone;
        MarkDirty();
        return clone;
    }

    public void Remove(Monster? monster)
    {
        if (monster == null || !Monsters.Remove(monster))
        {
            return;
        }

        if (ReferenceEquals(Selected, monster))
        {
            Selected = Monsters.FirstOrDefault();
        }

        MarkDirty();
    }

    /// <summary>Flag the document as having unsaved changes and notify listeners.</summary>
    public void MarkDirty()
    {
        IsDirty = true;
        NotifyChanged();
    }

    private void NotifyChanged() => StateChanged?.Invoke();
}
