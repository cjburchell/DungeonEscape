using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// Copies selected music files into the app's served <c>wwwroot</c> folder and
/// returns short relative URLs that the WebView audio element can play.
/// </summary>
internal static class MusicPreviewCache
{
    private const string CacheDirectoryName = "music-cache";

    public static string? GetPreviewUrl(string? musicDirectory, string? song)
    {
        if (string.IsNullOrWhiteSpace(musicDirectory) || string.IsNullOrWhiteSpace(song))
        {
            return null;
        }

        var sourcePath = Path.Combine(musicDirectory, song + ".ogg");
        if (!File.Exists(sourcePath))
        {
            return null;
        }

        try
        {
            var info = new FileInfo(sourcePath);
            var identity = $"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
            var cachePath = Path.Combine(GetCacheRoot(), Hash(identity), Path.GetFileName(sourcePath));

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            File.Copy(sourcePath, cachePath, overwrite: true);

            return ToRelativeUrl(cachePath);
        }
        catch
        {
            return null;
        }
    }

    private static string GetCacheRoot()
    {
        return Path.Combine(AppContext.BaseDirectory, "wwwroot", CacheDirectoryName);
    }

    private static string ToRelativeUrl(string cachePath)
    {
        var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var relative = Path.GetRelativePath(webRoot, cachePath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

        return "/" + string.Join('/', relative.Split('/').Select(Uri.EscapeDataString));
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}