using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

using Redpoint.DungeonEscape.Unity.UI;
namespace Redpoint.DungeonEscape.Unity.Core
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField]
        private TextAsset magicItemsJson;

        [SerializeField]
        private TextAsset nonMagicItemsJson;

        [SerializeField]
        private TextAsset weaponItemsJson;

        [SerializeField]
        private TextAsset armorItemsJson;

        [SerializeField]
        private TextAsset skillsJson;

        [SerializeField]
        private TextAsset spellsJson;

        [SerializeField]
        private TextAsset monstersJson;

        [SerializeField]
        [FormerlySerializedAs("classLevelsJson")]
        private TextAsset classesJson;

        [SerializeField]
        private TextAsset speciesJson;

        [SerializeField]
        private TextAsset backgroundsJson;

        [SerializeField]
        private TextAsset namesJson;

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
            if (CampaignRuntime.Catalog.Campaigns.Count == 0)
            {
                Data = new DungeonEscapeDataSet();
                CampaignRuntime.Initialize(Data);
                GameState.GetOrCreate();
                EnsureTitleMenu();
                return;
            }
            Data = new DungeonEscapeDataSet
            {
                CustomItems = LoadCustomItems(magicItemsJson, nonMagicItemsJson, weaponItemsJson, armorItemsJson),
                Skills = LoadJson<List<Skill>>(skillsJson, "skills"),
                Spells = LoadJson<List<Spell>>(spellsJson, "spells"),
                Monsters = LoadJson<List<Monster>>(monstersJson, "monsters"),
                ClassLevels = LoadJson<List<ClassStats>>(classesJson, "classes", "Assets/DungeonEscape/Data/class.json"),
                Species = LoadJson<List<SpeciesDefinition>>(speciesJson, "species", "Assets/DungeonEscape/Data/species.json") ?? DndCharacterRules.GetDefaultSpeciesDefinitions(),
                Backgrounds = LoadJson<List<BackgroundDefinition>>(backgroundsJson, "backgrounds", "Assets/DungeonEscape/Data/backgrounds.json") ?? DndCharacterRules.GetDefaultBackgrounds(),
                Names = LoadJson<Names>(namesJson, "names")
            };

            var context = CampaignRuntime.Initialize(Data);
            Data = context.Data;
            var startMapPath = context.Maps.Get(context.Campaign.StartMapId).AssetPath;
            Data.TestMap = LoadTiledMap(null, startMapPath, "campaign start map");
            GameDataCache.Load(Data);
            var gameState = GameState.GetOrCreate();
            EnsureGameMenu();
            EnsurePartyStatusWindow();
            EnsureGoldWindow();
            if (Settings.SkipSplashAndLoadQuickSave)
            {
                if (gameState.HasQuickSave())
                {
                    gameState.LoadQuick(true);
                }
            }

            ValidateTilesets(Data.TestMap, startMapPath);
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

        private static List<Item> LoadCustomItems(params TextAsset[] assets)
        {
            var loadedItems = new List<Item>();
            foreach (var asset in assets.Where(item => item != null))
            {
                var items = LoadJson<List<Item>>(asset, "custom items");
                if (items != null)
                {
                    loadedItems.AddRange(items);
                }
            }

            // Older boot scenes do not serialize every current item catalog field.
            // Read the canonical files as well so base definitions are always complete.
            foreach (var name in new[] { "customitems", "magicitems", "nonmagicitems", "weaponitems", "armoritems" })
            {
                var json = CampaignRuntime.ReadAsset("Assets/DungeonEscape/Data/" + name + ".json");
                if (json == null) throw new InvalidOperationException("Missing shared item catalog: " + name);
                loadedItems.AddRange(Newtonsoft.Json.JsonConvert.DeserializeObject<List<Item>>(json));
            }

            return loadedItems
                .Where(item => item != null)
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .SelectMany(group => string.IsNullOrWhiteSpace(group.Key) ? group : group.Take(1))
                .ToList();
        }

        private static T LoadJson<T>(TextAsset asset, string label)
        {
            return LoadJson<T>(asset, label, null);
        }

        private static T LoadJson<T>(TextAsset asset, string label, string assetPath)
        {
            if (asset == null)
            {
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var fullPath = UnityAssetPath.ToRuntimePath(assetPath);
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            return UnityJsonLoader.LoadFromText<T>(File.ReadAllText(fullPath));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError("Failed to deserialize " + label + " JSON from " + assetPath + ": " + exception.Message);
                            return default(T);
                        }
                    }
                }

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

                tileset.UnityTilesetPath = ContentPaths.ResolveRelative(mapAssetPath, tileset.Source);
                var tilesetFullPath = ToFullAssetPath(tileset.UnityTilesetPath);
                tileset.TilesetFound = File.Exists(tilesetFullPath);

                if (!tileset.TilesetFound)
                {
                    Debug.LogWarning("Tileset not found: " + tileset.UnityTilesetPath);
                    continue;
                }

                tileset.Document = TiledTilesetDocumentInfo.Parse(File.ReadAllText(tilesetFullPath));
                tileset.UnityImagePath = ContentPaths.ResolveRelative(tileset.UnityTilesetPath, tileset.Document.ImageSource);
                tileset.ImageFound = File.Exists(ToFullAssetPath(tileset.UnityImagePath));

                if (!tileset.ImageFound)
                {
                    Debug.LogWarning("Tileset image not found: " + tileset.UnityImagePath);
                }
            }
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
