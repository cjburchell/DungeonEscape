using System.IO;

namespace DungeonEscape.Tools.MonsterEditor.Services;

/// <summary>
/// Tracks the Dungeon Escape asset root (the folder that contains the
/// <c>Tilesets/</c>, <c>Images/</c> and <c>Data/</c> sub-folders). The root is
/// auto-detected by walking up the directory tree from an opened JSON file, and
/// can be overridden manually if detection fails.
/// </summary>
public sealed class AssetContext
{
    /// <summary>Raised when the resolved asset root changes.</summary>
    public event Action? Changed;

    /// <summary>The absolute path to the asset root, or null if not yet resolved.</summary>
    public string? AssetRoot { get; private set; }

    public string? TilesetPath =>
        AssetRoot == null ? null : Path.Combine(AssetRoot, "Tilesets", "allmonsters.tsx");

    /// <summary>Tileset used for item images (items2.tsx).</summary>
    public string? ItemsTilesetPath =>
        AssetRoot == null ? null : Path.Combine(AssetRoot, "Tilesets", "items2.tsx");

    /// <summary>Tileset used for spell images (items.tsx).</summary>
    public string? SpellsTilesetPath =>
        AssetRoot == null ? null : Path.Combine(AssetRoot, "Tilesets", "items.tsx");


    public string? MonsterImagesDirectory =>
        AssetRoot == null ? null : Path.Combine(AssetRoot, "Images", "monsters");

    public string? DataDirectory =>
        AssetRoot == null ? null : Path.Combine(AssetRoot, "Data");

    /// <summary>
    /// Attempt to discover the asset root by walking up from the supplied path
    /// looking for a directory that contains <c>Tilesets/allmonsters.tsx</c>.
    /// </summary>
    public bool TryDetectFrom(string? startPath)
    {
        if (string.IsNullOrEmpty(startPath))
        {
            return false;
        }

        var directory = Directory.Exists(startPath)
            ? new DirectoryInfo(startPath)
            : new FileInfo(startPath).Directory;

        while (directory != null)
        {
            if (IsAssetRoot(directory.FullName))
            {
                SetRoot(directory.FullName);
                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }

    public void SetRoot(string? root)
    {
        if (string.Equals(AssetRoot, root, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AssetRoot = root;
        Changed?.Invoke();
    }

    private static bool IsAssetRoot(string directory)
    {
        return File.Exists(Path.Combine(directory, "Tilesets", "allmonsters.tsx")) &&
               Directory.Exists(Path.Combine(directory, "Images", "monsters"));
    }
}
