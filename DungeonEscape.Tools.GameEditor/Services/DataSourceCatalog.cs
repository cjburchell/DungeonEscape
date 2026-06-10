namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// Provides the option lists used to populate the Spells / Skills / Items
/// dropdowns. The names are derived directly from the in-memory dataset owned by
/// <see cref="DataFolderService"/> and refreshed whenever that data changes, so
/// a newly added (or renamed) entity shows up everywhere it is referenced
/// without needing a reload.
/// </summary>
public sealed class DataSourceCatalog
{
    public const string RandomItemId = "#Random#";

    private readonly DataFolderService data;

    public DataSourceCatalog(DataFolderService data)
    {
        this.data = data;
        this.data.DataChanged += Reload;
        Reload();
    }

    public IReadOnlyList<string> Spells { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Skills { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Items { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Quests { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Dialogs { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Monsters { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Classes { get; private set; } = Array.Empty<string>();

    public void Reload()
    {
        Spells = Names(data.Spells.Select(s => s.Name));
        Skills = Names(data.Skills.Select(s => s.Name));
        Items = Names(data.Items.Select(i => i.Name)
            .Concat(data.Items.Select(i => i.Id))
            .Concat(data.ItemDefinitions.SelectMany(d => d.Names ?? new List<Redpoint.DungeonEscape.Data.ItemName>()).Select(n => n.Name)));
        Items = Items.Prepend(RandomItemId).ToList();
        Quests = Names(data.Quests.Select(q => q.Id));
        Dialogs = Names(data.Dialogs.Select(d => d.Id));
        Monsters = Names(data.Monsters.Select(m => m.Name));
        Classes = data.ClassLevels
            .Select(classStats => classStats.Class)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct()
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (Classes.Count == 0)
        {
            Classes = Enum.GetValues<Redpoint.DungeonEscape.State.Class>()
                .Select(value => value.ToString())
                .ToList();
        }
    }

    private static IReadOnlyList<string> Names(IEnumerable<string?> source)
    {
        return source
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
