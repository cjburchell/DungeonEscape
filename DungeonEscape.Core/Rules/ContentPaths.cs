using System;
using System.Collections.Generic;
using System.Linq;

namespace Redpoint.DungeonEscape.Rules
{
    public static class ContentPaths
    {
        public const string AssetPrefix = "Assets/DungeonEscape/";

        public static bool IsAssetPath(string path, string extension = null)
        {
            return path != null && path.StartsWith(AssetPrefix, StringComparison.Ordinal) &&
                   !path.Contains("\\") && !path.Contains(":") &&
                   !path.Split('/').Any(part => part == ".." || part == "." || part.Length == 0) &&
                   (extension == null || path.EndsWith(extension, StringComparison.Ordinal));
        }

        public static bool IsCampaignPath(string path, string scope, string extension = null) =>
            IsAssetPath(path, extension) && path.StartsWith(AssetPrefix + "Campaigns/" + scope + "/", StringComparison.Ordinal);

        public static bool IsJsonPath(string path) => IsAssetPath(path, ".json") &&
            (path.StartsWith(AssetPrefix + "Data/", StringComparison.Ordinal) || path.StartsWith(AssetPrefix + "Campaigns/", StringComparison.Ordinal));

        public static string RelativeToRoot(string path)
        {
            if (!IsAssetPath(path)) throw new ArgumentException("Invalid content asset path: " + path);
            return path.Substring(AssetPrefix.Length);
        }

        public static string ResolveRelative(string ownerAssetPath, string reference)
        {
            if (!IsAssetPath(ownerAssetPath) || string.IsNullOrEmpty(reference)) throw new ArgumentException("Invalid asset reference.");
            reference = reference.Replace('\\', '/');
            if (reference.StartsWith("Assets/", StringComparison.Ordinal))
            {
                if (!IsAssetPath(reference)) throw new ArgumentException("Asset reference is outside the content root.");
                return reference;
            }
            if (reference.StartsWith("/", StringComparison.Ordinal) || reference.Contains(":")) throw new ArgumentException("Asset reference must be relative.");
            var parts = new List<string>(ownerAssetPath.Split('/').Reverse().Skip(1).Reverse());
            foreach (var part in reference.Split('/'))
            {
                if (part == "." || part.Length == 0) continue;
                if (part == "..")
                {
                    if (parts.Count <= 2) throw new ArgumentException("Asset reference is outside the content root.");
                    parts.RemoveAt(parts.Count - 1);
                }
                else parts.Add(part);
            }
            var resolved = string.Join("/", parts);
            if (!IsAssetPath(resolved)) throw new ArgumentException("Asset reference is outside the content root.");
            return resolved;
        }
    }
}
