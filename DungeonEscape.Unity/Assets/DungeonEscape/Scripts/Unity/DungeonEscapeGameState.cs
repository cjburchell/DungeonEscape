using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeGameState : MonoBehaviour
    {
        private const string SaveFileVersion = "1.0";
        private const int MaxSaveSlots = 5;

        [SerializeField]
        private string defaultMapId = "overworld";

        [SerializeField]
        private int defaultColumn = 30;

        [SerializeField]
        private int defaultRow = 25;

        public GameSave CurrentSave { get; private set; }
        public GameFile GameFile { get; private set; }
        public event Action<GameSave> SaveLoaded;

        public Party Party
        {
            get { return CurrentSave == null ? null : CurrentSave.Party; }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                SaveQuick();
            }
            else if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadQuick();
            }
        }

        public void EnsureInitialized()
        {
            if (CurrentSave != null)
            {
                return;
            }

            GameFile = LoadGameFile();
            CurrentSave = GetQuickSave(GameFile);
            if (!IsUsableSave(CurrentSave))
            {
                CurrentSave = CreateDefaultSave();
                UpsertQuickSave(CurrentSave);
            }
        }

        public void SetCurrentMap(string mapId)
        {
            EnsureInitialized();
            Party.CurrentMapId = TiledMapLoader.NormalizeMapId(mapId);
            Party.CurrentMapIsOverWorld = Party.CurrentMapId == "overworld";
        }

        public DungeonEscapeMapTransition CreateWarpTransition(TiledMapWarp warp)
        {
            EnsureInitialized();

            var mapId = TiledMapLoader.NormalizeMapId(warp.MapId);
            SetCurrentMap(mapId);

            return new DungeonEscapeMapTransition
            {
                MapId = mapId,
                SpawnId = warp.SpawnId,
                UseSavedOverWorldPosition = mapId == "overworld" && string.IsNullOrEmpty(warp.SpawnId)
            };
        }

        public void SetCurrentPosition(WorldPosition position)
        {
            EnsureInitialized();
            Party.CurrentPosition = position;

            if (Party.CurrentMapIsOverWorld)
            {
                Party.OverWorldPosition = position;
            }
        }

        public void SetCurrentDirection(Direction direction)
        {
            EnsureInitialized();
            Party.CurrentDirection = direction;
        }

        public void IncrementStepCount()
        {
            EnsureInitialized();
            Party.StepCount++;
        }

        public void SaveQuick()
        {
            EnsureInitialized();
            CurrentSave.IsQuick = true;
            CurrentSave.Time = DateTime.Now;
            UpsertQuickSave(CurrentSave);
            SaveGameFile();
            Debug.Log("Quick saved to " + GetSaveFilePath());
        }

        public void LoadQuick()
        {
            GameFile = LoadGameFile();
            var quickSave = GetQuickSave(GameFile);
            if (!IsUsableSave(quickSave))
            {
                Debug.LogWarning("No quick save found.");
                return;
            }

            CurrentSave = quickSave;
            Debug.Log("Quick loaded: " + CurrentSave.Name);
            if (SaveLoaded != null)
            {
                SaveLoaded(CurrentSave);
            }
        }

        private GameSave CreateDefaultSave()
        {
            var party = new Party
            {
                PlayerName = "Player",
                CurrentMapId = TiledMapLoader.NormalizeMapId(defaultMapId),
                CurrentPosition = new WorldPosition(defaultColumn, defaultRow)
            };
            party.CurrentMapIsOverWorld = party.CurrentMapId == "overworld";
            party.OverWorldPosition = party.CurrentPosition.Value;

            return new GameSave
            {
                Party = party,
                IsQuick = true,
                Time = DateTime.Now
            };
        }

        private void UpsertQuickSave(GameSave save)
        {
            if (GameFile == null)
            {
                GameFile = new GameFile { Version = SaveFileVersion };
            }

            var existing = GetQuickSave(GameFile);
            if (existing != null && !ReferenceEquals(existing, save))
            {
                GameFile.Saves.Remove(existing);
            }

            save.IsQuick = true;
            if (!GameFile.Saves.Contains(save))
            {
                GameFile.Saves.Add(save);
            }
        }

        private void SaveGameFile()
        {
            if (!Directory.Exists(GetSavePath()))
            {
                Directory.CreateDirectory(GetSavePath());
            }

            File.WriteAllText(
                GetSaveFilePath(),
                JsonConvert.SerializeObject(
                    GameFile,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
        }

        private static GameFile LoadGameFile()
        {
            GameFile file = null;
            var path = GetSaveFilePath();
            if (File.Exists(path))
            {
                try
                {
                    file = JsonConvert.DeserializeObject<GameFile>(File.ReadAllText(path));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Failed to load save file from " + path + ": " + exception.Message);
                }
            }

            if (file == null || file.Version != SaveFileVersion)
            {
                file = new GameFile { Version = SaveFileVersion };
            }

            for (var i = file.Saves.Count(save => !save.IsQuick); i < MaxSaveSlots; i++)
            {
                file.Saves.Add(new GameSave());
            }

            return file;
        }

        private static GameSave GetQuickSave(GameFile file)
        {
            return file == null ? null : file.Saves.FirstOrDefault(save => save.IsQuick);
        }

        private static bool IsUsableSave(GameSave save)
        {
            return save != null &&
                   save.Party != null &&
                   !string.IsNullOrEmpty(save.Party.CurrentMapId) &&
                   save.Party.CurrentPosition.HasValue;
        }

        private static string GetSavePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Redpoint",
                "DungeonEscape");
        }

        private static string GetSaveFilePath()
        {
            return Path.Combine(GetSavePath(), "save.json");
        }

        public static DungeonEscapeGameState GetOrCreate()
        {
            var state = FindObjectOfType<DungeonEscapeGameState>();
            if (state != null)
            {
                state.EnsureInitialized();
                return state;
            }

            state = new GameObject("DungeonEscapeGameState").AddComponent<DungeonEscapeGameState>();
            state.EnsureInitialized();
            return state;
        }
    }
}
