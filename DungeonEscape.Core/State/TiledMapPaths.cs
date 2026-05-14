using System;
using System.IO;

namespace Redpoint.DungeonEscape.State
{
    public static class TiledMapPaths
    {
        private const string MapAssetPrefix = "Assets/DungeonEscape/Maps/";
        private const string MapsPrefix = "maps/";
        private const string ImagesPrefix = "images/";
        private const string ImagesSegment = "/Images/";

        public static string NormalizeMapAssetPath(string mapIdOrAssetPath)
        {
            if (string.IsNullOrEmpty(mapIdOrAssetPath))
            {
                return MapAssetPrefix + "overworld.tmx";
            }

            var normalized = mapIdOrAssetPath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase)
                    ? normalized
                    : normalized + ".tmx";
            }

            if (normalized.StartsWith(MapsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(MapsPrefix.Length);
            }

            if (!normalized.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase))
            {
                normalized += ".tmx";
            }

            return MapAssetPrefix + normalized;
        }

        public static string NormalizeMapId(string mapIdOrAssetPath)
        {
            if (string.IsNullOrEmpty(mapIdOrAssetPath))
            {
                return "overworld";
            }

            var normalized = mapIdOrAssetPath.Replace('\\', '/');
            if (normalized.StartsWith(MapAssetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(MapAssetPrefix.Length);
            }

            if (normalized.StartsWith(MapsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(MapsPrefix.Length);
            }

            if (normalized.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - ".tmx".Length);
            }

            return normalized;
        }

        public static string ResolveTilesetAssetPath(string source)
        {
            return "Assets/DungeonEscape/Tilesets/" + Path.GetFileName(source);
        }

        public static string ResolveTilesetImageAssetPath(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            var normalized = source.Replace('\\', '/');
            if (normalized.StartsWith(ImagesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(ImagesPrefix.Length);
            }
            else
            {
                var imagesIndex = normalized.IndexOf(ImagesSegment, StringComparison.OrdinalIgnoreCase);
                if (imagesIndex >= 0)
                {
                    normalized = normalized.Substring(imagesIndex + ImagesSegment.Length);
                }
            }

            return "Assets/DungeonEscape/Images/" + normalized;
        }
    }
}
