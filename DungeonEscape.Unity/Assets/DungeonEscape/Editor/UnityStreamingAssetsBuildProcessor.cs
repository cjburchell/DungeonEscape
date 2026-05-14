using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.UnityEditor
{
    public sealed class UnityStreamingAssetsBuildProcessor : IPreprocessBuildWithReport
    {
        private static readonly string[] SourceFolders =
        {
            "Assets/DungeonEscape/Data",
            "Assets/DungeonEscape/Audio",
            "Assets/DungeonEscape/Images",
            "Assets/DungeonEscape/Maps",
            "Assets/DungeonEscape/Tilesets"
        };

        private static readonly string[] ExcludedExtensions =
        {
            ".meta",
            ".xnb",
            ".spritefont",
            ".mgfxo",
            ".mgcb"
        };

        public int callbackOrder
        {
            get { return 0; }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            CopyRuntimeAssets();
        }

        public static void CopyRuntimeAssets()
        {
            var projectDirectory = Directory.GetParent(Application.dataPath);
            if (projectDirectory == null)
            {
                throw new InvalidOperationException("Unable to resolve the Unity project root.");
            }

            var projectRoot = projectDirectory.FullName;
            var targetRoot = Path.Combine(projectRoot, "Assets/StreamingAssets/DungeonEscape");

            if (Directory.Exists(targetRoot))
            {
                Directory.Delete(targetRoot, true);
            }

            Directory.CreateDirectory(targetRoot);

            foreach (var sourceFolder in SourceFolders)
            {
                var sourcePath = Path.Combine(projectRoot, sourceFolder);
                if (!Directory.Exists(sourcePath))
                {
                    Debug.LogWarning("Runtime asset source folder not found: " + sourceFolder);
                    continue;
                }

                var targetPath = Path.Combine(targetRoot, Path.GetFileName(sourceFolder));
                CopyDirectory(sourcePath, targetPath);
            }

            AssetDatabase.Refresh();
            Debug.Log("Copied Dungeon Escape runtime assets to StreamingAssets.");
        }

        private static void CopyDirectory(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);

            foreach (var filePath in Directory.GetFiles(sourcePath))
            {
                if (ShouldSkip(filePath))
                {
                    continue;
                }

                File.Copy(filePath, Path.Combine(targetPath, Path.GetFileName(filePath)), true);
            }

            foreach (var directoryPath in Directory.GetDirectories(sourcePath))
            {
                CopyDirectory(directoryPath, Path.Combine(targetPath, Path.GetFileName(directoryPath)));
            }
        }

        private static bool ShouldSkip(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            foreach (var excludedExtension in ExcludedExtensions)
            {
                if (string.Equals(extension, excludedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
