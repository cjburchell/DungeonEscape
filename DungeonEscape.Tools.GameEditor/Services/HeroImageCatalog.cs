using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace DungeonEscape.Tools.GameEditor.Services;

public sealed record HeroImageEntry(int ImageId, string DisplayName);

public sealed class HeroImageCatalog
{
    private const int HeroWidth = 32;
    private const int HeroHeight = 48;
    private const int CharacterFrameCount = 8;
    private const int DownIdleFrameOffset = 4;

    private readonly AssetContext context;
    private readonly Dictionary<int, string> imageUriCache = new();
    private List<HeroImageEntry> entries = new();
    private Bitmap? sourceBitmap;

    public HeroImageCatalog(AssetContext context)
    {
        this.context = context;
        this.context.Changed += Reload;
        Reload();
    }

    public IReadOnlyList<HeroImageEntry> Entries => entries;

    public bool TryGet(int imageId, out HeroImageEntry entry)
    {
        foreach (var candidate in entries)
        {
            if (candidate.ImageId == imageId)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null!;
        return false;
    }

    public string? GetImageDataUri(int imageId)
    {
        if (imageId < 0 || sourceBitmap == null)
        {
            return null;
        }

        if (imageUriCache.TryGetValue(imageId, out var cached))
        {
            return cached;
        }

        var frameIndex = imageId * CharacterFrameCount + DownIdleFrameOffset;
        var columns = sourceBitmap.Width / HeroWidth;
        if (columns <= 0)
        {
            return null;
        }

        var frameX = frameIndex % columns;
        var frameY = frameIndex / columns;
        var sourceX = frameX * HeroWidth;
        var sourceY = frameY * HeroHeight;
        if (sourceX + HeroWidth > sourceBitmap.Width || sourceY + HeroHeight > sourceBitmap.Height)
        {
            return null;
        }

        try
        {
            using var tile = new Bitmap(HeroWidth, HeroHeight, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(tile))
            {
                g.DrawImage(
                    sourceBitmap,
                    new Rectangle(0, 0, HeroWidth, HeroHeight),
                    new Rectangle(sourceX, sourceY, HeroWidth, HeroHeight),
                    GraphicsUnit.Pixel);
            }

            var cachePath = GetCachePath(imageId);
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            tile.Save(cachePath, ImageFormat.Png);
            var uri = ImagePreviewCache.ToRelativeUrl(cachePath);
            imageUriCache[imageId] = uri;
            return uri;
        }
        catch
        {
            return null;
        }
    }

    public void Reload()
    {
        imageUriCache.Clear();
        entries = new List<HeroImageEntry>();
        sourceBitmap?.Dispose();
        sourceBitmap = null;

        var path = context.HeroSpriteSheetPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes);
            sourceBitmap = new Bitmap(stream);
        }
        catch
        {
            sourceBitmap = null;
            return;
        }

        var frameCount = (sourceBitmap.Width / HeroWidth) * (sourceBitmap.Height / HeroHeight);
        var characterCount = Math.Max(1, frameCount / CharacterFrameCount);
        for (var index = 0; index < characterCount; index++)
        {
            entries.Add(new HeroImageEntry(index, $"#{index}"));
        }
    }

    private string GetCachePath(int imageId)
    {
        var identity = ImagePreviewCache.GetSourceIdentity(context.HeroSpriteSheetPath ?? "hero");
        return ImagePreviewCache.GetCachePath("hero", identity, imageId);
    }
}
