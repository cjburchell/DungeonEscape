using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Xml.Linq;
using Redpoint.DungeonEscape.Rules;

namespace DungeonEscape.Tools.GameEditor.Services;

public sealed record MapPreviewTile(
    int Gid,
    string TilesetName,
    int LocalTileId,
    int UsageCount,
    string? ImageUrl);

public sealed record MapPreview(
    string? MapImageUrl,
    int Width,
    int Height,
    int TileWidth,
    int TileHeight,
    IReadOnlyList<MapPreviewTile> UsedTiles,
    string? Error);

public sealed class MapPreviewCatalog
{
    private const uint TiledGidMask = 0x1FFFFFFF;

    public MapPreview GetPreview(MapDocument? map)
    {
        if (map?.Xml?.Root == null)
        {
            return Empty("Select a map to preview.");
        }

        try
        {
            return BuildPreview(map);
        }
        catch (Exception ex)
        {
            return Empty("Unable to render map preview: " + ex.Message);
        }
    }

    private static MapPreview BuildPreview(MapDocument map)
    {
        var root = map.Xml!.Root!;
        var mapWidth = ReadInt(root.Attribute("width"), 0);
        var mapHeight = ReadInt(root.Attribute("height"), 0);
        var tileWidth = ReadInt(root.Attribute("tilewidth"), 0);
        var tileHeight = ReadInt(root.Attribute("tileheight"), 0);
        if (mapWidth <= 0 || mapHeight <= 0 || tileWidth <= 0 || tileHeight <= 0)
        {
            return Empty("Map dimensions are missing or invalid.");
        }

        var tilesets = LoadTilesets(map.FilePath, root.Elements("tileset"), tileWidth, tileHeight);
        try
        {
            if (tilesets.Count == 0)
            {
                return Empty("No usable tilesets were found for this map.", mapWidth, mapHeight, tileWidth, tileHeight);
            }

            var identity = BuildMapIdentity(map.FilePath, tilesets);
            var mapCachePath = ImagePreviewCache.GetCachePath("maps", identity, 0);
            var usageCounts = new Dictionary<int, int>();
            var renderableElements = root.Elements()
                .Where(element =>
                    element.Name.LocalName == "layer" && IsVisible(element) ||
                    element.Name.LocalName == "objectgroup" && IsVisible(element))
                .ToList();

            if (!File.Exists(mapCachePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mapCachePath)!);
                RenderMap(mapCachePath, renderableElements, tilesets, mapWidth, mapHeight, tileWidth, tileHeight, usageCounts);
            }
            else
            {
                CountUsage(renderableElements, usageCounts);
            }

            var usedTiles = usageCounts
                .Where(pair => pair.Key > 0)
                .OrderBy(pair => pair.Key)
                .Select(pair => BuildUsedTile(identity, pair.Key, pair.Value, tilesets))
                .ToList();

            return new MapPreview(
                ImagePreviewCache.ToRelativeUrl(mapCachePath),
                mapWidth,
                mapHeight,
                tileWidth,
                tileHeight,
                usedTiles,
                null);
        }
        finally
        {
            foreach (var tileset in tilesets)
            {
                tileset.Image.Dispose();
            }
        }
    }

    private static void RenderMap(
        string mapCachePath,
        IEnumerable<XElement> renderableElements,
        IReadOnlyList<MapTileset> tilesets,
        int mapWidth,
        int mapHeight,
        int tileWidth,
        int tileHeight,
        Dictionary<int, int> usageCounts)
    {
        using var preview = new Bitmap(mapWidth * tileWidth, mapHeight * tileHeight, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(preview))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            foreach (var element in renderableElements)
            {
                if (element.Name.LocalName == "layer")
                {
                    RenderLayer(graphics, element, tilesets, mapWidth, tileWidth, tileHeight, usageCounts);
                }
                else if (element.Name.LocalName == "objectgroup")
                {
                    RenderObjectGroup(graphics, element, tilesets, tileWidth, tileHeight, usageCounts);
                }
            }
        }

        preview.Save(mapCachePath, ImageFormat.Png);
    }

    private static void RenderLayer(
        Graphics graphics,
        XElement layer,
        IReadOnlyList<MapTileset> tilesets,
        int fallbackMapWidth,
        int fallbackTileWidth,
        int fallbackTileHeight,
        Dictionary<int, int> usageCounts)
    {
        var layerWidth = ReadInt(layer.Attribute("width"), fallbackMapWidth);
        var data = layer.Element("data");
        if (data == null ||
            !string.Equals(data.Attribute("encoding")?.Value, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var gids = TiledTileData.ParseCsvTileData(data.Value);
        for (var index = 0; index < gids.Count; index++)
        {
            var gid = gids[index];
            if (gid <= 0)
            {
                continue;
            }

            usageCounts[gid] = usageCounts.GetValueOrDefault(gid) + 1;
            if (!TryGetTile(tilesets, gid, out var tileset, out var source))
            {
                continue;
            }

            var column = index % layerWidth;
            var row = index / layerWidth;
            graphics.DrawImage(
                tileset.Image,
                new Rectangle(column * fallbackTileWidth, row * fallbackTileHeight, fallbackTileWidth, fallbackTileHeight),
                source,
                GraphicsUnit.Pixel);
        }
    }

    private static void RenderObjectGroup(
        Graphics graphics,
        XElement objectGroup,
        IReadOnlyList<MapTileset> tilesets,
        int fallbackTileWidth,
        int fallbackTileHeight,
        Dictionary<int, int> usageCounts)
    {
        foreach (var mapObject in objectGroup.Elements("object"))
        {
            var gid = ReadGid(mapObject.Attribute("gid"));
            if (gid <= 0)
            {
                continue;
            }

            usageCounts[gid] = usageCounts.GetValueOrDefault(gid) + 1;
            if (!TryGetTile(tilesets, gid, out var tileset, out var source))
            {
                continue;
            }

            var width = ReadFloat(mapObject.Attribute("width"), fallbackTileWidth);
            var height = ReadFloat(mapObject.Attribute("height"), fallbackTileHeight);
            var x = ReadFloat(mapObject.Attribute("x"), 0);
            var y = ReadFloat(mapObject.Attribute("y"), 0) - height;
            graphics.DrawImage(
                tileset.Image,
                Rectangle.Round(new RectangleF(x, y, width, height)),
                source,
                GraphicsUnit.Pixel);
        }
    }

    private static void CountUsage(IEnumerable<XElement> renderableElements, Dictionary<int, int> usageCounts)
    {
        foreach (var element in renderableElements)
        {
            if (element.Name.LocalName == "layer")
            {
                var data = element.Element("data");
                if (data == null ||
                    !string.Equals(data.Attribute("encoding")?.Value, "csv", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var gid in TiledTileData.ParseCsvTileData(data.Value).Where(gid => gid > 0))
                {
                    usageCounts[gid] = usageCounts.GetValueOrDefault(gid) + 1;
                }
            }
            else if (element.Name.LocalName == "objectgroup")
            {
                foreach (var gid in element.Elements("object")
                             .Select(item => ReadGid(item.Attribute("gid")))
                             .Where(gid => gid > 0))
                {
                    usageCounts[gid] = usageCounts.GetValueOrDefault(gid) + 1;
                }
            }
        }
    }

    private static MapPreviewTile BuildUsedTile(
        string identity,
        int gid,
        int usageCount,
        IReadOnlyList<MapTileset> tilesets)
    {
        if (!TryGetTile(tilesets, gid, out var tileset, out var source))
        {
            return new MapPreviewTile(gid, "(missing)", 0, usageCount, null);
        }

        var tileCachePath = ImagePreviewCache.GetCachePath("map-tiles", identity, gid);
        if (!File.Exists(tileCachePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tileCachePath)!);
            using var tile = new Bitmap(tileset.TileWidth, tileset.TileHeight, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(tile))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                graphics.DrawImage(
                    tileset.Image,
                    new Rectangle(0, 0, tileset.TileWidth, tileset.TileHeight),
                    source,
                    GraphicsUnit.Pixel);
            }

            tile.Save(tileCachePath, ImageFormat.Png);
        }

        return new MapPreviewTile(
            gid,
            tileset.Name,
            gid - tileset.FirstGid,
            usageCount,
            ImagePreviewCache.ToRelativeUrl(tileCachePath));
    }

    private static bool TryGetTile(
        IReadOnlyList<MapTileset> tilesets,
        int gid,
        out MapTileset tileset,
        out Rectangle source)
    {
        tileset = null!;
        source = Rectangle.Empty;

        foreach (var candidate in tilesets)
        {
            if (candidate.FirstGid <= gid)
            {
                tileset = candidate;
            }
            else
            {
                break;
            }
        }

        if (tileset == null)
        {
            return false;
        }

        var localTileId = gid - tileset.FirstGid;
        if (localTileId < 0 || tileset.TileCount > 0 && localTileId >= tileset.TileCount)
        {
            return false;
        }

        var column = localTileId % tileset.Columns;
        var row = localTileId / tileset.Columns;
        source = new Rectangle(
            tileset.Margin + column * (tileset.TileWidth + tileset.Spacing),
            tileset.Margin + row * (tileset.TileHeight + tileset.Spacing),
            tileset.TileWidth,
            tileset.TileHeight);

        return source.Right <= tileset.Image.Width && source.Bottom <= tileset.Image.Height;
    }

    private static List<MapTileset> LoadTilesets(
        string mapPath,
        IEnumerable<XElement> tilesetElements,
        int fallbackTileWidth,
        int fallbackTileHeight)
    {
        var result = new List<MapTileset>();
        foreach (var tilesetElement in tilesetElements)
        {
            var firstGid = ReadInt(tilesetElement.Attribute("firstgid"), 0);
            var source = tilesetElement.Attribute("source")?.Value;
            var tilesetPath = string.IsNullOrWhiteSpace(source)
                ? mapPath
                : ResolveRelativePath(Path.GetDirectoryName(mapPath) ?? string.Empty, source);
            var tilesetXml = string.IsNullOrWhiteSpace(source)
                ? tilesetElement.ToString()
                : File.Exists(tilesetPath)
                    ? File.ReadAllText(tilesetPath)
                    : null;

            if (string.IsNullOrEmpty(tilesetXml))
            {
                continue;
            }

            var tilesetDocument = XDocument.Parse(tilesetXml);
            var tilesetRoot = tilesetDocument.Root;
            var imageElement = tilesetRoot?.Element("image");
            var imageSource = imageElement?.Attribute("source")?.Value;
            if (tilesetRoot == null || string.IsNullOrWhiteSpace(imageSource))
            {
                continue;
            }

            var imagePath = ResolveRelativePath(Path.GetDirectoryName(tilesetPath) ?? string.Empty, imageSource);
            if (!File.Exists(imagePath))
            {
                continue;
            }

            var image = LoadBitmap(imagePath, imageElement!.Attribute("trans")?.Value);
            var tileWidth = ReadInt(tilesetRoot.Attribute("tilewidth"), fallbackTileWidth);
            var tileHeight = ReadInt(tilesetRoot.Attribute("tileheight"), fallbackTileHeight);
            var spacing = ReadInt(tilesetRoot.Attribute("spacing"), 0);
            var margin = ReadInt(tilesetRoot.Attribute("margin"), 0);
            var columns = ReadInt(tilesetRoot.Attribute("columns"), 0);
            if (columns <= 0)
            {
                columns = (image.Width - margin + spacing) / (tileWidth + spacing);
            }

            if (tileWidth <= 0 || tileHeight <= 0 || columns <= 0)
            {
                image.Dispose();
                continue;
            }

            result.Add(new MapTileset(
                firstGid,
                ReadString(tilesetRoot.Attribute("name"), Path.GetFileNameWithoutExtension(tilesetPath)),
                tilesetPath,
                imagePath,
                image,
                tileWidth,
                tileHeight,
                columns,
                ReadInt(tilesetRoot.Attribute("tilecount"), 0),
                spacing,
                margin));
        }

        return result.OrderBy(item => item.FirstGid).ToList();
    }

    private static Bitmap LoadBitmap(string imagePath, string? transparentColor)
    {
        var bytes = File.ReadAllBytes(imagePath);
        using var stream = new MemoryStream(bytes);
        using var original = new Bitmap(stream);
        var bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.DrawImage(original, 0, 0, original.Width, original.Height);
        }

        if (!string.IsNullOrWhiteSpace(transparentColor) && transparentColor.Length == 6)
        {
            var color = Color.FromArgb(
                int.Parse(transparentColor[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(transparentColor[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(transparentColor[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            bitmap.MakeTransparent(color);
        }

        return bitmap;
    }

    private static string BuildMapIdentity(string mapPath, IEnumerable<MapTileset> tilesets)
    {
        var parts = new List<string> { ImagePreviewCache.GetSourceIdentity(mapPath) };
        foreach (var tileset in tilesets)
        {
            parts.Add(ImagePreviewCache.GetSourceIdentity(tileset.TilesetPath));
            parts.Add(ImagePreviewCache.GetSourceIdentity(tileset.ImagePath));
        }

        return string.Join("|", parts);
    }

    private static MapPreview Empty(
        string error,
        int width = 0,
        int height = 0,
        int tileWidth = 0,
        int tileHeight = 0) =>
        new(null, width, height, tileWidth, tileHeight, Array.Empty<MapPreviewTile>(), error);

    private static bool IsVisible(XElement element) =>
        !string.Equals(element.Attribute("visible")?.Value, "0", StringComparison.OrdinalIgnoreCase);

    private static int ReadInt(XAttribute? attribute, int fallback) =>
        attribute != null && int.TryParse(attribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    private static int ReadGid(XAttribute? attribute) =>
        attribute != null && uint.TryParse(attribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? (int)(value & TiledGidMask)
            : 0;

    private static float ReadFloat(XAttribute? attribute, float fallback) =>
        attribute != null && float.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    private static string ReadString(XAttribute? attribute, string fallback) =>
        string.IsNullOrWhiteSpace(attribute?.Value) ? fallback : attribute.Value;

    private static string ResolveRelativePath(string directory, string relativePath) =>
        Path.GetFullPath(Path.Combine(directory, relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));

    private sealed record MapTileset(
        int FirstGid,
        string Name,
        string TilesetPath,
        string ImagePath,
        Bitmap Image,
        int TileWidth,
        int TileHeight,
        int Columns,
        int TileCount,
        int Spacing,
        int Margin);
}
