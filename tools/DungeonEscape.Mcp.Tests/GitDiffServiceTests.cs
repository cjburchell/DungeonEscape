using System.Diagnostics;
using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class GitDiffServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeGitDiffTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CleanRepositoryReturnsCleanResult()
    {
        InitializeRepository();
        var result = await Service().GetAsync();
        Assert.Equal("available", result.Status);
        Assert.True(result.Clean);
        Assert.False(result.Truncated);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task UnstagedStagedAndCombinedStatesAreRepresented()
    {
        InitializeRepository();
        Write("DungeonEscape.Core/unstaged.cs", "initial\n");
        Write("DungeonEscape.Core/staged.cs", "initial\n");
        Write("DungeonEscape.Core/both.cs", "initial\n");
        Git("add", ".");
        Git("commit", "-m", "files");
        Write("DungeonEscape.Core/unstaged.cs", "unstaged\n");
        Write("DungeonEscape.Core/staged.cs", "staged\n");
        Write("DungeonEscape.Core/both.cs", "staged version\n");
        Git("add", "DungeonEscape.Core/staged.cs", "DungeonEscape.Core/both.cs");
        Write("DungeonEscape.Core/both.cs", "working version\n");

        var result = await Service().GetAsync();

        AssertEntry("DungeonEscape.Core/unstaged.cs", staged: false, unstaged: true);
        AssertEntry("DungeonEscape.Core/staged.cs", staged: true, unstaged: false);
        var both = AssertEntry("DungeonEscape.Core/both.cs", staged: true, unstaged: true);
        Assert.Contains("[staged]", both.Patch);
        Assert.Contains("[unstaged]", both.Patch);

        GitDiffEntry AssertEntry(string path, bool staged, bool unstaged)
        {
            var entry = Assert.Single(result.Entries, item => item.Path == path);
            Assert.Equal(staged, entry.Staged);
            Assert.Equal(unstaged, entry.Unstaged);
            Assert.False(entry.Untracked);
            Assert.False(Path.IsPathRooted(entry.Path));
            return entry;
        }
    }

    [Fact]
    public async Task DeletedAndRenamedFilesUseRepositoryRelativeMetadata()
    {
        InitializeRepository();
        Write("DungeonEscape.Core/delete.cs", "delete me\n");
        Write("DungeonEscape.Core/old name.cs", "rename me\n");
        Git("add", ".");
        Git("commit", "-m", "files");
        File.Delete(Path.Combine(root, "DungeonEscape.Core", "delete.cs"));
        Git("mv", "DungeonEscape.Core/old name.cs", "DungeonEscape.Core/new name.cs");

        var result = await Service().GetAsync();

        Assert.Equal("deleted", Assert.Single(result.Entries,
            entry => entry.Path == "DungeonEscape.Core/delete.cs").ChangeKind);
        var renamed = Assert.Single(result.Entries, entry => entry.ChangeKind == "renamed");
        Assert.Equal("DungeonEscape.Core/new name.cs", renamed.Path);
        Assert.Equal("DungeonEscape.Core/old name.cs", renamed.PreviousPath);
        Assert.False(Path.IsPathRooted(renamed.Path));
        Assert.False(Path.IsPathRooted(renamed.PreviousPath));
    }

    [Fact]
    public async Task BinaryChangesReturnMetadataWithoutPatchContent()
    {
        InitializeRepository();
        WriteBytes("DungeonEscape.Core/image.bin", [0, 1, 2, 3]);
        Git("add", ".");
        Git("commit", "-m", "binary");
        WriteBytes("DungeonEscape.Core/image.bin", [0, 9, 8, 7]);

        var entry = Assert.Single((await Service().GetAsync()).Entries);

        Assert.True(entry.Binary);
        Assert.Null(entry.Patch);
    }

    [Fact]
    public async Task UntrackedIsMetadataOnlyAndIgnoredFilesAreAbsent()
    {
        InitializeRepository();
        Write(".gitignore", "ignored.txt\n");
        Git("add", ".gitignore");
        Git("commit", "-m", "ignore");
        Write("DungeonEscape.Core/untracked secret.cs", "secret content\n");
        Write("ignored.txt", "ignored\n");

        var result = await Service().GetAsync();
        var entry = Assert.Single(result.Entries);

        Assert.Equal("DungeonEscape.Core/untracked secret.cs", entry.Path);
        Assert.True(entry.Untracked);
        Assert.Null(entry.Patch);
        Assert.DoesNotContain(result.Entries, item => item.Path.Contains("ignored", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CallerInclusionFlagsFilterWithoutAcceptingPathsOrRevisions()
    {
        InitializeRepository();
        Write("DungeonEscape.Core/tracked.cs", "initial\n");
        Git("add", ".");
        Git("commit", "-m", "tracked");
        Write("DungeonEscape.Core/tracked.cs", "changed\n");
        Write("DungeonEscape.Core/untracked.cs", "new\n");

        var stagedOnly = await Service().GetAsync(includeStaged: true, includeUnstaged: false,
            includeUntrackedMetadata: false);
        var unstagedOnly = await Service().GetAsync(includeStaged: false, includeUnstaged: true,
            includeUntrackedMetadata: false);
        var untrackedOnly = await Service().GetAsync(includeStaged: false, includeUnstaged: false,
            includeUntrackedMetadata: true);

        Assert.Empty(stagedOnly.Entries);
        Assert.Single(unstagedOnly.Entries, entry => entry.Path.EndsWith("tracked.cs", StringComparison.Ordinal));
        Assert.Single(untrackedOnly.Entries, entry => entry.Untracked);
    }

    [Fact]
    public async Task FileAndPatchBoundsSetAccurateTruncationFlags()
    {
        InitializeRepository();
        for (var index = 0; index < 3; index++)
            Write($"DungeonEscape.Core/file{index}.cs", new string('x', 2000));

        var fileBound = await Service().GetAsync(maxFiles: 2);
        Assert.Equal(2, fileBound.Entries.Count);
        Assert.True(fileBound.Truncated);

        Git("add", ".");
        Git("commit", "-m", "large files");
        for (var index = 0; index < 3; index++)
            Write($"DungeonEscape.Core/file{index}.cs", new string((char)('a' + index), 50_000));
        var patchBound = await Service().GetAsync(maxPatchCharacters: 40_000);
        Assert.True(patchBound.Truncated);
        Assert.Contains(patchBound.Entries, entry => entry.PatchTruncated);
        Assert.All(patchBound.Entries, entry =>
            Assert.True((entry.Patch?.Length ?? 0) <= GitDiffService.MaximumPatchCharactersPerFile));
        Assert.True(patchBound.Entries.Sum(entry => entry.Patch?.Length ?? 0) <= 40_000);
    }

    [Fact]
    public async Task InvalidLimitsAreRejected()
    {
        InitializeRepository();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Service().GetAsync(maxFiles: 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Service().GetAsync(
            maxPatchCharacters: GitDiffService.MaximumTotalPatchCharacters + 1));
        Assert.Equal(1024 * 1024, GitDiffService.MaximumProcessOutputBytes);
        Assert.Equal(16 * 1024, GitDiffService.MaximumStandardErrorBytes);
    }

    [Fact]
    public async Task GlobalProcessOutputBoundStopsOversizedDiffAndReportsTruncation()
    {
        InitializeRepository();
        Write("DungeonEscape.Core/huge.cs", new string('a', GitDiffService.MaximumProcessOutputBytes + 200_000));
        Git("add", ".");
        Git("commit", "-m", "huge file");
        Write("DungeonEscape.Core/huge.cs", new string('b', GitDiffService.MaximumProcessOutputBytes + 200_000));

        var result = await Service().GetAsync();

        var entry = Assert.Single(result.Entries);
        Assert.True(result.Truncated);
        Assert.True(entry.PatchTruncated);
        Assert.True((entry.Patch?.Length ?? 0) <= GitDiffService.MaximumPatchCharactersPerFile);
    }

    [Fact]
    public async Task MissingRepositoryAndProcessStartFailureFailSafely()
    {
        CreateIdentity();
        var noRepository = await Service().GetAsync();
        Assert.Equal("unavailable", noRepository.Status);
        Assert.Empty(noRepository.Entries);

        InitializeGit();
        var missingGit = await Service("definitely-not-a-real-git-executable").GetAsync();
        Assert.Equal("unavailable", missingGit.Status);
        Assert.Empty(missingGit.Entries);
    }

    [Fact]
    public async Task ProcessTimeoutFailsSafely()
    {
        InitializeRepository();
        var result = await Service("git", TimeSpan.Zero).GetAsync();
        Assert.Equal("unavailable", result.Status);
        Assert.Empty(result.Entries);
    }

    private GitDiffService Service(string executable = "git", TimeSpan? timeout = null) =>
        new(new ProjectInfoService(Path.GetTempPath(), root), executable, timeout);

    private void InitializeRepository()
    {
        CreateIdentity();
        InitializeGit();
    }

    private void CreateIdentity()
    {
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity"));
        File.WriteAllText(Path.Combine(root, "DungeonEscape.sln"), "");
    }

    private void InitializeGit()
    {
        if (Directory.Exists(Path.Combine(root, ".git"))) return;
        Git("init");
        Git("config", "user.name", "DungeonEscape Tests");
        Git("config", "user.email", "tests@example.invalid");
        Git("add", "DungeonEscape.sln");
        Git("commit", "-m", "initial");
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private void WriteBytes(string relativePath, byte[] content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

    private void Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = root
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"git {string.Join(' ', arguments)} failed: {stdout} {stderr}");
    }

    public void Dispose()
    {
        if (!Directory.Exists(root)) return;
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            File.SetAttributes(path, FileAttributes.Normal);
        Directory.Delete(root, true);
    }
}
