namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// Item image catalog backed by <c>items2.tsx</c>.
/// </summary>
public sealed class ItemImageCatalog
{
    public ItemImageCatalog(AssetContext context)
    {
        Catalog = new TilesetImageCatalog(context, ctx => ctx.ItemsTilesetPath);
    }

    public TilesetImageCatalog Catalog { get; }
}
