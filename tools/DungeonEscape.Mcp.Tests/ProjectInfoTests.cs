using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class ProjectInfoTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeMcpTests", Guid.NewGuid().ToString("N"));
    private string Server => Path.Combine(root, "tools", "DungeonEscape.Mcp");

    public ProjectInfoTests()
    {
        Directory.CreateDirectory(Server);
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity"));
        Write("DungeonEscape.sln", "");
    }

    [Fact]
    public void ConfiguredRootIsCanonicalAndIndependentOfExecutableLocation()
    {
        Write("DungeonEscape.Unity/ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 6000.4.4f1\n");
        var configured = Path.Combine(root, "DungeonEscape.Core", "..") + Path.DirectorySeparatorChar;
        var service = new ProjectInfoService(Path.GetTempPath(), configured);
        Assert.Equal(root, service.DiscoverRepositoryRoot());
        Assert.Equal(root, service.GetProjectInfo().RepositoryRoot);
        Assert.Equal("6000.4.4f1", service.GetProjectInfo().UnityVersion);
    }

    [Theory]
    [InlineData("DungeonEscape.sln")]
    [InlineData("DungeonEscape.Core")]
    [InlineData("DungeonEscape.Unity")]
    public void ConfiguredRootRequiresEveryIdentityMarker(string marker)
    {
        var path = Path.Combine(root, marker);
        if (File.Exists(path)) File.Delete(path);
        else Directory.Delete(path);
        Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, root));
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("../DungeonEscape")]
    public void ConfiguredRootRejectsNonAbsolutePaths(string configured)
    {
        Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, configured));
    }

    [Fact]
    public void ConfiguredRootDoesNotScanParentsOrFallBackAfterMisconfiguration()
    {
        Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, Path.Combine(root, "DungeonEscape.Core")));
        Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, Path.Combine(root, "..")));
        Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, Path.Combine(root, "missing")));
    }

    [Fact]
    public void ConfiguredRootDoesNotFallBackIfItsIdentityDisappears()
    {
        var selected = Path.Combine(root, "selected");
        Write("selected/DungeonEscape.sln", "");
        Directory.CreateDirectory(Path.Combine(selected, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(selected, "DungeonEscape.Unity"));
        var service = new ProjectInfoService(Server, selected);
        Assert.Equal(selected, service.DiscoverRepositoryRoot());
        File.Delete(Path.Combine(selected, "DungeonEscape.sln"));
        Assert.Null(service.DiscoverRepositoryRoot());
        Assert.Equal(ProjectInfoService.Unavailable, service.GetProjectInfo().RepositoryRoot);
    }

    [Fact]
    public void ConfiguredRootRejectsLinkedRootAndAncestor()
    {
        Write("nested/DungeonEscape.sln", "");
        Directory.CreateDirectory(Path.Combine(root, "nested", "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "nested", "DungeonEscape.Unity"));
        var link = Path.Combine(root, "linked");
        CreateDirectoryLink(link, root);
        try
        {
            Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, link));
            Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, Path.Combine(link, "nested")));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public void WindowsDriveRelativeNetworkAndDevicePathsAreRejected()
    {
        if (!OperatingSystem.IsWindows()) return;
        foreach (var configured in new[] { @"C:checkout", @"\checkout", @"\\server\share\checkout", @"\\?\C:\checkout" })
            Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, configured));
    }

    [Fact]
    public void ConfiguredRootRejectsLinkedIdentityMarker()
    {
        var marker = Path.Combine(root, "DungeonEscape.Core");
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        Directory.Delete(marker);
        CreateDirectoryLink(marker, outside);
        try { Assert.Throws<ArgumentException>(() => new ProjectInfoService(Server, root)); }
        finally { Directory.Delete(marker); }
    }

    [Fact]
    public void DiscoversOnlyServerCheckoutFromNestedBuildDirectory()
    {
        var output = Path.Combine(Server, "bin", "Debug", "net10.0");
        Directory.CreateDirectory(output);
        Assert.Equal(root, new ProjectInfoService(output).DiscoverRepositoryRoot());
        Assert.Null(new ProjectInfoService(root).DiscoverRepositoryRoot());
        var unrelated = Path.Combine(root, "unrelated");
        Directory.CreateDirectory(unrelated);
        Assert.Null(new ProjectInfoService(unrelated).DiscoverRepositoryRoot());
    }

    [Fact]
    public void InvalidNestedCandidateDoesNotFallBackToParentCheckout()
    {
        var nested = Path.Combine(Server, "tools", "DungeonEscape.Mcp");
        Directory.CreateDirectory(nested);
        Assert.Null(new ProjectInfoService(nested).DiscoverRepositoryRoot());
    }

    [Fact]
    public void NonexistentApplicationDirectoryIsUnavailable()
    {
        Assert.Null(new ProjectInfoService(Path.Combine(Server, "missing")).DiscoverRepositoryRoot());
    }

    [Fact]
    public void MissingIdentityMarkerRejectsRoot()
    {
        File.Delete(Path.Combine(root, "DungeonEscape.sln"));
        Assert.Null(new ProjectInfoService(Server).DiscoverRepositoryRoot());
        Assert.Equal(ProjectInfoService.Unavailable, new ProjectInfoService(Server).GetProjectInfo().RepositoryRoot);
    }

    [Fact]
    public void ReadsVersionAndSymbolicBranches()
    {
        Write("DungeonEscape.Unity/ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 6000.4.4f1\nm_EditorVersionWithRevision: ignored\n");
        Write(".git/HEAD", "ref: refs/heads/feature/ai-002\n");
        Write(".git/refs/remotes/origin/HEAD", "ref: refs/remotes/origin/main\n");
        var info = new ProjectInfoService(Server).GetProjectInfo();
        Assert.Equal("DungeonEscape", info.ProjectName);
        Assert.Equal("6000.4.4f1", info.UnityVersion);
        Assert.Equal("feature/ai-002", info.CurrentBranch);
        Assert.Equal("main", info.DefaultBranch);
    }

    [Fact]
    public void MissingMetadataIsUnavailableWithoutGuessingDefaultBranch()
    {
        var info = new ProjectInfoService(Server).GetProjectInfo();
        Assert.Equal(root, info.RepositoryRoot);
        Assert.Equal(ProjectInfoService.Unavailable, info.UnityVersion);
        Assert.Equal(ProjectInfoService.Unavailable, info.CurrentBranch);
        Assert.Equal(ProjectInfoService.Unavailable, info.DefaultBranch);
    }

    [Theory]
    [InlineData("0123456789012345678901234567890123456789")]
    [InlineData("ref: refs/heads/../../outside")]
    [InlineData("ref: refs/heads/main\nextra")]
    public void UnsupportedHeadIsUnavailable(string head)
    {
        Write(".git/HEAD", head);
        Assert.Equal(ProjectInfoService.Unavailable, new ProjectInfoService(Server).GetProjectInfo().CurrentBranch);
    }

    [Fact]
    public void GitFileLayoutIsUnavailable()
    {
        Write(".git", "gitdir: ../outside");
        Assert.Equal(ProjectInfoService.Unavailable, new ProjectInfoService(Server).GetProjectInfo().CurrentBranch);
    }

    [Fact]
    public void SharedGitLayoutIsUnavailable()
    {
        Write(".git/HEAD", "ref: refs/heads/main");
        Write(".git/commondir", "../outside");
        Assert.Equal(ProjectInfoService.Unavailable, new ProjectInfoService(Server).GetProjectInfo().CurrentBranch);
    }

    [Theory]
    [InlineData("m_EditorVersion: invalid")]
    [InlineData("m_EditorVersion: 6000.4.4f1\n", true)]
    public void InvalidOrOversizedMetadataIsUnavailable(string text, bool oversized = false)
    {
        Write("DungeonEscape.Unity/ProjectSettings/ProjectVersion.txt", text + (oversized ? new string('x', 4097) : ""));
        Assert.Equal(ProjectInfoService.Unavailable, new ProjectInfoService(Server).GetProjectInfo().UnityVersion);
    }

    [Fact]
    public void LinkedMetadataCannotEscapeCheckout()
    {
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "ProjectVersion.txt"), "m_EditorVersion: 6000.4.4f1");
        var link = Path.Combine(root, "DungeonEscape.Unity", "ProjectSettings");
        CreateDirectoryLink(link, outside);
        try
        {
            Assert.Equal(ProjectInfoService.Unavailable, new ProjectInfoService(Server).GetProjectInfo().UnityVersion);
            Assert.Equal(ProjectInfoService.Unavailable, new ProjectInfoService(Server, root).GetProjectInfo().UnityVersion);
            Assert.Null(new ProjectInfoService(link).DiscoverRepositoryRoot());
        }
        finally { Directory.Delete(link); }
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        // Junctions work without Windows symbolic-link privileges.
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

    private void Write(string path, string content)
    {
        var fullPath = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose() => Directory.Delete(root, true);
}
