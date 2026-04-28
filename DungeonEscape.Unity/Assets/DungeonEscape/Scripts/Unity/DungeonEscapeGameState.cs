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
        private const float DefaultAutoSaveIntervalSeconds = 5f;
        private static readonly System.Random Random = new System.Random();

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
        private bool saveDirty;
        private float autoSaveCountdown;

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

            UpdateAutoSave();
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
            MarkDirty();
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

            MarkDirty();
        }

        public void SetCurrentDirection(Direction direction)
        {
            EnsureInitialized();
            Party.CurrentDirection = direction;
            MarkDirty();
        }

        public void IncrementStepCount()
        {
            EnsureInitialized();
            Party.StepCount++;
            MarkDirty();
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
            MarkDirty();
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
                MarkDirty();
            }

            if (nextStage.HasValue)
            {
                activeQuest.CurrentStage = nextStage.Value;
                MarkDirty();
            }

            var activeStage = activeQuest.Stages.FirstOrDefault(item => item.Number == activeQuest.CurrentStage);
            if (activeStage != null)
            {
                activeStage.Completed = true;
                MarkDirty();
            }

            var currentStage = quest.Stages == null
                ? null
                : quest.Stages.FirstOrDefault(item => item.Number == activeQuest.CurrentStage);

            if (currentStage == null || !currentStage.CompleteQuest || activeQuest.Completed)
            {
                return "";
            }

            activeQuest.Completed = true;
            MarkDirty();
            var message = new StringBuilder();
            message.AppendLine("You have completed the quest " + quest.Name);

            if (quest.Gold != 0)
            {
                Party.Gold += quest.Gold;
                MarkDirty();
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

            MarkDirty();
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

            MarkDirty();
            return member.Name + " gave " + item.Name + " to " + recipientName + ".";
        }

        public bool IsObjectOpen(string mapId, int objectId)
        {
            EnsureInitialized();
            var objectState = GetObjectState(mapId, objectId, false);
            return objectState != null && objectState.IsOpen == true;
        }

        public bool TryGetObjectPosition(string mapId, int objectId, out WorldPosition position)
        {
            EnsureInitialized();
            position = WorldPosition.Zero;
            var objectState = GetObjectState(mapId, objectId, false);
            if (objectState == null || !objectState.Position.HasValue)
            {
                return false;
            }

            position = objectState.Position.Value;
            return true;
        }

        public Direction? GetObjectDirection(string mapId, int objectId)
        {
            EnsureInitialized();
            var objectState = GetObjectState(mapId, objectId, false);
            return objectState == null ? null : objectState.Direction;
        }

        public void SetObjectPosition(string mapId, int objectId, WorldPosition position, Direction direction)
        {
            EnsureInitialized();
            var objectState = GetObjectState(mapId, objectId, true);
            objectState.Position = position;
            objectState.Direction = direction;
            MarkDirty();
        }

        public void InitializeMapObjects(string mapId, TiledMapInfo mapInfo)
        {
            EnsureInitialized();
            if (mapInfo == null || mapInfo.ObjectGroups == null)
            {
                return;
            }

            foreach (var group in mapInfo.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (!IsPickupObject(mapObject))
                    {
                        continue;
                    }

                    var objectState = GetObjectState(mapId, mapObject.Id, true);
                    if (objectState.Items != null)
                    {
                        continue;
                    }

                    objectState.Name = mapObject.Name;
                    objectState.Type = GetSpriteType(mapObject);
                    objectState.Items = CreateMapObjectItems(mapObject);
                    objectState.IsOpen = objectState.Items.Count == 0 ? true : objectState.IsOpen;
                    MarkDirty();
                }
            }
        }

        public string PickupMapObject(TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            if (mapObject == null)
            {
                return "";
            }

            var objectState = GetObjectState(Party.CurrentMapId, mapObject.Id, true);
            if (objectState.IsOpen == true)
            {
                return "You found nothing.";
            }

            var message = new StringBuilder();
            if (objectState.Items != null)
            {
                foreach (var item in objectState.Items.ToList())
                {
                    if (item == null)
                    {
                        objectState.Items.Remove(item);
                        continue;
                    }

                    if (item.Type == ItemType.Gold)
                    {
                        Party.Gold += item.Cost;
                        message.AppendLine("You found " + item.Cost + " gold.");
                        objectState.Items.Remove(item);
                        MarkDirty();
                        continue;
                    }

                    var member = Party.AddItem(new ItemInstance(item));
                    if (member == null)
                    {
                        message.AppendLine("You found " + item.Name + " but your party did not have enough room.");
                    }
                    else
                    {
                        message.AppendLine(member.Name + " found a " + item.Name + ".");
                        message.Append(CheckQuest(item));
                        objectState.Items.Remove(item);
                        MarkDirty();
                    }
                }
            }

            if (message.Length == 0)
            {
                objectState.IsOpen = true;
                MarkDirty();
                return "You found nothing.";
            }

            objectState.IsOpen = true;
            MarkDirty();
            return message.ToString().TrimEnd();
        }

        private List<Item> CreateMapObjectItems(TiledObjectInfo mapObject)
        {
            var items = new List<Item>();
            string itemId;
            if (mapObject.Properties != null &&
                mapObject.Properties.TryGetValue("ItemId", out itemId) &&
                !string.IsNullOrEmpty(itemId))
            {
                Item item;
                if (DungeonEscapeGameDataCache.Current != null &&
                    DungeonEscapeGameDataCache.Current.TryGetCustomItem(itemId, out item) &&
                    item != null)
                {
                    items.Add(item);
                }

                return items;
            }

            string goldText;
            int gold;
            if (mapObject.Properties != null &&
                mapObject.Properties.TryGetValue("Gold", out goldText) &&
                int.TryParse(goldText, out gold) &&
                gold > 0)
            {
                items.Add(CreateGold(gold));
                return items;
            }

            var level = GetIntProperty(mapObject, mapObject.Class == "Chest" ? "ChestLevel" : "Level");
            var chestItem = CreateChestItem(level == 0 ? GetPartyMaxLevel() : level);
            if (chestItem != null)
            {
                items.Add(chestItem);
            }

            return items;
        }

        public Item CreateChestItem(int level, Rarity? rarity = null)
        {
            if (Chance(0.25f))
            {
                return CreateRandomItem(level, 1, rarity);
            }

            return CreateGold(Dice.Roll(5, Math.Max(1, level) * 3, 1));
        }

        public Item CreateRandomItem(int maxLevel, int minLevel = 1, Rarity? rarity = null)
        {
            maxLevel = Math.Max(maxLevel, 1);
            minLevel = Math.Max(minLevel, 1);

            if (Chance(0.50f))
            {
                var staticItems = DungeonEscapeGameDataCache.Current == null
                    ? new List<Item>()
                    : DungeonEscapeGameDataCache.Current.CustomItems
                        .Where(item => item != null &&
                                       (item.Type == ItemType.OneUse || item.Type == ItemType.RepeatableUse) &&
                                       !item.IsKey &&
                                       item.MinLevel < maxLevel)
                        .ToList();

                if (staticItems.Count > 0)
                {
                    return staticItems[Random.Next(staticItems.Count)];
                }
            }

            return CreateRandomEquipment(maxLevel, minLevel, rarity);
        }

        public Item CreateRandomEquipment(
            int maxLevel,
            int minLevel = 1,
            Rarity? rarity = null,
            ItemType? type = null,
            Class? itemClass = null,
            Slot? slot = null)
        {
            maxLevel = Math.Max(maxLevel, 1);
            minLevel = Math.Max(Math.Min(minLevel, maxLevel), 1);

            var itemRarity = Random.Next(100);
            if (!rarity.HasValue)
            {
                rarity = itemRarity > 75
                    ? itemRarity > 90 ? itemRarity > 98 ? Rarity.Epic : Rarity.Rare : Rarity.Uncommon
                    : Rarity.Common;
            }

            if (!type.HasValue)
            {
                var types = Item.EquippableItems;
                type = types[Random.Next(types.Count)];
            }

            var item = new Item
            {
                Rarity = rarity.Value,
                Type = type.Value,
                ImageId = 202,
                Id = Guid.NewGuid().ToString()
            };

            var availableItemDefinitions = GetAvailableItemDefinitions(type.Value, itemClass, slot).ToArray();
            if (availableItemDefinitions.Length == 0)
            {
                return null;
            }

            var itemDefinition = availableItemDefinitions[Random.Next(availableItemDefinitions.Length)];
            List<StatType> availableStats;
            switch (type.Value)
            {
                case ItemType.Weapon:
                    availableStats = new List<StatType> { StatType.Agility, StatType.Attack, StatType.Health, StatType.Magic };
                    item.MinLevel = RandomLevel(maxLevel, minLevel);
                    item.Stats.Add(new StatValue
                    {
                        Type = StatType.Attack,
                        Value = Math.Max(item.MinLevel - 5 + Random.Next(6) + itemDefinition.BaseStat, Math.Max(itemDefinition.BaseStat, 1))
                    });
                    break;
                case ItemType.Armor:
                    availableStats = new List<StatType>
                    {
                        StatType.Agility,
                        StatType.Defence,
                        StatType.Health,
                        StatType.Magic,
                        StatType.MagicDefence
                    };
                    item.MinLevel = RandomLevel(maxLevel, minLevel);
                    item.Stats.Add(new StatValue
                    {
                        Type = StatType.Defence,
                        Value = Math.Max(item.MinLevel - 5 + Random.Next(6) + itemDefinition.BaseStat, Math.Max(itemDefinition.BaseStat, 1))
                    });
                    break;
                default:
                    return null;
            }

            if (itemDefinition.Names == null || itemDefinition.Names.Count == 0)
            {
                return null;
            }

            var baseStatLevel = Math.Min((int)(item.MinLevel / 25.0f * itemDefinition.Names.Count), itemDefinition.Names.Count - 1);
            var baseName = itemDefinition.Names[baseStatLevel];
            if (itemDefinition.Classes != null)
            {
                item.Classes = itemDefinition.Classes.ToList();
            }

            item.ImageId = baseName.ImageId;
            item.Slots = itemDefinition.Slots;

            var prefix = string.Empty;
            var suffix = string.Empty;
            var statCount = Math.Min((int)rarity.Value, availableStats.Count);
            var chosenStats = new List<StatType>();
            for (var i = 0; i < statCount; i++)
            {
                var index = Random.Next(availableStats.Count);
                var stat = availableStats[index];
                availableStats.Remove(stat);
                chosenStats.Add(stat);
            }

            foreach (var stat in chosenStats.OrderBy(value => (int)value))
            {
                var itemLevel = RandomLevel(maxLevel, minLevel);
                item.MinLevel = Math.Max(itemLevel, item.MinLevel);
                var statValue = Math.Max(itemLevel - 5 + Random.Next(6), 1);
                item.Stats.Add(new StatValue
                {
                    Type = stat,
                    Value = statValue
                });

                ApplyStatName(stat, itemLevel, ref prefix, ref suffix);
            }

            item.Name = BuildEquipmentName(baseName.Name, prefix, suffix);
            item.Cost = item.MinLevel * ((int)rarity.Value + 1) * 100;
            if (item.Cost > 0)
            {
                item.Cost += Random.Next(Math.Max(1, item.Cost / 3));
            }

            if (DungeonEscapeGameDataCache.Current != null)
            {
                item.Setup(DungeonEscapeGameDataCache.Current.Skills);
            }

            return item;
        }

        private int GetPartyMaxLevel()
        {
            return Party.ActiveMembers.Any() ? Party.ActiveMembers.Max(member => member.Level) : 1;
        }

        private static Item CreateGold(int gold)
        {
            return new Item
            {
                Id = "Gold",
                Name = "Gold",
                Cost = gold,
                MinLevel = 0,
                Type = ItemType.Gold
            };
        }

        private static bool Chance(float probability)
        {
            return Random.NextDouble() < probability;
        }

        private static int RandomLevel(int maxLevel, int minLevel)
        {
            maxLevel = Math.Max(maxLevel, 1);
            minLevel = Math.Max(Math.Min(minLevel, maxLevel), 1);
            return maxLevel <= minLevel ? minLevel : Random.Next(maxLevel - minLevel) + minLevel;
        }

        private static IEnumerable<ItemDefinition> GetAvailableItemDefinitions(ItemType type, Class? itemClass, Slot? slot)
        {
            if (DungeonEscapeGameDataCache.Current == null || DungeonEscapeGameDataCache.Current.ItemDefinitions == null)
            {
                return new List<ItemDefinition>();
            }

            return DungeonEscapeGameDataCache.Current.ItemDefinitions.Where(definition =>
            {
                if (definition == null)
                {
                    return false;
                }

                if (itemClass.HasValue && slot.HasValue)
                {
                    return definition.Slots != null &&
                           definition.Classes != null &&
                           definition.Type == type &&
                           definition.Classes.Contains(itemClass.Value) &&
                           definition.Slots.Contains(slot.Value);
                }

                if (itemClass.HasValue)
                {
                    return definition.Classes != null &&
                           definition.Type == type &&
                           definition.Classes.Contains(itemClass.Value);
                }

                if (slot.HasValue)
                {
                    return definition.Slots != null &&
                           definition.Type == type &&
                           definition.Slots.Contains(slot.Value);
                }

                return definition.Type == type;
            });
        }

        private static void ApplyStatName(StatType stat, int itemLevel, ref string prefix, ref string suffix)
        {
            if (DungeonEscapeGameDataCache.Current == null || DungeonEscapeGameDataCache.Current.StatNames == null)
            {
                return;
            }

            var statName = DungeonEscapeGameDataCache.Current.StatNames.FirstOrDefault(item => item.Type == stat);
            if (statName == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(suffix) && statName.Suffix != null && statName.Suffix.Count > 0)
            {
                var statLevel = Math.Min((int)(itemLevel / 25.0f * statName.Suffix.Count), statName.Suffix.Count - 1);
                suffix = statName.Suffix[statLevel];
            }
            else if (statName.Prefix != null && statName.Prefix.Count > 0)
            {
                var statLevel = Math.Min((int)(itemLevel / 25.0f * statName.Prefix.Count), statName.Prefix.Count - 1);
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = statName.Prefix[statLevel];
                }
                else
                {
                    prefix += " " + statName.Prefix[statLevel];
                }
            }
        }

        private static string BuildEquipmentName(string baseName, string prefix, string suffix)
        {
            var name = baseName;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                name = prefix + " " + name;
            }

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                name = name + " of " + suffix;
            }

            return name;
        }

        private static int GetIntProperty(TiledObjectInfo mapObject, string propertyName)
        {
            string value;
            int result;
            return mapObject.Properties != null &&
                   mapObject.Properties.TryGetValue(propertyName, out value) &&
                   int.TryParse(value, out result)
                ? result
                : 0;
        }

        private static bool IsPickupObject(TiledObjectInfo mapObject)
        {
            return string.Equals(mapObject.Class, "Chest", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mapObject.Class, "HiddenItem", StringComparison.OrdinalIgnoreCase);
        }

        private static SpriteType GetSpriteType(TiledObjectInfo mapObject)
        {
            return string.Equals(mapObject.Class, "HiddenItem", StringComparison.OrdinalIgnoreCase)
                ? SpriteType.HiddenItem
                : SpriteType.Chest;
        }

        private string CheckQuest(Item item)
        {
            return item == null || string.IsNullOrEmpty(item.QuestId)
                ? ""
                : AdvanceQuest(item.QuestId, item.NextStage);
        }

        private ObjectState GetObjectState(string mapId, int objectId, bool create)
        {
            if (CurrentSave.MapStates == null)
            {
                CurrentSave.MapStates = new List<MapState>();
            }

            var normalizedMapId = TiledMapLoader.NormalizeMapId(mapId);
            var mapState = CurrentSave.MapStates.FirstOrDefault(item => item.Id == normalizedMapId);
            if (mapState == null)
            {
                if (!create)
                {
                    return null;
                }

                mapState = new MapState { Id = normalizedMapId };
                CurrentSave.MapStates.Add(mapState);
            }

            var objectState = mapState.Objects.FirstOrDefault(item => item.Id == objectId);
            if (objectState == null && create)
            {
                objectState = new ObjectState { Id = objectId };
                mapState.Objects.Add(objectState);
            }

            return objectState;
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
            SaveQuick(false);
        }

        private void SaveQuick(bool autoSave)
        {
            EnsureInitialized();
            CurrentSave.IsQuick = true;
            CurrentSave.Time = DateTime.Now;
            UpsertQuickSave(CurrentSave);
            SaveGameFile();
            saveDirty = false;
            autoSaveCountdown = 0f;
            Debug.Log((autoSave ? "Auto saved to " : "Quick saved to ") + GetSaveFilePath());
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
            saveDirty = false;
            autoSaveCountdown = 0f;
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
            MarkDirty();
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
            party.Members.Add(CreateStarterHero(party.PlayerName));

            return new GameSave
            {
                Party = party,
                IsQuick = true,
                Time = DateTime.Now
            };
        }

        private Hero CreateStarterHero(string playerName)
        {
            var hero = new Hero
            {
                Name = string.IsNullOrEmpty(playerName) ? "Player" : playerName,
                Class = Class.Hero,
                Gender = Gender.Male,
                IsActive = true,
                Order = 0,
                Level = 1,
                Xp = 0
            };

            ApplyStartingClassStats(hero);
            AddStartingEquipment(hero);
            return hero;
        }

        private static void ApplyStartingClassStats(Hero hero)
        {
            var classStats = DungeonEscapeGameDataCache.Current == null ||
                             DungeonEscapeGameDataCache.Current.ClassLevels == null
                ? null
                : DungeonEscapeGameDataCache.Current.ClassLevels.FirstOrDefault(item => item.Class == hero.Class);

            if (classStats == null || classStats.Stats == null)
            {
                ApplyFallbackStartingStats(hero);
                return;
            }

            hero.NextLevel = classStats.FirstLevel;
            hero.MaxHealth = RollStartingStat(classStats, StatType.Health, 30);
            hero.Health = hero.MaxHealth;
            hero.MaxMagic = RollStartingStat(classStats, StatType.Magic, 8);
            hero.Magic = hero.MaxMagic;
            hero.Attack = RollStartingStat(classStats, StatType.Attack, 8);
            hero.Defence = RollStartingStat(classStats, StatType.Defence, 6);
            hero.MagicDefence = RollStartingStat(classStats, StatType.MagicDefence, 4);
            hero.Agility = RollStartingStat(classStats, StatType.Agility, 6);
            hero.Skills = classStats.Skills == null ? new List<string>() : classStats.Skills.ToList();
        }

        private static int RollStartingStat(ClassStats classStats, StatType type, int fallbackValue)
        {
            var stat = classStats.Stats.FirstOrDefault(item => item.Type == type);
            return stat == null ? fallbackValue : stat.RollStartValue();
        }

        private static void ApplyFallbackStartingStats(Hero hero)
        {
            hero.NextLevel = 100;
            hero.MaxHealth = 30;
            hero.Health = 30;
            hero.MaxMagic = 8;
            hero.Magic = 8;
            hero.Attack = 8;
            hero.Defence = 6;
            hero.MagicDefence = 4;
            hero.Agility = 6;
            hero.Skills = new List<string>();
        }

        private void AddStartingEquipment(Hero hero)
        {
            EquipStartingItem(hero, CreateRandomEquipment(hero.Level, 1, Rarity.Common, ItemType.Armor, hero.Class, Slot.Chest));
            EquipStartingItem(hero, CreateRandomEquipment(hero.Level, 1, Rarity.Common, ItemType.Weapon, hero.Class));
        }

        private static void EquipStartingItem(Hero hero, Item item)
        {
            if (item == null || item.Slots == null || item.Slots.Count == 0)
            {
                return;
            }

            var instance = new ItemInstance(item);
            hero.Items.Add(instance);
            hero.Equip(instance);
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

        private void MarkDirty()
        {
            saveDirty = true;
            if (autoSaveCountdown <= 0f)
            {
                autoSaveCountdown = GetAutoSaveIntervalSeconds();
            }
        }

        private void UpdateAutoSave()
        {
            if (!saveDirty || !IsAutoSaveEnabled())
            {
                return;
            }

            autoSaveCountdown -= Time.deltaTime;
            if (autoSaveCountdown > 0f)
            {
                return;
            }

            SaveQuick(true);
        }

        private static bool IsAutoSaveEnabled()
        {
            return DungeonEscapeSettingsCache.Current == null ||
                   DungeonEscapeSettingsCache.Current.AutoSaveEnabled;
        }

        private static float GetAutoSaveIntervalSeconds()
        {
            var interval = DungeonEscapeSettingsCache.Current == null
                ? DefaultAutoSaveIntervalSeconds
                : DungeonEscapeSettingsCache.Current.AutoSaveIntervalSeconds;
            return interval <= 0f ? DefaultAutoSaveIntervalSeconds : interval;
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
