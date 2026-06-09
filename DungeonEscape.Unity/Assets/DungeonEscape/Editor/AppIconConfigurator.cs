using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DungeonEscape.Unity.Editor
{
    /// <summary>
    /// Keeps the Unity player application icon wired to the project icon asset.
    /// Unity serializes icon configuration inside ProjectSettings, but applying it
    /// through the editor API avoids hand-editing brittle ProjectSettings YAML.
    /// </summary>
    [InitializeOnLoad]
    public static class AppIconConfigurator
    {
        private const string IconAssetPath = "Assets/DungeonEscape/Images/Icons/DungeonEscape.png";

        static AppIconConfigurator()
        {
            EditorApplication.delayCall += ConfigureIcon;
        }

        [MenuItem("Dungeon Escape/Configure App Icon")]
        public static void ConfigureIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAssetPath);
            if (icon == null)
            {
                Debug.LogWarning($"Dungeon Escape app icon not found at '{IconAssetPath}'.");
                return;
            }

            var iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
            var icons = new Texture2D[iconSizes.Length];
            for (var i = 0; i < icons.Length; i++)
            {
                icons[i] = icon;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, IconKind.Application);
            EditorUtility.SetDirty(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}