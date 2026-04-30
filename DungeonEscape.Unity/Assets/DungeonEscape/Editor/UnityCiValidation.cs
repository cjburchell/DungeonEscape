using System.IO;
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

            Debug.Log("Dungeon Escape Unity CI validation passed.");
            EditorApplication.Exit(0);
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
