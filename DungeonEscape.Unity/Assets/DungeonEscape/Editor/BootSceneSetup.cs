using System.IO;
using Redpoint.DungeonEscape.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Redpoint.DungeonEscape.UnityEditor
{
    public static class BootSceneSetup
    {
        private const string SceneDirectory = "Assets/DungeonEscape/Scenes";
        private const string ScenePath = SceneDirectory + "/Boot.unity";

        [MenuItem("Dungeon Escape/Setup Boot Scene")]
        public static void SetupBootScene()
        {
            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
                AssetDatabase.Refresh();
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Boot";

            var bootstrapObject = new GameObject("DungeonEscapeBootstrap");
            var bootstrap = bootstrapObject.AddComponent<DungeonEscapeBootstrap>();

            AssignTextAsset(bootstrap, "itemDefinitionsJson", "Assets/DungeonEscape/Data/itemdef.json");
            AssignTextAsset(bootstrap, "questsJson", "Assets/DungeonEscape/Data/quests.json");
            AssignTextAsset(bootstrap, "dialogJson", "Assets/DungeonEscape/Data/dialog.json");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        private static void AssignTextAsset(DungeonEscapeBootstrap bootstrap, string fieldName, string assetPath)
        {
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (textAsset == null)
            {
                Debug.LogWarning("Could not find JSON asset: " + assetPath);
                return;
            }

            var serializedObject = new SerializedObject(bootstrap);
            serializedObject.FindProperty(fieldName).objectReferenceValue = textAsset;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
