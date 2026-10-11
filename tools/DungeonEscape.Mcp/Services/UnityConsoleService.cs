using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DungeonEscape.Mcp.Services;

public sealed record UnityConsoleResult(string Status, string? SourceType, bool Truncated,
    IReadOnlyList<string> Warnings, IReadOnlyList<UnityConsoleEntry> Entries);

public sealed record UnityConsoleEntry(string Severity, string? Timestamp, string Text);

/// <summary>Bounded inspection of fixed, project-scoped Unity logs created by repository scripts.</summary>
public sealed class UnityConsoleService(ProjectInfoService projectInfo)
{
    public const int DefaultMaximumEntries = 50;
    public const int MaximumEntries = 100;
    public const int MaximumEntryCharacters = 2000;
    public const int MaximumReturnedCharacters = 64 * 1024;
    public const long MaximumSourceFileBytes = 10 * 1024 * 1024;
    public const int MaximumTailBytes = 256 * 1024;

    public const string Available = "available";
    public const string Unavailable = "unavailable";
    public const string Malformed = "malformed";
    public const string Oversized = "oversized";

    private static readonly (string Path, string Type)[] Sources =
    [
        ("TestResults/editmode-log.txt", "project-editmode-test"),
        ("TestResults/playmode-log.txt", "project-playmode-test"),
        ("TestResults/unity-build-windows-log.txt", "project-windows-build"),
        ("DungeonEscape.Unity/Logs/local-unity-validate.log", "project-local-validation"),
        ("DungeonEscape.Unity/Logs/ci-unity-validate.log", "project-ci-validation"),
        ("DungeonEscape.Unity/Logs/ci-unity-build-windows.log", "project-ci-windows-build")
    ];

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly Regex Secret = new(
        """(?i)\b(api[-_ ]?key|access[-_ ]?token|refresh[-_ ]?token|client[-_ ]?secret|secret[-_ ]?key|token|password|passwd|secret|authorization)\b\s*[:=]\s*(?:"[^"]*"|'[^']*'|\S+)""",
        RegexOptions.CultureInvariant);
    private static readonly Regex Bearer = new(@"(?i)\bbearer\s+[A-Za-z0-9._~+/=-]+",
        RegexOptions.CultureInvariant);
    private static readonly Regex WindowsPath = new("""(?<![A-Za-z0-9])[A-Za-z]:[\\/][^\s"'<>|]+""",
        RegexOptions.CultureInvariant);
    private static readonly Regex UnixPath = new("""(?<![A-Za-z0-9.>])/(?:[^\s"'<>|/]+/)*[^\s"'<>|/]*""",
        RegexOptions.CultureInvariant);
    private static readonly Regex TimestampPrefix = new(
        @"^\[?(?<value>\d{4}-\d{2}-\d{2}[T ][0-9:.]+(?:Z|[+-]\d{2}:?\d{2}))\]?\s*",
        RegexOptions.CultureInvariant);

    public UnityConsoleResult Get(int? maxEntries = null, string? minimumSeverity = null)
    {
        var limit = maxEntries ?? DefaultMaximumEntries;
        if (limit is < 1 or > MaximumEntries)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), $"maxEntries must be between 1 and {MaximumEntries}.");
        var minimum = ParseSeverity(minimumSeverity);
        var repositoryRoot = projectInfo.DiscoverRepositoryRoot()
            ?? throw new InvalidOperationException("DungeonEscape repository root is not available.");
        var source = SelectNewestSource(repositoryRoot);
        if (source is null)
            return new(Unavailable, null, false,
                ["No project-scoped Unity log produced by the repository scripts is available."], []);

        try
        {
            using var stream = new FileStream(source.Value.FullPath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan);
            if (stream.Length > MaximumSourceFileBytes)
                return new(Oversized, source.Value.Type, true,
                    [$"The selected project-scoped Unity log exceeds {MaximumSourceFileBytes} bytes."], []);

            var tailLength = (int)Math.Min(stream.Length, MaximumTailBytes);
            var skippedPrefix = stream.Length > tailLength;
            stream.Seek(-tailLength, SeekOrigin.End);
            var bytes = new byte[tailLength];
            var count = 0;
            while (count < bytes.Length)
            {
                var read = stream.Read(bytes, count, bytes.Length - count);
                if (read == 0) break;
                count += read;
            }
            if (Array.IndexOf(bytes, (byte)0, 0, count) >= 0)
                return new(Malformed, source.Value.Type, skippedPrefix,
                    ["The selected log is not valid UTF-8 text."], []);
            string? text = null;
            var maximumStart = skippedPrefix ? Math.Min(3, count) : 0;
            for (var start = 0; start <= maximumStart && text is null; start++)
            {
                try { text = StrictUtf8.GetString(bytes, start, count - start); }
                catch (DecoderFallbackException) { }
            }
            if (text is null)
            {
                return new(Malformed, source.Value.Type, skippedPrefix,
                    ["The selected log is not valid UTF-8 text."], []);
            }
            if (skippedPrefix)
            {
                var firstLineEnd = text.IndexOf('\n');
                text = firstLineEnd < 0 ? "" : text[(firstLineEnd + 1)..];
            }
            return CreateResult(text, repositoryRoot, source.Value.Type, minimum, limit, skippedPrefix);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(Unavailable, source.Value.Type, false,
                ["The selected project-scoped Unity log could not be read."], []);
        }
    }

    private static UnityConsoleResult CreateResult(string text, string repositoryRoot, string sourceType,
        Severity minimum, int limit, bool initiallyTruncated)
    {
        var parsed = ParseEntries(text, repositoryRoot).Where(entry => entry.Level >= minimum).ToArray();
        var selected = parsed.Length > limit ? parsed[^limit..] : parsed;
        var truncated = initiallyTruncated || parsed.Length > limit;
        var returnedNewestFirst = new List<UnityConsoleEntry>();
        var totalCharacters = 0;
        for (var index = selected.Length - 1; index >= 0; index--)
        {
            var entry = selected[index];
            var value = entry.Text;
            if (value.Length > MaximumEntryCharacters)
            {
                value = value[..(MaximumEntryCharacters - 1)] + "…";
                truncated = true;
            }
            if (totalCharacters + value.Length > MaximumReturnedCharacters)
            {
                truncated = true;
                break;
            }
            totalCharacters += value.Length;
            returnedNewestFirst.Add(new(entry.Level.ToString().ToLowerInvariant(), entry.Timestamp, value));
        }
        returnedNewestFirst.Reverse();
        return new(Available, sourceType, truncated,
            ["Only fixed project-scoped script logs are inspected; the live Unity Console and global Editor/Player logs are not read."],
            returnedNewestFirst);
    }

    private static IReadOnlyList<ParsedEntry> ParseEntries(string text, string repositoryRoot)
    {
        var entries = new List<ParsedEntry>();
        StringBuilder? current = null;
        Severity currentLevel = Severity.Info;
        string? currentTimestamp = null;
        foreach (var raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var redacted = Redact(raw, repositoryRoot);
            var continuation = current is not null && IsContinuation(raw);
            if (!continuation) Flush();
            if (current is null)
            {
                current = new StringBuilder();
                currentLevel = DetectSeverity(raw);
                currentTimestamp = ExtractTimestamp(redacted, out redacted);
            }
            else currentLevel = (Severity)Math.Max((int)currentLevel, (int)DetectSeverity(raw));
            if (current.Length > 0) current.Append('\n');
            current.Append(redacted.TrimEnd());
        }
        Flush();
        return entries;

        void Flush()
        {
            if (current is null) return;
            entries.Add(new(currentLevel, currentTimestamp, current.ToString()));
            current = null;
            currentTimestamp = null;
        }
    }

    private static bool IsContinuation(string line) => char.IsWhiteSpace(line[0])
        || line.StartsWith("at ", StringComparison.Ordinal)
        || line.StartsWith("---", StringComparison.Ordinal)
        || line.StartsWith("(Filename:", StringComparison.Ordinal)
        || line.StartsWith("UnityEngine.", StringComparison.Ordinal);

    private static Severity DetectSeverity(string line)
    {
        if (line.Contains("exception", StringComparison.OrdinalIgnoreCase)) return Severity.Exception;
        if (line.Contains("error", StringComparison.OrdinalIgnoreCase)
            || line.Contains("failed", StringComparison.OrdinalIgnoreCase)) return Severity.Error;
        if (line.Contains("warning", StringComparison.OrdinalIgnoreCase)
            || line.Contains("warn:", StringComparison.OrdinalIgnoreCase)) return Severity.Warning;
        return Severity.Info;
    }

    private static string? ExtractTimestamp(string line, out string withoutTimestamp)
    {
        var match = TimestampPrefix.Match(line);
        withoutTimestamp = line;
        if (!match.Success || !DateTimeOffset.TryParse(match.Groups["value"].Value,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp)) return null;
        withoutTimestamp = line[match.Length..];
        return timestamp.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Redact(string value, string repositoryRoot)
    {
        var redacted = value.Replace(repositoryRoot, "<REPOSITORY>", RepositoryPathSecurity.Comparison);
        redacted = Bearer.Replace(redacted, "Bearer [REDACTED]");
        redacted = Secret.Replace(redacted, match => match.Groups[1].Value + "=[REDACTED]");
        redacted = WindowsPath.Replace(redacted, "<ABSOLUTE_PATH>");
        return UnixPath.Replace(redacted, "<ABSOLUTE_PATH>");
    }

    private static Severity ParseSeverity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Severity.Info;
        return value.Trim().ToLowerInvariant() switch
        {
            "info" => Severity.Info,
            "warning" => Severity.Warning,
            "error" => Severity.Error,
            "exception" => Severity.Exception,
            _ => throw new ArgumentException("minimumSeverity must be info, warning, error, or exception.",
                nameof(value))
        };
    }

    private static Source? SelectNewestSource(string repositoryRoot)
    {
        Source? newest = null;
        foreach (var candidate in Sources)
        {
            var path = Path.GetFullPath(Path.Combine(repositoryRoot,
                candidate.Path.Replace('/', Path.DirectorySeparatorChar)));
            if (!RepositoryPathSecurity.IsContained(repositoryRoot, path) || !File.Exists(path)
                || !RepositoryPathSecurity.IsOrdinaryPath(path)) continue;
            DateTime modified;
            try { modified = File.GetLastWriteTimeUtc(path); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
            if (newest is null || modified > newest.Value.Modified
                || modified == newest.Value.Modified && string.CompareOrdinal(candidate.Path, newest.Value.RelativePath) < 0)
                newest = new(path, candidate.Path, candidate.Type, modified);
        }
        return newest;
    }

    private enum Severity { Info, Warning, Error, Exception }
    private sealed record ParsedEntry(Severity Level, string? Timestamp, string Text);
    private readonly record struct Source(string FullPath, string RelativePath, string Type, DateTime Modified);
}
