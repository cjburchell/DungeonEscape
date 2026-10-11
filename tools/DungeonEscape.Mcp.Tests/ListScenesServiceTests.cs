using System.Text;
using DungeonEscape.Mcp.Services;
using Xunit;

namespace DungeonEscape.Mcp.Tests;

public sealed class ListScenesServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DungeonEscapeSceneTests",
        Guid.NewGuid().ToString("N"));
    private ListScenesService Service => new(new ProjectInfoService(Path.GetTempPath(), root));

    public ListScenesServiceTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Core"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity", "Assets"));
        Directory.CreateDirectory(Path.Combine(root, "DungeonEscape.Unity", "ProjectSettings"));
        File.WriteAllText(Path.Combine(root, "DungeonEscape.sln"), "");
    }

    [Fact]
    public void DiscoversScenesRecursivelyInDeterministicRelativeOrder()
    {
        Scene("Zed.unity");
        Scene("Folder/Alpha.unity");
        Write("DungeonEscape.Unity/Assets/Ignore.prefab", "not a scene");
        Settings("  m_Scenes:\n");

        var first = Service.List();
        var second = Service.List();

        var expected = new[]
        {
            "DungeonEscape.Unity/Assets/Folder/Alpha.unity",
            "DungeonEscape.Unity/Assets/Zed.unity"
        };
        Assert.Equal(expected, first.Scenes.Select(scene => scene.Path));
        Assert.Equal(expected, second.Scenes.Select(scene => scene.Path));
        Assert.All(first.Scenes, scene => { Assert.False(scene.InBuildSettings); Assert.Null(scene.Enabled); });
        Assert.False(first.Truncated);
    }

    [Fact]
    public void ReportsEnabledDisabledAbsentAndDuplicateBuildEntries()
    {
        Scene("Enabled.unity");
        Scene("Disabled.unity");
        Scene("Absent.unity");
        Settings("""
                   m_Scenes:
                   - enabled: 0
                     path: Assets/Enabled.unity
                   - enabled: 1
                     path: Assets/Enabled.unity
                   - enabled: 0
                     path: Assets/Disabled.unity
                   - enabled: 1
                     path: Assets/Missing.unity
                 """);

        var result = Service.List();

        Assert.Equal(ListScenesService.Available, result.BuildSettingsStatus);
        Assert.Equal(true, result.Scenes.Single(scene => scene.Path.EndsWith("Enabled.unity")).Enabled);
        Assert.Equal(false, result.Scenes.Single(scene => scene.Path.EndsWith("Disabled.unity")).Enabled);
        var absent = result.Scenes.Single(scene => scene.Path.EndsWith("Absent.unity"));
        Assert.False(absent.InBuildSettings);
        Assert.Null(absent.Enabled);
        Assert.DoesNotContain(result.Scenes, scene => scene.Path.EndsWith("Missing.unity"));
    }

    [Fact]
    public void ParsesUnityPathsWithWhitespaceAndWindowsLineEndings()
    {
        Scene("Scenes/Boot.unity");
        Settings("  m_Scenes:  \r\n  - enabled : 1\r\n    path : Assets/Scenes/Boot.unity  \r\n");
        var scene = Assert.Single(Service.List().Scenes);
        Assert.True(scene.InBuildSettings);
        Assert.True(scene.Enabled);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("invalid-utf8")]
    [InlineData("binary")]
    public void UnavailableBuildSettingsProduceExplicitUnknownMetadata(string kind)
    {
        Scene("Only.unity");
        var path = BuildSettingsPath();
        if (kind == "malformed") File.WriteAllText(path, "not build settings");
        if (kind == "invalid-utf8") File.WriteAllBytes(path, [0xc3, 0x28]);
        if (kind == "binary") File.WriteAllBytes(path, [0x41, 0x00, 0x42]);

        var result = Service.List();

        Assert.Equal(kind == "missing" ? ListScenesService.Missing : ListScenesService.Malformed,
            result.BuildSettingsStatus);
        var scene = Assert.Single(result.Scenes);
        Assert.Null(scene.InBuildSettings);
        Assert.Null(scene.Enabled);
    }

    [Fact]
    public void OversizedBuildSettingsProduceExplicitUnknownMetadata()
    {
        Scene("Only.unity");
        File.WriteAllText(BuildSettingsPath(), new string('x', (int)ListScenesService.MaximumBuildSettingsBytes + 1));
        var result = Service.List();
        Assert.Equal(ListScenesService.Oversized, result.BuildSettingsStatus);
        Assert.Null(Assert.Single(result.Scenes).InBuildSettings);
    }

    [Fact]
    public void MalformedOrUnsafeBuildEntryFailsClosed()
    {
        Scene("Only.unity");
        Settings("  m_Scenes:\n  - enabled: 1\n    path: Assets/../Outside.unity\n");
        var result = Service.List();
        Assert.Equal(ListScenesService.Malformed, result.BuildSettingsStatus);
        Assert.Null(Assert.Single(result.Scenes).InBuildSettings);
    }

    [Fact]
    public void SceneLimitBoundsDiscoveryAndMarksTruncated()
    {
        Settings("  m_Scenes:\n");
        for (var index = ListScenesService.MaximumScenesReturned; index >= 0; index--)
            Scene($"Many/{index:D4}.unity");

        var result = Service.List();

        Assert.Equal(ListScenesService.MaximumScenesReturned, result.Scenes.Count);
        Assert.True(result.Truncated);
        Assert.Equal("DungeonEscape.Unity/Assets/Many/0000.unity", result.Scenes[0].Path);
        Assert.Equal($"DungeonEscape.Unity/Assets/Many/{ListScenesService.MaximumScenesReturned - 1:D4}.unity",
            result.Scenes[^1].Path);
    }

    [Fact]
    public void LinkedSceneAndDirectoryAreSkipped()
    {
        if (!CanCreateSymbolicLinks()) return;
        Scene("Ordinary.unity");
        var outside = Path.Combine(Path.GetTempPath(), "DungeonEscapeSceneOutside", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "Escaped.unity"), "");
        var linkedDirectory = Path.Combine(root, "DungeonEscape.Unity", "Assets", "Linked");
        var linkedFile = Path.Combine(root, "DungeonEscape.Unity", "Assets", "LinkedFile.unity");
        try
        {
            Directory.CreateSymbolicLink(linkedDirectory, outside);
            File.CreateSymbolicLink(linkedFile, Path.Combine(outside, "Escaped.unity"));
            Settings("  m_Scenes:\n");
            Assert.Equal("DungeonEscape.Unity/Assets/Ordinary.unity", Assert.Single(Service.List().Scenes).Path);
        }
        finally { Directory.Delete(outside, true); }
    }

    private void Scene(string assetsPath) => Write("DungeonEscape.Unity/Assets/" + assetsPath, "scene content ignored");
    private void Settings(string content) => File.WriteAllText(BuildSettingsPath(), content, new UTF8Encoding(false));
    private string BuildSettingsPath() => Path.Combine(root, "DungeonEscape.Unity", "ProjectSettings",
        "EditorBuildSettings.asset");
    private void Write(string relative, string content)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
    private bool CanCreateSymbolicLinks()
    {
        var target = Path.Combine(root, "link-target.tmp");
        var link = Path.Combine(root, "link-test.tmp");
        try
        {
            File.WriteAllText(target, "");
            File.CreateSymbolicLink(link, target);
            File.Delete(link);
            File.Delete(target);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            return false;
        }
    }
    public void Dispose()
    {
        try { if (Directory.Exists(root)) Directory.Delete(root, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
