using System.Text;
using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class ReadScriptServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeReadTests",
        Guid.NewGuid().ToString("N"));
    private ReadScriptService Service => new(new ProjectInfoService(Path.GetTempPath(), root));

    public ReadScriptServiceTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity", "Assets", "DungeonEscape", "Scripts"));
        File.WriteAllText(Path.Combine(root, "DungeonEscape.sln"), "");
    }

    [Fact]
    public void ReadsCoreAndUnityFilesWithRepositoryRelativePaths()
    {
        Write("DungeonEscape.Core/CoreThing.cs", "core");
        Write("DungeonEscape.Unity/Assets/DungeonEscape/Scripts/UnityThing.cs", "unity");

        var core = Service.Read("DungeonEscape.Core/CoreThing.cs");
        var unity = Service.Read("DungeonEscape.Unity/Assets/DungeonEscape/Scripts/UnityThing.cs");

        Assert.Equal("DungeonEscape.Core/CoreThing.cs", core.Path);
        Assert.Equal("core", core.Content);
        Assert.Equal("DungeonEscape.Unity/Assets/DungeonEscape/Scripts/UnityThing.cs", unity.Path);
        Assert.Equal("unity", unity.Content);
        Assert.DoesNotContain(root, core.Path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultsToFirstHundredLines()
    {
        WriteLines("DungeonEscape.Core/Lines.cs", 150);

        var result = Service.Read("DungeonEscape.Core/Lines.cs");

        Assert.Equal(1, result.RequestedStartLine);
        Assert.Equal(ReadScriptService.DefaultLineCount, result.RequestedEndLine);
        Assert.Equal(1, result.EffectiveStartLine);
        Assert.Equal(100, result.EffectiveEndLine);
        Assert.Equal(100, result.LinesReturned);
        Assert.Equal(150, result.TotalLineCount);
        Assert.False(result.Truncated);
        Assert.StartsWith("line-001\n", result.Content);
        Assert.EndsWith("line-100", result.Content);
    }

    [Theory]
    [InlineData(3, null, 3, 6, "line-003\nline-004\nline-005\nline-006")]
    [InlineData(null, 2, 1, 2, "line-001\nline-002")]
    [InlineData(4, 3, 4, 6, "line-004\nline-005\nline-006")]
    public void HonorsExplicitStartCountAndMiddleRanges(int? startLine, int? lineCount,
        int expectedStart, int expectedEnd, string expectedContent)
    {
        WriteLines("DungeonEscape.Core/Lines.cs", 6);

        var result = Service.Read("DungeonEscape.Core/Lines.cs", startLine, lineCount);

        Assert.Equal(expectedStart, result.EffectiveStartLine);
        Assert.Equal(expectedEnd, result.EffectiveEndLine);
        Assert.Equal(expectedContent, result.Content);
    }

    [Fact]
    public void StartPastEndReturnsDeterministicEmptyRange()
    {
        WriteLines("DungeonEscape.Core/Lines.cs", 3);

        var result = Service.Read("DungeonEscape.Core/Lines.cs", 10, 5);

        Assert.Equal(10, result.EffectiveStartLine);
        Assert.Equal(3, result.EffectiveEndLine);
        Assert.Equal(0, result.LinesReturned);
        Assert.Equal(3, result.TotalLineCount);
        Assert.Empty(result.Content);
        Assert.False(result.Truncated);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, ReadScriptService.MaximumLineCount + 1)]
    [InlineData(int.MaxValue, 2)]
    public void RejectsInvalidOrExcessiveLineRanges(int startLine, int lineCount)
    {
        Write("DungeonEscape.Core/A.cs", "text");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Service.Read("DungeonEscape.Core/A.cs", startLine, lineCount));
    }

    [Fact]
    public void ReturnedTextIsByteBoundedAndUnicodeSafe()
    {
        var emoji = char.ConvertFromUtf32(0x1F9ED);
        Write("DungeonEscape.Core/LargeLine.cs",
            string.Concat(Enumerable.Repeat(emoji, ReadScriptService.MaximumReturnedBytes)));

        var result = Service.Read("DungeonEscape.Core/LargeLine.cs", 1, 1);

        Assert.True(result.Truncated);
        Assert.Equal(1, result.LinesReturned);
        Assert.True(Encoding.UTF8.GetByteCount(result.Content) <= ReadScriptService.MaximumReturnedBytes);
        AssertWellFormedUtf16(result.Content);
    }

    [Fact]
    public void OversizedFilesAreRejectedWithoutAbsolutePathDisclosure()
    {
        WriteBytes("DungeonEscape.Core/Large.cs", new byte[ReadScriptService.MaximumFileBytes + 1]);

        var error = Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/Large.cs"));

        Assert.DoesNotContain(root, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AbsoluteTraversalUnapprovedExcludedDirectoryAndMissingPathsAreRejected()
    {
        Write("DungeonEscape.Core/A.cs", "text");
        Write("README.md", "outside");
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core", "bin"));

        var errors = new[]
        {
            Assert.Throws<ArgumentException>(() => Service.Read(Path.Combine(root, "DungeonEscape.Core", "A.cs"))),
            Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/../README.md")),
            Assert.Throws<ArgumentException>(() => Service.Read("README.md")),
            Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/bin/A.cs")),
            Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/Missing.cs"))
        };
        Assert.All(errors, error => Assert.DoesNotContain(root, error.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DirectoriesUnsupportedExtensionsBinaryAndMalformedUtf8AreRejected()
    {
        Write("DungeonEscape.Core/Image.png", "not source");
        WriteBytes("DungeonEscape.Core/Binary.cs", [0x41, 0x00, 0x42]);
        WriteBytes("DungeonEscape.Core/Invalid.cs", [0xc3, 0x28]);

        Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core"));
        Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/Image.png"));
        Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/Binary.cs"));
        Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/Invalid.cs"));
    }

    [Fact]
    public void LinkedFileAndDirectoryEscapesAreRejected()
    {
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        var outsideFile = Path.Combine(outside, "Escaped.cs");
        File.WriteAllText(outsideFile, "secret");
        var directoryLink = Path.Combine(root, "DungeonEscape.Core", "Linked");
        CreateDirectoryLink(directoryLink, outside);
        var fileLink = Path.Combine(root, "DungeonEscape.Core", "LinkedFile.cs");
        File.CreateSymbolicLink(fileLink, outsideFile);
        try
        {
            Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/Linked/Escaped.cs"));
            Assert.Throws<ArgumentException>(() => Service.Read("DungeonEscape.Core/LinkedFile.cs"));
        }
        finally
        {
            File.Delete(fileLink);
            Directory.Delete(directoryLink);
        }
    }

    [Fact]
    public void OutputIsDeterministic()
    {
        WriteLines("DungeonEscape.Core/Lines.cs", 12);
        Assert.Equal(Service.Read("DungeonEscape.Core/Lines.cs", 2, 5),
            Service.Read("DungeonEscape.Core/Lines.cs", 2, 5));
    }

    private void WriteLines(string relativePath, int count) =>
        Write(relativePath, string.Join('\n', Enumerable.Range(1, count).Select(index => $"line-{index:D3}")));

    private void Write(string relativePath, string content) => WriteBytes(relativePath, Encoding.UTF8.GetBytes(content));

    private void WriteBytes(string relativePath, byte[] content)
    {
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, content);
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/c mklink /J \"{link}\" \"{target}\"",
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true
            })!;
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }
        else Directory.CreateSymbolicLink(link, target);
    }

    private static void AssertWellFormedUtf16(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            Assert.False(char.IsLowSurrogate(value[index]));
            if (!char.IsHighSurrogate(value[index])) continue;
            Assert.True(index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]));
            index++;
        }
    }

    public void Dispose() => Directory.Delete(root, true);
}
