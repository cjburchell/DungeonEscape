using System.Text;

namespace DungeonEscape.Mcp.Services;

public sealed record SearchCodeResult(string Query, string? SearchRoot, bool Truncated,
    IReadOnlyList<SearchCodeMatch> Matches);
public sealed record SearchCodeMatch(string Path, int Line, string Snippet);

/// <summary>Bounded literal UTF-8 search over hard-coded repository areas.</summary>
public sealed class SearchCodeService(ProjectInfoService projectInfo)
{
    public const int DefaultMaxResults = 20;
    public const int MaximumResults = 50;
    public const int MaximumQueryLength = 128;
    public const int MaximumSnippetLength = 240;
    public const long MaximumFileBytes = 1024 * 1024;
    public const long MaximumTotalBytes = 25 * 1024 * 1024;
    public const int MaximumFiles = 5000;

    public static readonly IReadOnlyList<string> ApprovedRoots =
    [
        "DungeonEscape.Core",
        "DungeonEscape.Unity/Assets/DungeonEscape/Scripts",
        "DungeonEscape.Tools.GameEditor",
        "tools",
        ".ai",
        "memory-bank"
    ];

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "Library", "Temp", "Obj", "obj", "bin", "Build", "Builds", "Logs",
        "UserSettings", "TestResults", "artifacts"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".props", ".targets", ".razor", ".json", ".md", ".txt",
        ".xml", ".yml", ".yaml", ".ps1", ".sh", ".asmdef", ".uxml", ".uss"
    };

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public SearchCodeResult Search(string query, string? path = null, int? maxResults = null)
    {
        ValidateQuery(query);
        var limit = maxResults ?? DefaultMaxResults;
        if (limit is < 1 or > MaximumResults)
            throw new ArgumentOutOfRangeException(nameof(maxResults), $"maxResults must be between 1 and {MaximumResults}.");

        var repositoryRoot = projectInfo.DiscoverRepositoryRoot()
            ?? throw new InvalidOperationException("DungeonEscape repository root is not available.");
        var normalizedPath = NormalizeRequestedPath(path);
        var searchTargets = ResolveSearchTargets(repositoryRoot, normalizedPath);
        var candidates = CollectCandidates(repositoryRoot, searchTargets);
        var matches = new List<SearchCodeMatch>();
        var truncated = candidates.Truncated;
        long totalBytes = 0;

        foreach (var file in candidates.Paths)
        {
            try
            {
                if (!RepositoryPathSecurity.IsOrdinaryPath(file)) continue;
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read,
                    4096, FileOptions.SequentialScan);
                if (stream.Length > MaximumFileBytes)
                {
                    truncated = true;
                    continue;
                }
                if (totalBytes + stream.Length > MaximumTotalBytes)
                {
                    truncated = true;
                    break;
                }

                var bytes = new byte[stream.Length + 1];
                var count = 0;
                while (count < bytes.Length)
                {
                    var read = stream.Read(bytes, count, bytes.Length - count);
                    if (read == 0) break;
                    count += read;
                }
                if (count > MaximumFileBytes)
                {
                    truncated = true;
                    continue;
                }
                totalBytes += count;
                if (Array.IndexOf(bytes, (byte)0, 0, count) >= 0) continue;

                string text;
                try { text = StrictUtf8.GetString(bytes, 0, count); }
                catch (DecoderFallbackException) { continue; }

                var relative = Relative(repositoryRoot, file);
                using var reader = new StringReader(text);
                for (var lineNumber = 1; reader.ReadLine() is { } line; lineNumber++)
                {
                    var matchIndex = line.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                    if (matchIndex < 0) continue;
                    if (matches.Count == limit)
                    {
                        truncated = true;
                        return new(query, normalizedPath, truncated, matches);
                    }
                    matches.Add(new(relative, lineNumber, CreateSnippet(line, matchIndex, query.Length)));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }

        return new(query, normalizedPath, truncated, matches);
    }

    private static void ValidateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("query is required.", nameof(query));
        if (query.Length > MaximumQueryLength)
            throw new ArgumentException($"query must be at most {MaximumQueryLength} characters.", nameof(query));
        if (query.Any(char.IsControl))
            throw new ArgumentException("query must not contain control characters.", nameof(query));
    }

    private static string? NormalizeRequestedPath(string? path)
    {
        if (path is null) return null;
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path must not be empty.", nameof(path));
        if (Path.IsPathRooted(path) || Path.IsPathFullyQualified(path))
            throw new ArgumentException("path must be repository-relative.", nameof(path));
        var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment == ".."))
            throw new ArgumentException("path must not contain '..'.", nameof(path));
        if (segments.Any(segment => ExcludedDirectories.Contains(segment)))
            throw new ArgumentException("path contains an excluded directory.", nameof(path));
        return string.Join('/', segments.Where(segment => segment != "."));
    }

    private static IReadOnlyList<string> ResolveSearchTargets(string repositoryRoot, string? normalizedPath)
    {
        var approved = ApprovedRoots.Select(root => Path.GetFullPath(Path.Combine(repositoryRoot,
            root.Replace('/', Path.DirectorySeparatorChar)))).ToArray();
        if (normalizedPath is null)
            return approved.Where(path => Directory.Exists(path) && RepositoryPathSecurity.IsOrdinaryPath(path)).ToArray();

        var candidate = Path.GetFullPath(Path.Combine(repositoryRoot,
            normalizedPath.Replace('/', Path.DirectorySeparatorChar)));
        if (!RepositoryPathSecurity.IsContained(repositoryRoot, candidate))
            throw new ArgumentException("path escapes the repository root.", nameof(normalizedPath));
        if (!approved.Any(root => RepositoryPathSecurity.IsContained(root, candidate)))
            throw new ArgumentException("path is outside the approved search roots.", nameof(normalizedPath));
        if (!File.Exists(candidate) && !Directory.Exists(candidate))
            throw new ArgumentException("path does not exist.", nameof(normalizedPath));
        if (!RepositoryPathSecurity.IsOrdinaryPath(candidate))
            throw new ArgumentException("linked or reparse-point search roots are not allowed.", nameof(normalizedPath));
        return [candidate];
    }

    private static (IReadOnlyList<string> Paths, bool Truncated) CollectCandidates(string repositoryRoot,
        IReadOnlyList<string> searchTargets)
    {
        var paths = new List<string>();
        var truncated = false;
        foreach (var target in searchTargets)
        {
            if (File.Exists(target))
            {
                if (IsSearchableFile(target) && !TryAdd(target)) break;
                continue;
            }
            if (!CollectDirectory(target)) break;
        }
        paths.Sort((left, right) => string.Compare(Relative(repositoryRoot, left),
            Relative(repositoryRoot, right), StringComparison.Ordinal));
        return (paths, truncated);

        bool TryAdd(string file)
        {
            paths.Add(file);
            if (paths.Count == MaximumFiles)
            {
                truncated = true;
                return false;
            }
            return true;
        }

        bool CollectDirectory(string directory)
        {
            try
            {
                // Buffer only one directory's entries so traversal order is stable without ever
                // collecting an unbounded repository-wide candidate list.
                var entries = Directory.EnumerateFileSystemEntries(directory)
                    .OrderBy(entry => Relative(repositoryRoot, entry), StringComparer.Ordinal);
                foreach (var entry in entries)
                {
                    FileAttributes attributes;
                    try { attributes = File.GetAttributes(entry); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
                    if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        if (!ExcludedDirectories.Contains(Path.GetFileName(entry)) && !CollectDirectory(entry))
                            return false;
                    }
                    else if (IsSearchableFile(entry) && !TryAdd(entry)) return false;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { truncated = true; }
            return !truncated;
        }
    }

    private static bool IsSearchableFile(string path) => TextExtensions.Contains(Path.GetExtension(path));

    private static string Relative(string repositoryRoot, string path) =>
        Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

    private static string CreateSnippet(string line, int matchIndex, int queryLength)
    {
        var clean = new string(line.Select(character => character == '\t' ? ' '
            : char.IsControl(character) ? ' ' : character).ToArray());
        if (clean.Length <= MaximumSnippetLength) return clean;

        var contentLength = MaximumSnippetLength - 2;
        var start = Math.Clamp(matchIndex - (contentLength - queryLength) / 2,
            0, clean.Length - contentLength);
        var prefix = start > 0;
        var suffix = start + contentLength < clean.Length;
        contentLength = MaximumSnippetLength - (prefix ? 1 : 0) - (suffix ? 1 : 0);
        start = Math.Clamp(matchIndex - (contentLength - queryLength) / 2,
            0, clean.Length - contentLength);

        // Never start on the low half or end after the high half of a surrogate pair.
        if (start > 0 && char.IsLowSurrogate(clean[start]) && char.IsHighSurrogate(clean[start - 1]))
            start++;
        var end = Math.Min(clean.Length, start + contentLength);
        if (end < clean.Length && char.IsHighSurrogate(clean[end - 1]) && char.IsLowSurrogate(clean[end]))
            end--;

        prefix = start > 0;
        suffix = end < clean.Length;
        return (prefix ? "\u2026" : "") + clean[start..end] + (suffix ? "\u2026" : "");
    }
}
