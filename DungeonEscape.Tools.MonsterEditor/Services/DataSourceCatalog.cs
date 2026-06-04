using System.IO;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;

namespace DungeonEscape.Tools.MonsterEditor.Services;

/// <summary>
/// Loads the option lists used to populate the Spells / Skills / Items dropdowns
/// from the sibling data files next to the loaded monster file (or under the
/// detected asset Data folder).
/// </summary>
public sealed class DataSourceCatalog
{
    private readonly AssetContext context;

    public DataSourceCatalog(AssetContext context)
    {
        this.context = context;
        this.context.Changed += Reload;
        Reload();
    }

    public IReadOnlyList<string> Spells { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Skills { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Items { get; private set; } = Array.Empty<string>();

    public void Reload()
    {
        Spells = LoadNames<Spell>("spells.json", s => s.Name);
        Skills = LoadNames<Skill>("skills.json", s => s.Name);
        Items = LoadNames<Item>("customitems.json", i => i.Name);
    }

    private IReadOnlyList<string> LoadNames<T>(string fileName, Func<T, string?> nameSelector)
    {
        var dataDirectory = context.DataDirectory;
        if (string.IsNullOrEmpty(dataDirectory))
        {
            return Array.Empty<string>();
        }

        var path = Path.Combine(dataDirectory, fileName);
        if (!File.Exists(path))
        {
            return Array.Empty<string>();
        }

        try
        {
            var items = JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(path));
            if (items == null)
            {
                return Array.Empty<string>();
            }

            return items
                .Select(nameSelector)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
