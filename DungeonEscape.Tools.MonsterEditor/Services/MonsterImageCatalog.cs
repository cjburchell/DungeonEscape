using System.IO;
using System.Xml.Linq;

namespace DungeonEscape.Tools.MonsterEditor.Services;

/// <summary>
/// Describes a single selectable monster image (a tile in allmonsters.tsx).
/// </summary>
public sealed record MonsterImageEntry(int ImageId, string DisplayName, string ImagePath);

/// <summary>
/// Parses <c>allmonsters.tsx</c> to build the ImageId &rarr; PNG mapping used by
/// the editor's image dropdown and thumbnails. Mirrors the resolution logic used
/// by the game's CombatAssetLoader.
/// </summary>
public sealed class MonsterImageCatalog
{
    private readonly AssetContext context;
    private readonly Dictionary<int, MonsterImageEntry> entriesById = new();
    private List<MonsterImageEntry> entries = new();

    public MonsterImageCatalog(AssetContext context)
    {
        this.context = context;
        this.context.Changed += Reload;
        Reload();
    }

    public IReadOnlyList<MonsterImageEntry> Entries => entries;

    public bool TryGet(int imageId, out MonsterImageEntry entry) =>
        entriesById.TryGetValue(imageId, out entry!);

    public string? GetImagePath(int imageId) =>
        entriesById.TryGetValue(imageId, out var entry) ? entry.ImagePath : null;

    public string GetDisplayName(int imageId) =>
        entriesById.TryGetValue(imageId, out var entry)
            ? entry.DisplayName
            : $"#{imageId} (unknown)";

    /// <summary>
    /// Returns a base64 data URI for the monster image so it can be rendered
    /// directly inside the WebView, or null if the image cannot be found.
    /// </summary>
    public string? GetImageDataUri(int imageId)
    {
        var path = GetImagePath(imageId);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
        catch
        {
            return null;
        }
    }


    public void Reload()
    {
        entriesById.Clear();
        entries = new List<MonsterImageEntry>();

        var tilesetPath = context.TilesetPath;
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

        foreach (var tile in root.Elements("tile"))
        {
            var image = tile.Element("image");
            var idAttribute = tile.Attribute("id");
            var sourceAttribute = image?.Attribute("source");

            if (image == null || idAttribute == null || sourceAttribute == null ||
                !int.TryParse(idAttribute.Value, out var id))
            {
                continue;
            }

            var imagePath = ResolveImageAssetPath(sourceAttribute.Value);
            var className = (string?)tile.Attribute("class");
            var fileName = Path.GetFileNameWithoutExtension(imagePath);
            var displayName = string.IsNullOrEmpty(className)
                ? $"#{id} ({fileName})"
                : $"#{id} {className}";

            var entry = new MonsterImageEntry(id, displayName, imagePath);
            entriesById[id] = entry;
            entries.Add(entry);
        }

        entries.Sort((a, b) => a.ImageId.CompareTo(b.ImageId));
    }

    private string ResolveImageAssetPath(string source)
    {
        var normalized = source.Replace('\\', '/');
        while (normalized.StartsWith("../", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(3);
        }

        const string imagesPrefix = "Images/";
        if (normalized.StartsWith(imagesPrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(imagesPrefix.Length);
        }

        var assetRoot = context.AssetRoot ?? string.Empty;
        var combined = Path.Combine(assetRoot, "Images", normalized.Replace('/', Path.DirectorySeparatorChar));

        // Tileset sources are not always case-correct (e.g. Squid.png vs squid.png);
        // resolve case-insensitively against the real file on disk.
        return ResolveCaseInsensitive(combined);
    }

    private static string ResolveCaseInsensitive(string path)
    {
        if (File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName) || !Directory.Exists(directory))
        {
            return path;
        }

        foreach (var candidate in Directory.GetFiles(directory))
        {
            if (string.Equals(Path.GetFileName(candidate), fileName, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return path;
    }
}
