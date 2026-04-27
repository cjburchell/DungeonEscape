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

            CreateCamera();

            AssignTextAsset(bootstrap, "itemDefinitionsJson", "Assets/DungeonEscape/Data/itemdef.json");
            AssignTextAsset(bootstrap, "customItemsJson", "Assets/DungeonEscape/Data/customitems.json");
            AssignTextAsset(bootstrap, "skillsJson", "Assets/DungeonEscape/Data/skills.json");
            AssignTextAsset(bootstrap, "spellsJson", "Assets/DungeonEscape/Data/spells.json");
            AssignTextAsset(bootstrap, "monstersJson", "Assets/DungeonEscape/Data/allmonsters.json");
            AssignTextAsset(bootstrap, "questsJson", "Assets/DungeonEscape/Data/quests.json");
            AssignTextAsset(bootstrap, "dialogJson", "Assets/DungeonEscape/Data/dialog.json");
            AssignTextAsset(bootstrap, "classLevelsJson", "Assets/DungeonEscape/Data/classlevels.json");
            AssignTextAsset(bootstrap, "namesJson", "Assets/DungeonEscape/Data/names.json");
            AssignTextAsset(bootstrap, "statNamesJson", "Assets/DungeonEscape/Data/statnames.json");
            AssignString(bootstrap, "testMapAssetPath", "Assets/DungeonEscape/Maps/overworld.tmx");

            CreatePreviewStatusView(bootstrap);
            var mapView = CreateMapView();
            CreatePlayerController(mapView);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        private static TiledMapView CreateMapView()
        {
            var previewObject = new GameObject("TiledMapView");
            var renderer = previewObject.AddComponent<TiledMapView>();
            var serializedObject = new SerializedObject(renderer);
            serializedObject.FindProperty("bootstrap").objectReferenceValue = UnityEngine.Object.FindObjectOfType<DungeonEscapeBootstrap>();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return renderer;
        }

        private static void CreatePreviewStatusView(DungeonEscapeBootstrap bootstrap)
        {
            var statusObject = new GameObject("PreviewStatusView");
            var statusView = statusObject.AddComponent<DungeonEscapeDataDebugView>();
            var serializedObject = new SerializedObject(statusView);
            serializedObject.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePlayerController(TiledMapView mapView)
        {
            var playerObject = new GameObject("PlayerGridController");
            var marker = playerObject.AddComponent<PlayerGridController>();
            var serializedObject = new SerializedObject(marker);
            serializedObject.FindProperty("mapView").objectReferenceValue = mapView;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
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

        private static void AssignString(DungeonEscapeBootstrap bootstrap, string fieldName, string value)
        {
            var serializedObject = new SerializedObject(bootstrap);
            serializedObject.FindProperty(fieldName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
