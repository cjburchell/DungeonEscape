using System;
using System.Collections.Generic;
using System.IO;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeBootstrap : MonoBehaviour
    {
        [SerializeField]
        private TextAsset itemDefinitionsJson;

        [SerializeField]
        private TextAsset customItemsJson;

        [SerializeField]
        private TextAsset skillsJson;

        [SerializeField]
        private TextAsset spellsJson;

        [SerializeField]
        private TextAsset monstersJson;

        [SerializeField]
        private TextAsset questsJson;

        [SerializeField]
        private TextAsset dialogJson;

        [SerializeField]
        private TextAsset classLevelsJson;

        [SerializeField]
        private TextAsset namesJson;

        [SerializeField]
        private TextAsset statNamesJson;

        [SerializeField]
        private TextAsset testMapTmx;

        [SerializeField]
        private string testMapAssetPath = "Assets/DungeonEscape/Maps/overworld.tmx";

        public DungeonEscapeDataSet Data { get; private set; }
        public Settings Settings { get; private set; }

        private void Awake()
        {
            EnsureCamera();
            Settings = DungeonEscapeSettingsCache.Load();
            DungeonEscapeUiSettings.GetOrCreate().ApplySettings(Settings);

            Debug.Log("Dungeon Escape Unity bootstrap starting.");

            Data = new DungeonEscapeDataSet
            {
                ItemDefinitions = LoadJson<List<ItemDefinition>>(itemDefinitionsJson, "item definitions"),
                CustomItems = LoadJson<List<Item>>(customItemsJson, "custom items"),
                Skills = LoadJson<List<Skill>>(skillsJson, "skills"),
                Spells = LoadJson<List<Spell>>(spellsJson, "spells"),
                Monsters = LoadJson<List<Monster>>(monstersJson, "monsters"),
                Quests = LoadJson<List<Quest>>(questsJson, "quests"),
                Dialogs = LoadJson<List<Dialog>>(dialogJson, "dialog"),
                ClassLevels = LoadJson<List<ClassStats>>(classLevelsJson, "class levels"),
                Names = LoadJson<Names>(namesJson, "names"),
                StatNames = LoadJson<List<StatName>>(statNamesJson, "stat names"),
                TestMap = LoadTiledMap(testMapTmx, testMapAssetPath, "test map")
            };

            Data.Link();
            DungeonEscapeGameDataCache.Load(Data);
            DungeonEscapeGameState.GetOrCreate();
            EnsureGameMenu();
            ValidateTilesets(Data.TestMap, testMapAssetPath);

            Debug.Log("Dungeon Escape data loaded. Item definitions: " + Count(Data.ItemDefinitions) +
                      ", custom items: " + Count(Data.CustomItems) +
                      ", skills: " + Count(Data.Skills) +
                      ", spells: " + Count(Data.Spells) +
                      ", monsters: " + Count(Data.Monsters) +
                      ", quests: " + Count(Data.Quests) +
                      ", dialog sets: " + Count(Data.Dialogs) +
                      ", class levels: " + Count(Data.ClassLevels) +
                      ", stat names: " + Count(Data.StatNames) +
                      ", test map: " + (Data.TestMap == null ? "none" : Data.TestMap.Width + "x" + Data.TestMap.Height));
        }

        private static T LoadJson<T>(TextAsset asset, string label)
        {
            if (asset == null)
            {
                Debug.LogError("Missing " + label + " JSON TextAsset.");
                return default(T);
            }

            try
            {
                var data = UnityJsonLoader.LoadFromTextAsset<T>(asset);
                Debug.Log("Loaded " + label + " JSON: " + asset.name);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to deserialize " + label + " JSON from " + asset.name + ": " + exception.Message);
                return default(T);
            }
        }

        private static int Count<T>(ICollection<T> values)
        {
            return values == null ? 0 : values.Count;
        }

        private static TiledMapInfo LoadTiledMap(TextAsset asset, string assetPath, string label)
        {
            var text = asset == null ? null : asset.text;
            var sourceName = asset == null ? assetPath : asset.name;

            if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(assetPath))
            {
                var fullPath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
                if (File.Exists(fullPath))
                {
                    text = File.ReadAllText(fullPath);
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                Debug.LogError("Missing " + label + " TMX asset: " + assetPath);
                return null;
            }

            try
            {
                var map = TiledMapInfo.Parse(text);
                Debug.Log("Loaded " + label + " TMX: " + sourceName);
                return map;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to parse " + label + " TMX from " + sourceName + ": " + exception.Message);
                return null;
            }
        }

        private static void ValidateTilesets(TiledMapInfo map, string mapAssetPath)
        {
            if (map == null || map.Tilesets == null)
            {
                return;
            }

            foreach (var tileset in map.Tilesets)
            {
                if (string.IsNullOrEmpty(tileset.Source))
                {
                    tileset.TilesetFound = true;
                    continue;
                }

                tileset.UnityTilesetPath = ResolveTilesetAssetPath(tileset.Source);
                var tilesetFullPath = ToFullAssetPath(tileset.UnityTilesetPath);
                tileset.TilesetFound = File.Exists(tilesetFullPath);

                if (!tileset.TilesetFound)
                {
                    Debug.LogWarning("Tileset not found: " + tileset.UnityTilesetPath);
                    continue;
                }

                tileset.Document = TiledTilesetDocumentInfo.Parse(File.ReadAllText(tilesetFullPath));
                tileset.UnityImagePath = ResolveTilesetImageAssetPath(tileset.Document.ImageSource);
                tileset.ImageFound = File.Exists(ToFullAssetPath(tileset.UnityImagePath));

                if (!tileset.ImageFound)
                {
                    Debug.LogWarning("Tileset image not found: " + tileset.UnityImagePath);
                }
            }
        }

        private static string ResolveTilesetAssetPath(string source)
        {
            return "Assets/DungeonEscape/Tilesets/" + Path.GetFileName(source);
        }

        private static string ResolveTilesetImageAssetPath(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            var normalized = source.Replace('\\', '/');
            const string imagesPrefix = "images/";
            if (normalized.StartsWith(imagesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(imagesPrefix.Length);
            }

            return "Assets/DungeonEscape/Images/" + normalized;
        }

        private static string ToFullAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            return Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null || FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
        }

        private static void EnsureGameMenu()
        {
            if (FindFirstObjectByType<DungeonEscapeGameMenu>() != null)
            {
                return;
            }

            new GameObject("DungeonEscapeGameMenu").AddComponent<DungeonEscapeGameMenu>();
        }
    }
}
