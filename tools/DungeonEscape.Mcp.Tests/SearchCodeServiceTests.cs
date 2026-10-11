using System.Text;
using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class SearchCodeServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeSearchTests",
        Guid.NewGuid().ToString("N"));
    private SearchCodeService Service => new(new ProjectInfoService(Path.GetTempPath(), root));

    public SearchCodeServiceTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity", "Assets", "DungeonEscape", "Scripts"));
        File.WriteAllText(Path.Combine(root, "DungeonEscape.sln"), "");
    }

    [Fact]
    public void SearchesCoreAndUnityScriptsWithRelativePathsAndOneBasedLines()
    {
        Write("DungeonEscape.Core/Rules/CoreRule.cs", "first\nKnownSymbol core\n");
        Write("DungeonEscape.Unity/Assets/DungeonEscape/Scripts/UnityThing.cs", "KnownSymbol unity\n");

        var result = Service.Search("knownsymbol");

        Assert.Collection(result.Matches,
            match =>
            {
                Assert.Equal("DungeonEscape.Core/Rules/CoreRule.cs", match.Path);
                Assert.Equal(2, match.Line);
            },
            match =>
            {
                Assert.Equal("DungeonEscape.Unity/Assets/DungeonEscape/Scripts/UnityThing.cs", match.Path);
                Assert.Equal(1, match.Line);
            });
        Assert.Null(result.SearchRoot);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void OptionalSubtreeAndFileFilteringAreNormalized()
    {
        Write("DungeonEscape.Core/One/A.cs", "Needle\n");
        Write("DungeonEscape.Core/Two/B.cs", "Needle\n");

        var subtree = Service.Search("Needle", @"DungeonEscape.Core\One");
        var file = Service.Search("Needle", "DungeonEscape.Core/Two/B.cs");

        Assert.Equal("DungeonEscape.Core/One", subtree.SearchRoot);
        Assert.Equal("DungeonEscape.Core/One/A.cs", Assert.Single(subtree.Matches).Path);
        Assert.Equal("DungeonEscape.Core/Two/B.cs", file.SearchRoot);
        Assert.Equal("DungeonEscape.Core/Two/B.cs", Assert.Single(file.Matches).Path);
    }

    [Fact]
    public void NoMatchesReturnsEmptyResult()
    {
        Write("DungeonEscape.Core/A.cs", "haystack");
        var result = Service.Search("needle");
        Assert.Empty(result.Matches);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void ResultLimitAndSnippetLengthAreBounded()
    {
        var longLine = new string('a', 300) + "Needle" + new string('b', 300);
        Write("DungeonEscape.Core/A.cs", $"{longLine}\nNeedle two\nNeedle three\n");

        var result = Service.Search("Needle", maxResults: 2);

        Assert.Equal(2, result.Matches.Count);
        Assert.True(result.Truncated);
        Assert.Equal(SearchCodeService.MaximumSnippetLength, result.Matches[0].Snippet.Length);
        Assert.StartsWith("…", result.Matches[0].Snippet);
        Assert.EndsWith("…", result.Matches[0].Snippet);
        Assert.Contains("Needle", result.Matches[0].Snippet);
    }

    [Fact]
    public void LargeFileIsSkippedAndMarksResultTruncated()
    {
        WriteBytes("DungeonEscape.Core/Large.cs",
            Encoding.UTF8.GetBytes("Needle" + new string('x', (int)SearchCodeService.MaximumFileBytes)));
        var result = Service.Search("Needle");
        Assert.Empty(result.Matches);
        Assert.True(result.Truncated);
    }

    [Fact]
    public void AggregateByteLimitStopsSearchAndMarksResultTruncated()
    {
        var bytes = Encoding.UTF8.GetBytes(new string('x', (int)SearchCodeService.MaximumFileBytes));
        for (var index = 0; index < 26; index++)
            WriteBytes($"DungeonEscape.Core/{index:D2}.cs", bytes);
        Write("DungeonEscape.Core/zz.cs", "Needle");

        var result = Service.Search("Needle");

        Assert.Empty(result.Matches);
        Assert.True(result.Truncated);
    }

    [Fact]
    public void FileCountLimitStopsSearchAndMarksResultTruncated()
    {
        for (var index = 0; index <= SearchCodeService.MaximumFiles; index++)
            Write($"DungeonEscape.Core/{index:D4}.cs", "");
        Write("DungeonEscape.Core/zzzz.cs", "Needle");

        var result = Service.Search("Needle");

        Assert.Empty(result.Matches);
        Assert.True(result.Truncated);
    }

    [Fact]
    public void UnsupportedBinaryAndInvalidUtf8FilesAreIgnored()
    {
        Write("DungeonEscape.Core/Unsupported.png", "Needle");
        WriteBytes("DungeonEscape.Core/Binary.cs", [0x41, 0x00, 0x4e]);
        WriteBytes("DungeonEscape.Core/Invalid.cs", [0xc3, 0x28]);
        Write("DungeonEscape.Core/Valid.cs", "Needle");

        var result = Service.Search("Needle");

        Assert.Equal("DungeonEscape.Core/Valid.cs", Assert.Single(result.Matches).Path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("line\nbreak")]
    public void InvalidQueriesAreRejected(string query) =>
        Assert.Throws<ArgumentException>(() => Service.Search(query));

    [Fact]
    public void LongQueryAndInvalidResultLimitsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Service.Search(new string('x', 129)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Service.Search("x", maxResults: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Service.Search("x", maxResults: 51));
    }

    [Fact]
    public void AbsoluteTraversalSiblingPrefixExcludedAndOutsidePathsAreRejected()
    {
        Write("DungeonEscape.Core/A.cs", "Needle");
        Write("DungeonEscape.Core.Evil/A.cs", "Needle");
        Write("README.md", "Needle");
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core", "bin"));

        Assert.Throws<ArgumentException>(() => Service.Search("x", Path.Combine(root, "DungeonEscape.Core")));
        Assert.Throws<ArgumentException>(() => Service.Search("x", "DungeonEscape.Core/../README.md"));
        Assert.Throws<ArgumentException>(() => Service.Search("x", "DungeonEscape.Core.Evil/A.cs"));
        Assert.Throws<ArgumentException>(() => Service.Search("x", "DungeonEscape.Core/bin"));
        Assert.Throws<ArgumentException>(() => Service.Search("x", "README.md"));
    }

    [Fact]
    public void LinkedSearchRootIsRejectedAndLinkedDescendantIsSkipped()
    {
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "Escaped.cs"), "Needle");
        var link = Path.Combine(root, "DungeonEscape.Core", "Linked");
        CreateDirectoryLink(link, outside);
        try
        {
            Assert.Throws<ArgumentException>(() => Service.Search("Needle", "DungeonEscape.Core/Linked"));
            Assert.Empty(Service.Search("Needle", "DungeonEscape.Core").Matches);
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public void RegexAndShellCharactersAreTreatedLiterally()
    {
        const string literal = "[abc] $(danger).*";
        Write("DungeonEscape.Core/A.cs", literal);
        Assert.Single(Service.Search(literal).Matches);
        Assert.Single(Service.Search(".*").Matches);
        Assert.Empty(Service.Search("[a-z]+").Matches);
    }

    [Fact]
    public void ResultsAreDeterministicallySortedByRelativePath()
    {
        Write("DungeonEscape.Core/Z.cs", "Needle");
        Write("DungeonEscape.Core/A.cs", "Needle");
        Write("DungeonEscape.Core/M.cs", "Needle");

        Assert.Equal(["DungeonEscape.Core/A.cs", "DungeonEscape.Core/M.cs", "DungeonEscape.Core/Z.cs"],
            Service.Search("Needle").Matches.Select(match => match.Path));
    }

    private void Write(string relativePath, string content) =>
        WriteBytes(relativePath, Encoding.UTF8.GetBytes(content));

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

    public void Dispose() => Directory.Delete(root, true);
}
