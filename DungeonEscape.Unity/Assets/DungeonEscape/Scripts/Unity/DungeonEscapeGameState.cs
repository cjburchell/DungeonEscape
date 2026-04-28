using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public bool ShouldApplyInitialSpawn { get; private set; }
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
            else if (Input.GetKeyDown(KeyCode.F10))
            {
                RestartNewGame();
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
                ShouldApplyInitialSpawn = true;
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

        public string StartQuest(string questId)
        {
            EnsureInitialized();
            Quest quest;
            if (string.IsNullOrEmpty(questId) ||
                DungeonEscapeGameDataCache.Current == null ||
                !DungeonEscapeGameDataCache.Current.TryGetQuest(questId, out quest))
            {
                return "";
            }

            if (Party.ActiveQuests.Any(activeQuest => activeQuest.Id == quest.Id))
            {
                return "";
            }

            Party.ActiveQuests.Add(CreateActiveQuest(quest));
            return "Started quest: " + quest.Name;
        }

        public string AdvanceQuest(string questId, int? nextStage)
        {
            EnsureInitialized();
            Quest quest;
            if (string.IsNullOrEmpty(questId) ||
                DungeonEscapeGameDataCache.Current == null ||
                !DungeonEscapeGameDataCache.Current.TryGetQuest(questId, out quest))
            {
                return "";
            }

            var activeQuest = Party.ActiveQuests.FirstOrDefault(item => item.Id == quest.Id);
            if (activeQuest == null)
            {
                activeQuest = CreateActiveQuest(quest);
                Party.ActiveQuests.Add(activeQuest);
            }

            if (nextStage.HasValue)
            {
                activeQuest.CurrentStage = nextStage.Value;
            }

            var activeStage = activeQuest.Stages.FirstOrDefault(item => item.Number == activeQuest.CurrentStage);
            if (activeStage != null)
            {
                activeStage.Completed = true;
            }

            var currentStage = quest.Stages == null
                ? null
                : quest.Stages.FirstOrDefault(item => item.Number == activeQuest.CurrentStage);

            if (currentStage == null || !currentStage.CompleteQuest || activeQuest.Completed)
            {
                return "";
            }

            activeQuest.Completed = true;
            var message = new StringBuilder();
            message.AppendLine("You have completed the quest " + quest.Name);

            if (quest.Gold != 0)
            {
                Party.Gold += quest.Gold;
                message.AppendLine("The party got " + quest.Gold + " gold.");
            }

            if (quest.Items != null)
            {
                foreach (var itemId in quest.Items)
                {
                    message.Append(GiveItem(itemId));
                }
            }

            return message.ToString().TrimEnd();
        }

        public string GiveItems(IEnumerable<string> itemIds)
        {
            if (itemIds == null)
            {
                return "";
            }

            var message = new StringBuilder();
            foreach (var itemId in itemIds)
            {
                message.Append(GiveItem(itemId));
            }

            return message.ToString().TrimEnd();
        }

        public string GiveItem(string itemId)
        {
            EnsureInitialized();
            Item item;
            if (string.IsNullOrEmpty(itemId) ||
                DungeonEscapeGameDataCache.Current == null ||
                !DungeonEscapeGameDataCache.Current.TryGetCustomItem(itemId, out item))
            {
                return string.IsNullOrEmpty(itemId) ? "" : "Missing item: " + itemId + "\n";
            }

            var member = Party.AddItem(new ItemInstance(item));
            if (member == null)
            {
                return "No party member can carry " + item.Name + ".\n";
            }

            return member.Name + " got " + item.Name + ".\n" + CheckQuest(item);
        }

        public string TakeItem(string itemId, string recipientName)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(itemId))
            {
                return "";
            }

            var result = Party.RemoveItem(itemId);
            var item = result.Item1;
            var member = result.Item2;
            if (item == null || member == null)
            {
                return "You do not have " + itemId + ".";
            }

            return member.Name + " gave " + item.Name + " to " + recipientName + ".";
        }

        private string CheckQuest(Item item)
        {
            return item == null || string.IsNullOrEmpty(item.QuestId)
                ? ""
                : AdvanceQuest(item.QuestId, item.NextStage);
        }

        private static ActiveQuest CreateActiveQuest(Quest quest)
        {
            return new ActiveQuest
            {
                Id = quest.Id,
                CurrentStage = 0,
                Stages = quest.Stages == null
                    ? new List<QuestStageState>()
                    : quest.Stages.Select(item => new QuestStageState { Number = item.Number }).ToList()
            };
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
            ShouldApplyInitialSpawn = false;
            Debug.Log("Quick loaded: " + CurrentSave.Name);
            if (SaveLoaded != null)
            {
                SaveLoaded(CurrentSave);
            }
        }

        public void RestartNewGame()
        {
            GameFile = LoadGameFile();
            CurrentSave = CreateDefaultSave();
            CurrentSave.IsQuick = false;
            ShouldApplyInitialSpawn = true;
            Debug.Log("Started a new game without loading a saved level.");
            if (SaveLoaded != null)
            {
                SaveLoaded(CurrentSave);
            }
        }

        public void MarkInitialSpawnApplied()
        {
            ShouldApplyInitialSpawn = false;
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
