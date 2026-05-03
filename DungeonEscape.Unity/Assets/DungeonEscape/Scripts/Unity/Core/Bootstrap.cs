using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Core
{
    public sealed class Bootstrap : MonoBehaviour
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
        private bool startupComplete;

        private void Awake()
        {
            EnsureCamera();
            Settings = SettingsCache.Load();
            DisplaySettings.Apply(Settings);
            UiSettings.GetOrCreate().ApplySettings(Settings);
            Audio.GetOrCreate().ApplySettings(Settings);

            if (Settings.SkipSplashAndLoadQuickSave)
            {
                LoadGameDataAndStart();
            }
            else
            {
                EnsureSplashScreen();
            }
        }

        private void LoadGameDataAndStart()
        {
            if (startupComplete)
            {
                return;
            }

            startupComplete = true;
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
            GameDataCache.Load(Data);
            var gameState = GameState.GetOrCreate();
            EnsureGameMenu();
            EnsurePartyStatusWindow();
            EnsureGoldWindow();
            if (Settings.SkipSplashAndLoadQuickSave)
            {
                if (gameState.HasQuickSave())
                {
                    gameState.LoadQuick();
                }
            }

            ValidateTilesets(Data.TestMap, testMapAssetPath);
        }

        private IEnumerator Start()
        {
            if (Settings == null || Settings.SkipSplashAndLoadQuickSave)
            {
                yield break;
            }

            yield return null;
            yield return null;
            LoadGameDataAndStart();

            while (SplashScreen.IsVisible)
            {
                yield return null;
            }

            EnsureTitleMenu();
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
                var fullPath = UnityAssetPath.ToRuntimePath(assetPath);
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
            return UnityAssetPath.ToRuntimePath(assetPath);
        }

        private static void EnsureCamera()
        {
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                existingCamera = FindAnyObjectByType<Camera>();
            }

            if (existingCamera != null)
            {
                EnsureAudioListener(existingCamera.gameObject);
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            EnsureAudioListener(cameraObject);
        }

        private static void EnsureAudioListener(GameObject cameraObject)
        {
            if (cameraObject == null || FindAnyObjectByType<AudioListener>() != null)
            {
                return;
            }

            cameraObject.AddComponent<AudioListener>();
        }

        private static void EnsureGameMenu()
        {
            if (FindAnyObjectByType<GameMenu>() != null)
            {
                return;
            }

            new GameObject("GameMenu").AddComponent<GameMenu>();
        }

        private static void EnsurePartyStatusWindow()
        {
            if (FindAnyObjectByType<PartyStatusWindow>() != null)
            {
                return;
            }

            new GameObject("PartyStatusWindow").AddComponent<PartyStatusWindow>();
        }

        private static void EnsureGoldWindow()
        {
            if (FindAnyObjectByType<GoldWindow>() != null)
            {
                return;
            }

            new GameObject("GoldWindow").AddComponent<GoldWindow>();
        }

        private static void EnsureTitleMenu()
        {
            if (FindAnyObjectByType<TitleMenu>() != null)
            {
                return;
            }

            new GameObject("TitleMenu").AddComponent<TitleMenu>();
        }

        private static void EnsureSplashScreen()
        {
            if (FindAnyObjectByType<SplashScreen>() != null)
            {
                return;
            }

            new GameObject("SplashScreen").AddComponent<SplashScreen>();
        }
    }
}
