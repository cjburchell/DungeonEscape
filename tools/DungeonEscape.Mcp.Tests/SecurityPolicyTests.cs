using DungeonEscape.Mcp.Security;
using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class SecurityPolicyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeSecurityPolicy",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void CapabilityCatalogContainsExactlySevenUniqueCompleteDefinitions()
    {
        Assert.Equal(7, McpCapabilityCatalog.All.Count);
        Assert.Equal(7, McpCapabilityCatalog.All.Select(definition => definition.Name)
            .Distinct(StringComparer.Ordinal).Count());
        Assert.All(McpCapabilityCatalog.All, definition =>
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Name));
            Assert.False(string.IsNullOrWhiteSpace(definition.Description));
        });
        Assert.Equal(6, McpCapabilityCatalog.All.Count(definition => definition.ReadOnly));
        var tests = McpCapabilityCatalog.GetRequired(McpCapabilityNames.RunTests);
        Assert.False(tests.ReadOnly);
        Assert.False(tests.Destructive);
        Assert.False(tests.Idempotent);
        Assert.False(tests.OpenWorld);
        Assert.Equal(McpSideEffectClassification.LocalTestArtifacts, tests.SideEffects);
    }

    [Fact]
    public void UnknownMissingDuplicateAndIncompleteCapabilitiesFailClosed()
    {
        Assert.False(McpCapabilityCatalog.TryGet("unknown_tool", out _));
        Assert.Throws<KeyNotFoundException>(() => McpCapabilityCatalog.GetRequired("unknown_tool"));

        var valid = new McpCapabilityDefinition("one", true, false, true, false,
            McpSideEffectClassification.None, "description");
        Assert.Throws<InvalidOperationException>(() =>
            McpCapabilityCatalog.CreateValidatedIndex([valid, valid with { Description = "other" }]));
        Assert.Throws<InvalidOperationException>(() =>
            McpCapabilityCatalog.CreateValidatedIndex([valid with { Name = "" }]));
        Assert.Throws<InvalidOperationException>(() =>
            McpCapabilityCatalog.CreateValidatedIndex([valid with { Description = "" }]));
    }

    [Fact]
    public void RepositoryRelativeResolutionRejectsTraversalAbsoluteAndSiblingPrefixConfusion()
    {
        Directory.CreateDirectory(root);
        var sibling = root + "-sibling";
        Directory.CreateDirectory(sibling);
        try
        {
            Assert.False(RepositoryPathSecurity.TryResolveContainedRelativePath(root, "../outside", out _));
            Assert.False(RepositoryPathSecurity.TryResolveContainedRelativePath(root,
                Path.Combine(root, "absolute.cs"), out _));
            Assert.False(RepositoryPathSecurity.IsContained(root, sibling));
            Assert.True(RepositoryPathSecurity.IsContained(root, root));
        }
        finally { Directory.Delete(sibling, true); }
    }

    [Fact]
    public void RepositoryRelativeResolutionCanonicalizesAndFormatsStableSlashPaths()
    {
        Directory.CreateDirectory(Path.Combine(root, "folder", "child"));
        Assert.True(RepositoryPathSecurity.TryResolveContainedRelativePath(root,
            "folder/./child/file.cs", out var resolved));
        Assert.True(RepositoryPathSecurity.IsContained(root, resolved));
        Assert.Equal("folder/child/file.cs", RepositoryPathSecurity.Relative(root, resolved));
        Assert.Equal(OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal,
            RepositoryPathSecurity.Comparison);
    }

    [Theory]
    [InlineData("DungeonEscape.Core/bin/generated.cs")]
    [InlineData("DungeonEscape.Core/obj/generated.cs")]
    [InlineData("DungeonEscape.Core/TestResults/generated.cs")]
    [InlineData(".git/config")]
    public void SharedContentPolicyRejectsExcludedGeneratedDirectories(string path)
    {
        Assert.Throws<ArgumentException>(() => RepositoryContentPolicy.NormalizeRequestedPath(path, true));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
