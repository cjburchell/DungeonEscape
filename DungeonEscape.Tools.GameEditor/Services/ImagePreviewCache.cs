using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// Stores image previews under the app's served <c>wwwroot</c> folder and returns
/// short relative URLs that Photino's local web host can render reliably.
/// </summary>
internal static class ImagePreviewCache
{
    private const string CacheDirectoryName = "image-cache";

    public static string GetCachePath(string catalog, string sourceIdentity, int imageId)
    {
        var key = Hash(sourceIdentity);
        return Path.Combine(GetCacheRoot(), catalog, key, imageId + ".png");
    }

    public static string ToRelativeUrl(string cachePath)
    {
        var relative = Path.GetRelativePath(GetWebRoot(), cachePath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

        return "/" + relative;
    }

    public static string GetSourceIdentity(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists
                ? $"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}"
                : path;
        }
        catch
        {
            return path;
        }
    }

    private static string GetCacheRoot()
    {
        return Path.Combine(GetWebRoot(), CacheDirectoryName);
    }

    private static string GetWebRoot()
    {
        return Path.Combine(AppContext.BaseDirectory, "wwwroot");
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}