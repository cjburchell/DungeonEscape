namespace DungeonEscape.Tools.MonsterEditor.Services;

/// <summary>
/// Provides the option lists used to populate the Spells / Skills / Items
/// dropdowns. The names are derived directly from the in-memory dataset owned by
/// <see cref="DataFolderService"/> and refreshed whenever that data changes, so
/// a newly added (or renamed) entity shows up everywhere it is referenced
/// without needing a reload.
/// </summary>
public sealed class DataSourceCatalog
{
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

    public void Reload()
    {
        Spells = Names(data.Spells.Select(s => s.Name));
        Skills = Names(data.Skills.Select(s => s.Name));
        Items = Names(data.Items.Select(i => i.Name));
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
