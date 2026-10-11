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
        var prefix = canonicalParent + Path.DirectorySeparatorChar;
        return canonicalCandidate.StartsWith(prefix, Comparison);
    }

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
