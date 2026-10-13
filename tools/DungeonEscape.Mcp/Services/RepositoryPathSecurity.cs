namespace DungeonEscape.Mcp.Services;

/// <summary>Shared local-path checks for the trusted DungeonEscape checkout.</summary>
public static class RepositoryPathSecurity
{
    public static StringComparison Comparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static bool IsContained(string parent, string candidate)
    {
        var canonicalParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent));
        var canonicalCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
        if (canonicalCandidate.Equals(canonicalParent, Comparison)) return true;
        var prefix = Path.EndsInDirectorySeparator(canonicalParent)
            ? canonicalParent
            : canonicalParent + Path.DirectorySeparatorChar;
        return canonicalCandidate.StartsWith(prefix, Comparison);
    }

    public static bool TryResolveContainedRelativePath(string repositoryRoot, string relativePath,
        out string fullPath)
    {
        fullPath = "";
        if (string.IsNullOrEmpty(relativePath) || Path.IsPathRooted(relativePath)
            || Path.IsPathFullyQualified(relativePath)) return false;
        try
        {
            var segments = relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == "..")) return false;
            fullPath = Path.GetFullPath(Path.Combine(repositoryRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
            return IsContained(repositoryRoot, fullPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
        {
            fullPath = "";
            return false;
        }
    }

    public static string Relative(string repositoryRoot, string path) =>
        Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

    public static bool IsOrdinaryPath(string path)
    {
        try
        {
            for (FileSystemInfo? entry = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
                 entry is not null;
                 entry = entry is DirectoryInfo directory ? directory.Parent : ((FileInfo)entry).Directory)
            {
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }
}
