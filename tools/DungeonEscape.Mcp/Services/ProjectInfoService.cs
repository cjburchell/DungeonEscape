using System.Text.RegularExpressions;

namespace DungeonEscape.Mcp.Services;

public sealed record ProjectInfo(string ProjectName, string UnityVersion, string RepositoryRoot,
    string CurrentBranch, string DefaultBranch);

/// <summary>Trusted discovery, independent of MCP and caller-controlled paths.</summary>
public sealed class ProjectInfoService(string applicationDirectory)
{
    public const string Unavailable = "not available";

    public ProjectInfo GetProjectInfo()
    {
        var root = DiscoverRepositoryRoot();
        if (root is null)
            return new(Unavailable, Unavailable, Unavailable, Unavailable, Unavailable);

        var version = ReadMetadata(root, "DungeonEscape.Unity/ProjectSettings/ProjectVersion.txt");
        var match = Regex.Match(version ?? "", @"(?m)^m_EditorVersion: (\d+\.\d+\.\d+[abfp]\d+)\r?$",
            RegexOptions.CultureInvariant);
        // Only ordinary .git directories with loose symbolic HEAD metadata are supported.
        var git = Path.Combine(root, ".git");
        var simpleGit = Directory.Exists(git) && IsOrdinaryPath(git)
            && !File.Exists(Path.Combine(git, "commondir"));
        return new("DungeonEscape", match.Success ? match.Groups[1].Value : Unavailable, root,
            simpleGit ? ReadBranch(ReadMetadata(root, ".git/HEAD"), "ref: refs/heads/") : Unavailable,
            simpleGit ? ReadBranch(ReadMetadata(root, ".git/refs/remotes/origin/HEAD"),
                "ref: refs/remotes/origin/") : Unavailable);
    }

    public string? DiscoverRepositoryRoot()
    {
        try
        {
            var directory = new DirectoryInfo(Path.GetFullPath(applicationDirectory));
            if (!directory.Exists || !IsOrdinaryPath(directory.FullName)) return null;
            // Stop at the server's own project directory. Never search arbitrary parent repos.
            for (; directory is not null; directory = directory.Parent)
            {
                if (directory.Name != "DungeonEscape.Mcp") continue;
                if (directory.Parent?.Name != "tools") return null;
                var root = directory.Parent.Parent?.FullName;
                return root is not null
                    && File.Exists(Path.Combine(root, "DungeonEscape.sln"))
                    && Directory.Exists(Path.Combine(root, "DungeonEscape.Core"))
                    && Directory.Exists(Path.Combine(root, "DungeonEscape.Unity"))
                    && IsOrdinaryPath(Path.Combine(root, "DungeonEscape.sln"))
                    && IsOrdinaryPath(Path.Combine(root, "DungeonEscape.Core"))
                    && IsOrdinaryPath(Path.Combine(root, "DungeonEscape.Unity")) ? root : null;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
        return null;
    }

    private static string? ReadMetadata(string root, string relativePath)
    {
        try
        {
            var path = Path.GetFullPath(Path.Combine(root, relativePath));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(path) || !IsOrdinaryPath(path)) return null;
            // Bound reads even if metadata is malformed or changes while being read.
            using var reader = new StreamReader(path);
            var buffer = new char[4097];
            var count = reader.ReadBlock(buffer, 0, buffer.Length);
            return count > 4096 ? null : new string(buffer, 0, count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
    }

    private static string ReadBranch(string? value, string prefix)
    {
        var line = value?.TrimEnd('\r', '\n');
        if (line is null || !line.StartsWith(prefix, StringComparison.Ordinal)) return Unavailable;
        var branch = line[prefix.Length..];
        // Deliberately conservative subset; no resolving refs as filesystem paths.
        return Regex.IsMatch(branch, @"\A[A-Za-z0-9_-]+(?:[./][A-Za-z0-9_-]+)*\z",
            RegexOptions.CultureInvariant) ? branch : Unavailable;
    }

    private static bool IsOrdinaryPath(string path)
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
