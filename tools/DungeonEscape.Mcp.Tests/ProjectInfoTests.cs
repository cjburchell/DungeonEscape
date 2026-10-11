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
        // Junctions work without Windows symbolic-link privileges.
        if (OperatingSystem.IsWindows())
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/c mklink /J \"{link}\" \"{outside}\"",
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true
            })!;
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }
        else Directory.CreateSymbolicLink(link, outside);
        try
        {
            Assert.Equal(ProjectInfoService.Unavailable, new ProjectInfoService(Server).GetProjectInfo().UnityVersion);
            Assert.Null(new ProjectInfoService(link).DiscoverRepositoryRoot());
        }
        finally { Directory.Delete(link); }
    }

    private void Write(string path, string content)
    {
        var fullPath = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose() => Directory.Delete(root, true);
}
