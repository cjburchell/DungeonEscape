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
        var filesExamined = 0;

        foreach (var file in candidates.Paths)
        {
            if (filesExamined >= MaximumFiles)
            {
                truncated = true;
                break;
            }
            filesExamined++;

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
                if (IsSearchableFile(target)) paths.Add(target);
                continue;
            }
            CollectDirectory(target);
        }
        paths.Sort((left, right) => string.Compare(Relative(repositoryRoot, left),
            Relative(repositoryRoot, right), StringComparison.Ordinal));
        return (paths, truncated);

        void CollectDirectory(string directory)
        {
            try
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(directory)
                             .OrderBy(value => value, StringComparer.Ordinal))
                {
                    FileAttributes attributes;
                    try { attributes = File.GetAttributes(entry); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
                    if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        if (!ExcludedDirectories.Contains(Path.GetFileName(entry))) CollectDirectory(entry);
                    }
                    else if (IsSearchableFile(entry)) paths.Add(entry);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { truncated = true; }
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
        var start = Math.Max(0, matchIndex - (MaximumSnippetLength - queryLength) / 2);
        if (start + MaximumSnippetLength > clean.Length) start = clean.Length - MaximumSnippetLength;
        var length = MaximumSnippetLength;
        var prefix = start > 0;
        var suffix = start + length < clean.Length;
        if (prefix) { start += 1; length -= 1; }
        if (suffix) length -= 1;
        return (prefix ? "…" : "") + clean.Substring(start, length) + (suffix ? "…" : "");
    }
}
