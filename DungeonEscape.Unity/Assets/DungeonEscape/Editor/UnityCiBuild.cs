using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Redpoint.DungeonEscape.UnityEditor
{
    public static class UnityCiBuild
    {
        private const string BootScenePath = "Assets/DungeonEscape/Scenes/Boot.unity";
        private const string WindowsBuildPath = "Builds/Windows/DungeonEscape.exe";

        public static void BuildWindows64()
        {
            BuildPlayer(
                BuildTarget.StandaloneWindows64,
                WindowsBuildPath);
        }

        private static void BuildPlayer(BuildTarget target, string outputPath)
        {
            if (!File.Exists(BootScenePath))
            {
                Debug.LogError("Build scene is missing: " + BootScenePath);
                EditorApplication.Exit(1);
                return;
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { BootScenePath },
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Unity build succeeded: " + outputPath + " (" + report.summary.totalSize + " bytes)");
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError("Unity build failed: " + report.summary.result);
            EditorApplication.Exit(1);
        }
    }
}
