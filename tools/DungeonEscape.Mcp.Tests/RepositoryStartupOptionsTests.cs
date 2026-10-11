using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class RepositoryStartupOptionsTests
{
    [Fact]
    public void OmittedOptionUsesDiscovery() => Assert.Null(RepositoryStartupOptions.ParseRepositoryRoot([]));

    [Fact]
    public void ParsesLauncherPathWithoutTreatingItAsHostConfiguration()
    {
        var root = Path.Combine(Path.GetTempPath(), "checkout with spaces");
        Assert.Equal(root, RepositoryStartupOptions.ParseRepositoryRoot(["--repository-root", root]));
    }

    [Theory]
    [InlineData("--repository-root")]
    [InlineData("--repository-root", "")]
    [InlineData("--repository-root", " ")]
    [InlineData("--unknown", "path")]
    [InlineData("--repository-root=path")]
    [InlineData("--repository-root", "path", "--repository-root", "other")]
    public void RejectsMissingAmbiguousOrUnknownOptions(params string[] args)
    {
        Assert.Throws<ArgumentException>(() => RepositoryStartupOptions.ParseRepositoryRoot(args));
    }
}
