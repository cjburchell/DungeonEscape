namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// Spell image catalog backed by <c>items.tsx</c>.
/// </summary>
public sealed class SpellImageCatalog
{
    public SpellImageCatalog(AssetContext context)
    {
        Catalog = new TilesetImageCatalog(context, ctx => ctx.SpellsTilesetPath);
    }

    public TilesetImageCatalog Catalog { get; }
}
