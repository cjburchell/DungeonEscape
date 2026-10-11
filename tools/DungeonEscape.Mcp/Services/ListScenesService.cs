using System.Text;
using System.Text.RegularExpressions;

namespace DungeonEscape.Mcp.Services;

public sealed record ListScenesResult(string BuildSettingsStatus, bool Truncated,
    IReadOnlyList<UnitySceneInfo> Scenes);

public sealed record UnitySceneInfo(string Path, bool? InBuildSettings, bool? Enabled);

/// <summary>Bounded discovery of ordinary Unity scene files and their build-settings status.</summary>
public sealed class ListScenesService(ProjectInfoService projectInfo)
{
    public const int MaximumSceneFilesExamined = 500;
    public const int MaximumScenesReturned = 500;
    public const long MaximumBuildSettingsBytes = 256 * 1024;

    public const string Available = "available";
    public const string Missing = "missing";
    public const string Malformed = "malformed";
    public const string Oversized = "oversized";

    private const string AssetsRelativePath = "DungeonEscape.Unity/Assets";
    private const string BuildSettingsRelativePath =
        "DungeonEscape.Unity/ProjectSettings/EditorBuildSettings.asset";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly Regex EnabledLine = new(@"^\s*-\s*enabled\s*:\s*([01])\s*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex PathLine = new(@"^\s*path\s*:\s*(.*?)\s*$",
        RegexOptions.CultureInvariant);

    public ListScenesResult List()
    {
        var repositoryRoot = projectInfo.DiscoverRepositoryRoot()
            ?? throw new InvalidOperationException("DungeonEscape repository root is not available.");
        var assetsRoot = Path.GetFullPath(Path.Combine(repositoryRoot, AssetsRelativePath));
        if (!RepositoryPathSecurity.IsContained(repositoryRoot, assetsRoot)
            || !Directory.Exists(assetsRoot) || !RepositoryPathSecurity.IsOrdinaryPath(assetsRoot))
            throw new InvalidOperationException("Unity Assets root is not available as an ordinary directory.");

        var discovered = Discover(repositoryRoot, assetsRoot);
        var settings = ReadBuildSettings(repositoryRoot);
        var scenes = discovered.Paths.Select(path =>
        {
            if (settings.Entries is null) return new UnitySceneInfo(path, null, null);
            return settings.Entries.TryGetValue(path, out var enabled)
                ? new UnitySceneInfo(path, true, enabled)
                : new UnitySceneInfo(path, false, null);
        }).ToArray();
        return new(settings.Status, discovered.Truncated, scenes);
    }

    private static DiscoveryResult Discover(string repositoryRoot, string assetsRoot)
    {
        var paths = new List<string>();
        var examined = 0;
        var truncated = false;
        Visit(assetsRoot);
        paths.Sort(StringComparer.Ordinal);
        return new(paths, truncated);

        void Visit(string directory)
        {
            if (truncated || !RepositoryPathSecurity.IsContained(assetsRoot, directory)
                || !RepositoryPathSecurity.IsOrdinaryPath(directory)) return;
            string[] entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(directory).Select(Path.GetFullPath)
                    .Where(entry => RepositoryPathSecurity.IsContained(assetsRoot, entry))
                    .OrderBy(entry => Relative(repositoryRoot, entry), StringComparer.Ordinal).ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return; }

            foreach (var entry in entries)
            {
                if (truncated) return;
                if (!RepositoryPathSecurity.IsOrdinaryPath(entry)) continue;
                if (Directory.Exists(entry))
                {
                    Visit(entry);
                    continue;
                }
                if (!File.Exists(entry) || !Path.GetExtension(entry).Equals(".unity", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (examined == MaximumSceneFilesExamined || paths.Count == MaximumScenesReturned)
                {
                    truncated = true;
                    return;
                }
                examined++;
                paths.Add(Relative(repositoryRoot, entry));
            }
        }
    }

    private static BuildSettingsResult ReadBuildSettings(string repositoryRoot)
    {
        var path = Path.GetFullPath(Path.Combine(repositoryRoot, BuildSettingsRelativePath));
        if (!RepositoryPathSecurity.IsContained(repositoryRoot, path) || !File.Exists(path)
            || !RepositoryPathSecurity.IsOrdinaryPath(path)) return new(Missing, null);
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.SequentialScan);
            if (stream.Length > MaximumBuildSettingsBytes) return new(Oversized, null);
            var bytes = new byte[stream.Length + 1];
            var count = 0;
            while (count < bytes.Length)
            {
                var read = stream.Read(bytes, count, bytes.Length - count);
                if (read == 0) break;
                count += read;
            }
            if (count > MaximumBuildSettingsBytes || Array.IndexOf(bytes, (byte)0, 0, count) >= 0)
                return new(count > MaximumBuildSettingsBytes ? Oversized : Malformed, null);
            string text;
            try { text = StrictUtf8.GetString(bytes, 0, count); }
            catch (DecoderFallbackException) { return new(Malformed, null); }
            return ParseBuildSettings(text, repositoryRoot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(Missing, null);
        }
    }

    private static BuildSettingsResult ParseBuildSettings(string text, string repositoryRoot)
    {
        if (!Regex.IsMatch(text, @"(?m)^\s*m_Scenes\s*:\s*$", RegexOptions.CultureInvariant))
            return new(Malformed, null);
        var entries = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        bool? enabled = null;
        string? path = null;
        var invalid = false;
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var enabledMatch = EnabledLine.Match(line);
            if (enabledMatch.Success)
            {
                Flush();
                enabled = enabledMatch.Groups[1].Value == "1";
                continue;
            }
            var pathMatch = PathLine.Match(line);
            if (pathMatch.Success && enabled is not null) path = pathMatch.Groups[1].Value;
        }
        Flush();
        return invalid ? new(Malformed, null) : new(Available, entries);

        void Flush()
        {
            if (enabled is null) return;
            if (!TryNormalizeUnityScenePath(path, repositoryRoot, out var normalized)) invalid = true;
            else entries[normalized] = entries.TryGetValue(normalized, out var existing)
                ? existing || enabled.Value
                : enabled.Value;
            enabled = null;
            path = null;
        }
    }

    private static bool TryNormalizeUnityScenePath(string? value, string repositoryRoot, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(value)) return false;
        var unityPath = value.Trim().Replace('\\', '/');
        if (!unityPath.StartsWith("Assets/", StringComparison.Ordinal) || Path.IsPathRooted(unityPath)
            || unityPath.Split('/').Any(segment => segment is ".." or ".")
            || !Path.GetExtension(unityPath).Equals(".unity", StringComparison.OrdinalIgnoreCase)) return false;
        var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, "DungeonEscape.Unity",
            unityPath.Replace('/', Path.DirectorySeparatorChar)));
        var assetsRoot = Path.GetFullPath(Path.Combine(repositoryRoot, AssetsRelativePath));
        if (!RepositoryPathSecurity.IsContained(assetsRoot, fullPath)) return false;
        normalized = Relative(repositoryRoot, fullPath);
        return true;
    }

    private static string Relative(string repositoryRoot, string path) =>
        Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

    private sealed record DiscoveryResult(IReadOnlyList<string> Paths, bool Truncated);
    private sealed record BuildSettingsResult(string Status, Dictionary<string, bool>? Entries);
}
