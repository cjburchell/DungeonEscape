using System.Text;
using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class UnityConsoleServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeConsoleTests",
        Guid.NewGuid().ToString("N"));
    private UnityConsoleService Service => new(new ProjectInfoService(Path.GetTempPath(), root));

    public UnityConsoleServiceTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity"));
        File.WriteAllText(Path.Combine(root, "DungeonEscape.sln"), "");
    }

    [Fact]
    public void MissingLogsReturnExplicitUnavailableResult()
    {
        var result = Service.Get();
        Assert.Equal(UnityConsoleService.Unavailable, result.Status);
        Assert.Null(result.SourceType);
        Assert.Empty(result.Entries);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void ParsesSeveritiesTimestampAndMultilineStackTrace()
    {
        Log("""
            2026-10-11T14:30:00Z ordinary message
            Warning: careful
            Error: failed operation
            InvalidOperationException: broken
              at Game.Run ()
            (Filename: Assets/Test.cs Line: 10)
            """);

        var result = Service.Get();

        Assert.Equal(UnityConsoleService.Available, result.Status);
        Assert.Equal("project-playmode-test", result.SourceType);
        Assert.Equal(["info", "warning", "error", "exception"], result.Entries.Select(x => x.Severity));
        Assert.Equal("2026-10-11T14:30:00.0000000+00:00", result.Entries[0].Timestamp);
        Assert.Null(result.Entries[1].Timestamp);
        Assert.Contains("at Game.Run", result.Entries[3].Text);
        Assert.Contains("Filename:", result.Entries[3].Text);
    }

    [Fact]
    public void MaximumEntriesAndMinimumSeverityAreEnforcedFromNewestEntries()
    {
        Log("Info one\nWarning: two\nError: three\nException: four\n");
        var result = Service.Get(2, "warning");
        Assert.Equal(["error", "exception"], result.Entries.Select(x => x.Severity));
        Assert.True(result.Truncated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void InvalidMaximumEntriesIsRejected(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Service.Get(value));

    [Fact]
    public void InvalidSeverityIsRejected() =>
        Assert.Throws<ArgumentException>(() => Service.Get(minimumSeverity: "critical"));

    [Fact]
    public void TailReadingDropsOldPartialContentAndReturnsRecentContent()
    {
        var prefix = "old secret line\n" + new string('x', UnityConsoleService.MaximumTailBytes) + "\n";
        Log(prefix + "Error: recent diagnostic\n");
        var result = Service.Get();
        Assert.True(result.Truncated);
        Assert.DoesNotContain(result.Entries, entry => entry.Text.Contains("old secret", StringComparison.Ordinal));
        Assert.Contains(result.Entries, entry => entry.Text.Contains("recent diagnostic", StringComparison.Ordinal));
    }

    [Fact]
    public void TotalAndPerEntryTextAreBounded()
    {
        var lines = Enumerable.Range(0, 100)
            .Select(index => $"Error {index:D3}: " + new string('x', 3000));
        Log(string.Join('\n', lines));
        var result = Service.Get(100);
        Assert.True(result.Truncated);
        Assert.All(result.Entries, entry => Assert.True(entry.Text.Length <= UnityConsoleService.MaximumEntryCharacters));
        Assert.True(result.Entries.Sum(entry => entry.Text.Length) <= UnityConsoleService.MaximumReturnedCharacters);
    }

    [Fact]
    public void OversizedLogIsRejectedBeforeContentIsRead()
    {
        var path = LogPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            stream.SetLength(UnityConsoleService.MaximumSourceFileBytes + 1);
        var result = Service.Get();
        Assert.Equal(UnityConsoleService.Oversized, result.Status);
        Assert.True(result.Truncated);
        Assert.Empty(result.Entries);
    }

    [Theory]
    [InlineData(new byte[] { 0x41, 0x00, 0x42 })]
    [InlineData(new byte[] { 0xc3, 0x28 })]
    public void BinaryAndInvalidUtf8AreMalformed(byte[] bytes)
    {
        var path = LogPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        var result = Service.Get();
        Assert.Equal(UnityConsoleService.Malformed, result.Status);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public void RedactsSecretsAndAbsolutePathsWhilePreservingDiagnosticContext()
    {
        var repoFile = Path.Combine(root, "DungeonEscape.Unity", "Assets", "Game.cs");
        Log($"""
            Error connecting: Bearer abc.def-123 diagnostic continues
            api_key=super-secret request failed
            access_token: token-value retry possible
            password = "do-not-return" useful suffix
            Repository file {repoFile}:42
            User file C:\Users\Someone\private.txt
            Linux file /home/someone/private.txt
            """);

        var result = Service.Get();
        var output = string.Join('\n', result.Entries.Select(entry => entry.Text));
        Assert.DoesNotContain("abc.def-123", output);
        Assert.DoesNotContain("super-secret", output);
        Assert.DoesNotContain("token-value", output);
        Assert.DoesNotContain("do-not-return", output);
        Assert.DoesNotContain("C:\\Users", output);
        Assert.DoesNotContain("/home/someone", output);
        Assert.DoesNotContain(root, output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[REDACTED]", output);
        Assert.Contains("diagnostic continues", output);
        Assert.Contains("request failed", output);
        Assert.Contains("useful suffix", output);
        Assert.Contains("<REPOSITORY>", output);
    }

    [Fact]
    public void NewestFixedSourceWinsDeterministicallyAndNoOtherFileIsRead()
    {
        Log("Error: playmode\n");
        var ignored = Write("TestResults/arbitrary.log", "password=must-not-appear");
        File.SetLastWriteTimeUtc(ignored, DateTime.UtcNow.AddHours(1));
        var validation = Write("DungeonEscape.Unity/Logs/local-unity-validate.log", "Warning: validation\n");
        File.SetLastWriteTimeUtc(validation, DateTime.UtcNow.AddMinutes(1));

        var first = Service.Get();
        var second = Service.Get();

        Assert.Equal("project-local-validation", first.SourceType);
        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.SourceType, second.SourceType);
        Assert.Equal(first.Truncated, second.Truncated);
        Assert.Equal(first.Warnings, second.Warnings);
        Assert.Equal(first.Entries, second.Entries);
        Assert.Contains("validation", Assert.Single(first.Entries).Text);
    }

    [Fact]
    public void MalformedLinesDegradeToBoundedInfoEntries()
    {
        Log("???\n[not-a-timestamp] still useful\n");
        var result = Service.Get();
        Assert.Equal(2, result.Entries.Count);
        Assert.All(result.Entries, entry => Assert.Equal("info", entry.Severity));
        Assert.All(result.Entries, entry => Assert.Null(entry.Timestamp));
    }

    private void Log(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath())!);
        File.WriteAllText(LogPath(), content, new UTF8Encoding(false));
    }
    private string LogPath() => Path.Combine(root, "TestResults", "playmode-log.txt");
    private string Write(string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }

    public void Dispose()
    {
        try { if (Directory.Exists(root)) Directory.Delete(root, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
