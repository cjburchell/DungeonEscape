using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Linq;

namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// A single selectable image (a tile) from a grid-based tileset.
/// </summary>
public sealed record TilesetImageEntry(int ImageId, string DisplayName);

/// <summary>
/// Slices a grid-based tileset PNG (such as <c>items2.tsx</c> for items or
/// <c>items.tsx</c> for spells) into individual tile images indexed by ImageId.
/// Mirrors the slicing logic used by the game's <c>TilesetSprites</c> so the
/// editor previews match in-game rendering. Tiles are produced lazily and cached
/// as base64 PNG data URIs for display inside the WebView.
/// </summary>
public sealed class TilesetImageCatalog
{
    private readonly AssetContext context;
    private readonly Func<AssetContext, string?> tilesetPathSelector;

    private readonly Dictionary<int, string> dataUriCache = new();
    private List<TilesetImageEntry> entries = new();

    private Bitmap? sourceBitmap;
    private int tileWidth;
    private int tileHeight;
    private int columns;
    private int tileCount;
    private int spacing;
    private int margin;

    public TilesetImageCatalog(AssetContext context, Func<AssetContext, string?> tilesetPathSelector)
    {
        this.context = context;
        this.tilesetPathSelector = tilesetPathSelector;
        this.context.Changed += Reload;
        Reload();
    }

    public IReadOnlyList<TilesetImageEntry> Entries => entries;

    public bool TryGet(int imageId, out TilesetImageEntry entry)
    {
        foreach (var candidate in entries)
        {
            if (candidate.ImageId == imageId)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null!;
        return false;
    }

    /// <summary>
    /// Returns a base64 PNG data URI for the given tile, or null when the tile
    /// cannot be resolved.
    /// </summary>
    public string? GetImageDataUri(int imageId)
    {
        if (imageId < 0 || sourceBitmap == null || columns <= 0)
        {
            return null;
        }

        if (tileCount > 0 && imageId >= tileCount)
        {
            return null;
        }

        if (dataUriCache.TryGetValue(imageId, out var cached))
        {
            return cached;
        }

        try
        {
            var column = imageId % columns;
            var row = imageId / columns;
            var sourceX = margin + column * (tileWidth + spacing);
            var sourceY = margin + row * (tileHeight + spacing);

            if (sourceX + tileWidth > sourceBitmap.Width || sourceY + tileHeight > sourceBitmap.Height)
            {
                return null;
            }

            using var tile = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(tile))
            {
                g.DrawImage(
                    sourceBitmap,
                    new Rectangle(0, 0, tileWidth, tileHeight),
                    new Rectangle(sourceX, sourceY, tileWidth, tileHeight),
                    GraphicsUnit.Pixel);
            }

            using var stream = new MemoryStream();
            tile.Save(stream, ImageFormat.Png);
            var uri = "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
            dataUriCache[imageId] = uri;
            return uri;
        }
        catch
        {
            return null;
        }
    }

    public void Reload()
    {
        dataUriCache.Clear();
        entries = new List<TilesetImageEntry>();
        sourceBitmap?.Dispose();
        sourceBitmap = null;

        var tilesetPath = tilesetPathSelector(context);
        if (string.IsNullOrEmpty(tilesetPath) || !File.Exists(tilesetPath))
        {
            return;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(File.ReadAllText(tilesetPath));
        }
        catch
        {
            return;
        }

        var root = document.Root;
        if (root == null)
        {
            return;
        }

        tileWidth = ReadInt(root.Attribute("tilewidth"), 0);
        tileHeight = ReadInt(root.Attribute("tileheight"), 0);
        columns = ReadInt(root.Attribute("columns"), 0);
        tileCount = ReadInt(root.Attribute("tilecount"), 0);
        spacing = ReadInt(root.Attribute("spacing"), 0);
        margin = ReadInt(root.Attribute("margin"), 0);

        var image = root.Element("image");
        var source = image?.Attribute("source")?.Value;
        if (string.IsNullOrEmpty(source) || tileWidth <= 0 || tileHeight <= 0)
        {
            return;
        }

        var imagePath = ResolveImageAssetPath(tilesetPath, source);
        if (!File.Exists(imagePath))
        {
            return;
        }

        try
        {
            // Load via a stream copy so we don't keep a file lock.
            var bytes = File.ReadAllBytes(imagePath);
            using var ms = new MemoryStream(bytes);
            sourceBitmap = new Bitmap(ms);
        }
        catch
        {
            sourceBitmap = null;
            return;
        }

        if (columns <= 0)
        {
            columns = (sourceBitmap.Width - margin + spacing) / (tileWidth + spacing);
        }

        var rows = tileCount > 0
            ? (tileCount + columns - 1) / columns
            : (sourceBitmap.Height - margin + spacing) / (tileHeight + spacing);

        var total = tileCount > 0 ? tileCount : rows * columns;
        for (var id = 0; id < total; id++)
        {
            entries.Add(new TilesetImageEntry(id, $"#{id}"));
        }
    }

    private static int ReadInt(XAttribute? attribute, int fallback)
    {
        return attribute != null && int.TryParse(attribute.Value, out var value) ? value : fallback;
    }

    private static string ResolveImageAssetPath(string tilesetPath, string source)
    {
        var tilesetDir = Path.GetDirectoryName(tilesetPath) ?? string.Empty;
        var combined = Path.GetFullPath(Path.Combine(tilesetDir, source.Replace('\\', '/')));
        return combined;
    }
}
