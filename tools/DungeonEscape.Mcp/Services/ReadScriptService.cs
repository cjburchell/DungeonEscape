using System.Text;

namespace DungeonEscape.Mcp.Services;

public sealed record ReadScriptResult(string Path, int RequestedStartLine, int RequestedEndLine,
    int EffectiveStartLine, int EffectiveEndLine, int LinesReturned, int? TotalLineCount,
    bool Truncated, string Content);

/// <summary>Bounded UTF-8 reads from one file governed by the shared repository content policy.</summary>
public sealed class ReadScriptService(ProjectInfoService projectInfo)
{
    public const int DefaultLineCount = 100;
    public const int MaximumLineCount = 200;
    public const long MaximumFileBytes = SearchCodeService.MaximumFileBytes;
    public const int MaximumReturnedBytes = 64 * 1024;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public ReadScriptResult Read(string path, int? startLine = null, int? lineCount = null)
    {
        var requestedStart = startLine ?? 1;
        var requestedCount = lineCount ?? DefaultLineCount;
        if (requestedStart < 1)
            throw new ArgumentOutOfRangeException(nameof(startLine), "startLine must be at least 1.");
        if (requestedCount is < 1 or > MaximumLineCount)
            throw new ArgumentOutOfRangeException(nameof(lineCount),
                $"lineCount must be between 1 and {MaximumLineCount}.");
        int requestedEnd;
        try { requestedEnd = checked(requestedStart + requestedCount - 1); }
        catch (OverflowException) { throw new ArgumentOutOfRangeException(nameof(startLine), "requested line range is too large."); }

        var repositoryRoot = projectInfo.DiscoverRepositoryRoot()
            ?? throw new InvalidOperationException("DungeonEscape repository root is not available.");
        var normalizedPath = RepositoryContentPolicy.NormalizeRequestedPath(path, required: true)!;
        var file = RepositoryContentPolicy.ResolveApprovedFile(repositoryRoot, normalizedPath);
        var text = ReadStrictUtf8(file, normalizedPath);

        var selected = new List<string>();
        var totalLines = 0;
        using (var reader = new StringReader(text))
        {
            while (reader.ReadLine() is { } line)
            {
                totalLines++;
                if (totalLines >= requestedStart && totalLines <= requestedEnd) selected.Add(line);
            }
        }

        var content = new StringBuilder();
        var linesReturned = 0;
        var returnedBytes = 0;
        var truncated = false;
        foreach (var line in selected)
        {
            var separator = linesReturned == 0 ? "" : "\n";
            var separatorBytes = separator.Length;
            var remaining = MaximumReturnedBytes - returnedBytes;
            var required = separatorBytes + StrictUtf8.GetByteCount(line);
            if (required <= remaining)
            {
                content.Append(separator).Append(line);
                returnedBytes += required;
                linesReturned++;
                continue;
            }

            var partial = TruncateUtf8(line, remaining - separatorBytes);
            if (partial.Length > 0 || line.Length == 0 && remaining >= separatorBytes)
            {
                content.Append(separator).Append(partial);
                linesReturned++;
            }
            truncated = true;
            break;
        }

        var effectiveEnd = linesReturned == 0 ? Math.Min(totalLines, requestedStart - 1)
            : requestedStart + linesReturned - 1;
        return new(normalizedPath, requestedStart, requestedEnd, requestedStart, effectiveEnd,
            linesReturned, totalLines, truncated, content.ToString());
    }

    private static string ReadStrictUtf8(string file, string normalizedPath)
    {
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.SequentialScan);
            if (stream.Length > MaximumFileBytes)
                throw new ArgumentException($"path exceeds the {MaximumFileBytes}-byte file limit.", nameof(normalizedPath));
            var bytes = new byte[stream.Length + 1];
            var count = 0;
            while (count < bytes.Length)
            {
                var read = stream.Read(bytes, count, bytes.Length - count);
                if (read == 0) break;
                count += read;
            }
            if (count > MaximumFileBytes)
                throw new ArgumentException($"path exceeds the {MaximumFileBytes}-byte file limit.", nameof(normalizedPath));
            if (Array.IndexOf(bytes, (byte)0, 0, count) >= 0)
                throw new ArgumentException("path contains binary content.", nameof(normalizedPath));
            try { return StrictUtf8.GetString(bytes, 0, count); }
            catch (DecoderFallbackException)
            {
                throw new ArgumentException("path is not valid UTF-8 text.", nameof(normalizedPath));
            }
        }
        catch (ArgumentException) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("The requested repository file could not be read.");
        }
    }

    private static string TruncateUtf8(string value, int maximumBytes)
    {
        if (maximumBytes <= 0) return "";
        var end = Math.Min(value.Length, maximumBytes);
        while (end > 0)
        {
            if (end < value.Length && char.IsHighSurrogate(value[end - 1]) && char.IsLowSurrogate(value[end])) end--;
            if (StrictUtf8.GetByteCount(value.AsSpan(0, end)) <= maximumBytes) return value[..end];
            end--;
        }
        return "";
    }
}
