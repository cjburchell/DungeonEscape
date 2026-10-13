using System.Diagnostics;
using System.Text;

namespace DungeonEscape.Mcp.Services;

public sealed record GitDiffResult(string Status, bool Clean, bool Truncated, bool StagedIncluded,
    bool UnstagedIncluded, bool UntrackedIncluded, IReadOnlyList<GitDiffEntry> Entries,
    IReadOnlyList<string> Warnings);

public sealed record GitDiffEntry(string Path, string? PreviousPath, string ChangeKind, bool Staged,
    bool Unstaged, bool Untracked, bool Binary, string? Patch, bool PatchTruncated);

/// <summary>Bounded local-only Git status/diff inspection using fixed read-only commands.</summary>
public sealed class GitDiffService
{
    public const int DefaultMaximumFiles = 200;
    public const int MaximumFiles = 200;
    public const int MaximumPatchCharactersPerFile = 32 * 1024;
    public const int DefaultMaximumTotalPatchCharacters = 256 * 1024;
    public const int MaximumTotalPatchCharacters = 256 * 1024;
    public const int MaximumProcessOutputBytes = 1024 * 1024;
    public const int MaximumStandardErrorBytes = 16 * 1024;
    public static readonly TimeSpan DefaultProcessTimeout = TimeSpan.FromSeconds(15);

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly ProjectInfoService projectInfo;
    private readonly string gitExecutable;
    private readonly TimeSpan processTimeout;

    public GitDiffService(ProjectInfoService projectInfo, string gitExecutable = "git",
        TimeSpan? processTimeout = null)
    {
        this.projectInfo = projectInfo;
        this.gitExecutable = gitExecutable;
        this.processTimeout = processTimeout ?? DefaultProcessTimeout;
    }

    public async Task<GitDiffResult> GetAsync(bool includeStaged = true, bool includeUnstaged = true,
        bool includeUntrackedMetadata = true, int? maxFiles = null, int? maxPatchCharacters = null,
        CancellationToken cancellationToken = default)
    {
        var fileLimit = maxFiles ?? DefaultMaximumFiles;
        var patchLimit = maxPatchCharacters ?? DefaultMaximumTotalPatchCharacters;
        if (fileLimit is < 1 or > MaximumFiles)
            throw new ArgumentOutOfRangeException(nameof(maxFiles), $"maxFiles must be between 1 and {MaximumFiles}.");
        if (patchLimit is < 1 or > MaximumTotalPatchCharacters)
            throw new ArgumentOutOfRangeException(nameof(maxPatchCharacters),
                $"maxPatchCharacters must be between 1 and {MaximumTotalPatchCharacters}.");

        var root = projectInfo.DiscoverRepositoryRoot();
        if (root is null || !Directory.Exists(Path.Combine(root, ".git")))
            return Unavailable(includeStaged, includeUnstaged, includeUntrackedMetadata,
                "The configured checkout is not an available Git working tree.");

        var status = await RunGitAsync(root,
            ["status", "--porcelain=v1", "-z", "--untracked-files=all", "--ignored=no",
                "--ignore-submodules=all"],
            cancellationToken);
        if (!status.Started || status.TimedOut || status.ExitCode != 0 && !status.OutputTruncated
            || status.InvalidUtf8)
            return Unavailable(includeStaged, includeUnstaged, includeUntrackedMetadata,
                status.TimedOut ? "Git status timed out." : "Git status was unavailable or malformed.");

        var warnings = new List<string>();
        var parsed = ParseStatus(status.Text, root, warnings, out var malformed);
        var candidates = parsed.Where(entry =>
            (includeUntrackedMetadata && entry.Untracked)
            || (includeStaged && entry.Staged)
            || (includeUnstaged && entry.Unstaged)).ToArray();
        var truncated = status.OutputTruncated || malformed || candidates.Length > fileLimit;
        if (status.OutputTruncated) warnings.Add("Git status output exceeded the process-output limit.");
        if (malformed) warnings.Add("Malformed or unsafe status entries were skipped.");
        if (candidates.Length > fileLimit) warnings.Add("The changed-file limit was reached.");

        var results = new List<GitDiffEntry>();
        var totalPatchCharacters = 0;
        foreach (var candidate in candidates.Take(fileLimit))
        {
            var patchParts = new List<string>();
            var binary = false;
            var patchTruncated = false;
            if (!candidate.Untracked && includeStaged && candidate.Staged)
                await AddPatchAsync(true, candidate, patchParts);
            if (!candidate.Untracked && includeUnstaged && candidate.Unstaged)
                await AddPatchAsync(false, candidate, patchParts);

            var patch = patchParts.Count == 0 ? null : string.Join("\n", patchParts);
            if (patch is not null)
            {
                var perFileAvailable = Math.Min(MaximumPatchCharactersPerFile,
                    Math.Max(0, patchLimit - totalPatchCharacters));
                if (patch.Length > perFileAvailable)
                {
                    patch = patch[..perFileAvailable];
                    patchTruncated = true;
                    truncated = true;
                }
                totalPatchCharacters += patch.Length;
            }
            results.Add(new(candidate.Path, candidate.PreviousPath, candidate.ChangeKind,
                includeStaged && candidate.Staged, includeUnstaged && candidate.Unstaged,
                candidate.Untracked, binary, patch, patchTruncated));

            async Task AddPatchAsync(bool staged, StatusEntry entry, List<string> parts)
            {
                var arguments = new List<string> { "diff" };
                if (staged) arguments.Add("--cached");
                arguments.AddRange(["--no-ext-diff", "--no-textconv", "--no-color",
                    "--ignore-submodules=all", "--", entry.Path]);
                var diff = await RunGitAsync(root, arguments, cancellationToken);
                if (!diff.Started || diff.TimedOut || diff.ExitCode != 0 && !diff.OutputTruncated
                    || diff.InvalidUtf8)
                {
                    patchTruncated = true;
                    truncated = true;
                    warnings.Add($"The {(staged ? "staged" : "unstaged")} patch for {entry.Path} was unavailable.");
                    return;
                }
                patchTruncated |= diff.OutputTruncated;
                truncated |= diff.OutputTruncated;
                var text = diff.Text;
                if (text.Contains("Binary files ", StringComparison.Ordinal)
                    && text.Contains(" differ", StringComparison.Ordinal))
                {
                    binary = true;
                    return;
                }
                if (text.Length > 0) parts.Add(staged ? "[staged]\n" + text : "[unstaged]\n" + text);
            }
        }

        return new("available", parsed.Count == 0, truncated, includeStaged, includeUnstaged,
            includeUntrackedMetadata, results, warnings);
    }

    private async Task<ProcessResult> RunGitAsync(string root, IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(gitExecutable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            WorkingDirectory = root
        };
        foreach (var argument in new[]
        {
            "-c", "core.fsmonitor=false", "-c", "core.untrackedCache=false",
            "-c", "submodule.recurse=false"
        }) start.ArgumentList.Add(argument);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["GIT_PAGER"] = "cat";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_OPTIONAL_LOCKS"] = "0";

        Process? process;
        try { process = Process.Start(start); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return ProcessResult.NotStarted;
        }
        if (process is null) return ProcessResult.NotStarted;
        using (process)
        {
            process.StandardInput.Close();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(processTimeout);
            var stdout = ReadBoundedAsync(process.StandardOutput.BaseStream, MaximumProcessOutputBytes,
                timeout.Token, stopAtLimit: true, () =>
                {
                    try { if (!process.HasExited) process.Kill(true); }
                    catch (InvalidOperationException) { }
                });
            var stderr = ReadBoundedAsync(process.StandardError.BaseStream, MaximumStandardErrorBytes,
                timeout.Token, stopAtLimit: false);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                var output = await stdout;
                _ = await stderr; // Always drain bounded stderr; never return its potentially sensitive text.
                try
                {
                    return new(true, process.ExitCode, false, output.Truncated, false,
                        StrictUtf8.GetString(output.Bytes));
                }
                catch (DecoderFallbackException)
                {
                    return new(true, process.ExitCode, false, output.Truncated, true, "");
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (!process.HasExited) process.Kill(true);
                await process.WaitForExitAsync(CancellationToken.None);
                return new(true, -1, true, true, false, "");
            }
            finally
            {
                if (!process.HasExited) process.Kill(true);
            }
        }
    }

    private static async Task<BoundedBytes> ReadBoundedAsync(Stream stream, int maximum,
        CancellationToken cancellationToken, bool stopAtLimit, Action? limitReached = null)
    {
        using var memory = new MemoryStream(Math.Min(maximum, 64 * 1024));
        var buffer = new byte[8192];
        var truncated = false;
        while (true)
        {
            var count = await stream.ReadAsync(buffer, cancellationToken);
            if (count == 0) break;
            var accepted = Math.Min(count, maximum - (int)memory.Length);
            if (accepted > 0) memory.Write(buffer, 0, accepted);
            if (accepted < count || memory.Length == maximum)
            {
                truncated = true;
                if (stopAtLimit)
                {
                    limitReached?.Invoke();
                    break;
                }
            }
        }
        return new(memory.ToArray(), truncated);
    }

    private static IReadOnlyList<StatusEntry> ParseStatus(string text, string root, List<string> warnings,
        out bool malformed)
    {
        malformed = false;
        var entries = new List<StatusEntry>();
        var records = text.Split('\0');
        for (var index = 0; index < records.Length - 1; index++)
        {
            var record = records[index];
            if (record.Length < 4 || record[2] != ' ')
            {
                malformed = true;
                continue;
            }
            var x = record[0];
            var y = record[1];
            var path = record[3..];
            string? previousPath = null;
            if (x is 'R' or 'C' || y is 'R' or 'C')
            {
                if (++index >= records.Length - 1) { malformed = true; break; }
                previousPath = records[index];
            }
            if (!IsSafeRelativePath(root, path) || previousPath is not null && !IsSafeRelativePath(root, previousPath))
            {
                malformed = true;
                continue;
            }
            var untracked = x == '?' && y == '?';
            entries.Add(new(path.Replace('\\', '/'), previousPath?.Replace('\\', '/'),
                Kind(x, y), !untracked && x != ' ', !untracked && y != ' ', untracked));
        }
        if (records.Length > 0 && records[^1].Length != 0) malformed = true;
        return entries;
    }

    private static bool IsSafeRelativePath(string root, string path)
    {
        return RepositoryPathSecurity.TryResolveContainedRelativePath(root, path, out _);
    }

    private static string Kind(char x, char y)
    {
        if (x == '?' && y == '?') return "untracked";
        var code = x != ' ' ? x : y;
        return code switch
        {
            'A' => "added", 'D' => "deleted", 'R' => "renamed", 'C' => "copied",
            'T' => "type-changed", 'U' => "unmerged", _ => "modified"
        };
    }

    private static GitDiffResult Unavailable(bool staged, bool unstaged, bool untracked, string warning) =>
        new("unavailable", false, false, staged, unstaged, untracked, [], [warning]);

    private sealed record StatusEntry(string Path, string? PreviousPath, string ChangeKind,
        bool Staged, bool Unstaged, bool Untracked);
    private sealed record BoundedBytes(byte[] Bytes, bool Truncated);
    private sealed record ProcessResult(bool Started, int ExitCode, bool TimedOut, bool OutputTruncated,
        bool InvalidUtf8, string Text)
    {
        public static ProcessResult NotStarted { get; } = new(false, -1, false, false, false, "");
    }
}
