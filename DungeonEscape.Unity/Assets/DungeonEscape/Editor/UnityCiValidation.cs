using System.IO;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

            if (TryExecuteOpenCSharpProjectMenu())
            {
                Debug.Log("Unity C# project files generated through Assets/Open C# Project menu command.");
                return;
            }

            Debug.LogWarning("Could not generate Unity C# project files for ReSharper scanning.");
        }

        private static bool TryInvokeSyncVs()
        {
            var syncVsType = Type.GetType("UnityEditor.SyncVS,UnityEditor");
            if (syncVsType == null)
            {
                Debug.Log("UnityEditor.SyncVS type was not found.");
                return false;
            }

            var syncSolution = syncVsType.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (syncSolution == null)
            {
                Debug.Log("UnityEditor.SyncVS.SyncSolution method was not found.");
                return false;
            }

            syncSolution.Invoke(null, null);
            return true;
        }

        private static bool TryInvokeCodeEditorSync()
        {
            var codeEditorType = Type.GetType("UnityEditor.CodeEditor.CodeEditor,UnityEditor");
            if (codeEditorType == null)
            {
                Debug.Log("UnityEditor.CodeEditor.CodeEditor type was not found.");
                return false;
            }

            var currentEditor = codeEditorType.GetProperty("CurrentEditor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (currentEditor == null)
            {
                Debug.Log("UnityEditor.CodeEditor.CodeEditor.CurrentEditor property was not found.");
                return false;
            }

            var editor = currentEditor.GetValue(null, null);
            if (editor == null)
            {
                Debug.Log("Unity current code editor was null.");
                return false;
            }

            var syncAll = editor.GetType().GetMethod("SyncAll", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (syncAll == null)
            {
                Debug.Log("Unity current code editor SyncAll method was not found on " + editor.GetType().FullName + ".");
                return false;
            }

            syncAll.Invoke(editor, null);
            return true;
        }

        private static bool TryExecuteOpenCSharpProjectMenu()
        {
            try
            {
                return EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
            }
            catch (Exception exception)
            {
                Debug.Log("Assets/Open C# Project menu command failed: " + exception.Message);
                return false;
            }
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
