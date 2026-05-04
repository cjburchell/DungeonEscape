using System;
using System.IO;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Core
{
    public static class UnityAssetPath
    {
        private const string DungeonEscapeAssetPrefix = "Assets/DungeonEscape/";
        private const string StreamingDungeonEscapeFolder = "DungeonEscape";

        public static string ToRuntimePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            var normalized = assetPath.Replace('\\', '/');
            var editorPath = Path.Combine(Application.dataPath, normalized.Replace("Assets/", string.Empty));
            if (File.Exists(editorPath) || Directory.Exists(editorPath))
            {
                return editorPath;
            }

            if (normalized.StartsWith(DungeonEscapeAssetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = normalized.Substring(DungeonEscapeAssetPrefix.Length);
                return Path.Combine(Application.streamingAssetsPath, StreamingDungeonEscapeFolder, relativePath);
            }

            return editorPath;
        }
    }
}
