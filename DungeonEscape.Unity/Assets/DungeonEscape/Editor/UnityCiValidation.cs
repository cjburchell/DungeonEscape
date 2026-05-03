using System.IO;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.UnityEditor
{
    public static class UnityCiValidation
    {
        private const string BootScenePath = "Assets/DungeonEscape/Scenes/Boot.unity";

        public static void ValidateProject()
        {
            var failed = false;
            failed |= !ValidateAsset(BootScenePath);
            failed |= !ValidateAsset("Assets/DungeonEscape/Data/itemdef.json");
            failed |= !ValidateAsset("Assets/DungeonEscape/Data/quests.json");
            failed |= !ValidateAsset("Assets/DungeonEscape/Data/dialog.json");
            failed |= !ValidateAsset("Assets/DungeonEscape/Maps/overworld.tmx");
            failed |= !ValidateAsset("Assets/DungeonEscape/Images/sprites/hero.png");
            failed |= !ValidateAsset("Assets/DungeonEscape/Images/sprites/cart.png");
            failed |= !ValidateAsset("Assets/DungeonEscape/Images/sprites/ship2.png");

            var scene = EditorSceneManager.OpenScene(BootScenePath);
            if (!scene.IsValid())
            {
                Debug.LogError("Boot scene could not be opened: " + BootScenePath);
                failed = true;
            }

            if (failed)
            {
                EditorApplication.Exit(1);
                return;
            }

            GenerateCSharpProjects();
            Debug.Log("Dungeon Escape Unity CI validation passed.");
            EditorApplication.Exit(0);
        }

        private static void GenerateCSharpProjects()
        {
            if (TryInvokeSyncVs())
            {
                Debug.Log("Unity C# project files generated through UnityEditor.SyncVS.");
                return;
            }

            if (TryInvokeCodeEditorSync())
            {
                Debug.Log("Unity C# project files generated through UnityEditor.CodeEditor.");
                return;
            }

            Debug.LogWarning("Could not generate Unity C# project files for ReSharper scanning.");
        }

        private static bool TryInvokeSyncVs()
        {
            var syncVsType = Type.GetType("UnityEditor.SyncVS,UnityEditor");
            var syncSolution = syncVsType == null
                ? null
                : syncVsType.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (syncSolution == null)
            {
                return false;
            }

            syncSolution.Invoke(null, null);
            return true;
        }

        private static bool TryInvokeCodeEditorSync()
        {
            var codeEditorType = Type.GetType("UnityEditor.CodeEditor.CodeEditor,UnityEditor");
            var currentEditor = codeEditorType == null
                ? null
                : codeEditorType.GetProperty("CurrentEditor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var editor = currentEditor == null ? null : currentEditor.GetValue(null, null);
            var syncAll = editor == null
                ? null
                : editor.GetType().GetMethod("SyncAll", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (syncAll == null)
            {
                return false;
            }

            syncAll.Invoke(editor, null);
            return true;
        }

        private static bool ValidateAsset(string assetPath)
        {
            if (File.Exists(assetPath))
            {
                return true;
            }

            Debug.LogError("Required Unity asset is missing: " + assetPath);
            return false;
        }
    }
}
