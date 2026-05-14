using Redpoint.DungeonEscape.Data;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Tools;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Core
{
    public sealed class GameState : MonoBehaviour, IGame
    {
        private const string SaveFileVersion = "1.0";
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
        public static bool AutoSaveBlocked { get; set; }
        public event Action<GameSave> SaveLoaded;
        private bool saveDirty;
        private float autoSaveCountdown;
        private NameGenerator nameGenerator;
        private readonly Dictionary<string, List<RandomMonster>> randomMonsterCache = new Dictionary<string, List<RandomMonster>>(StringComparer.OrdinalIgnoreCase);

        public Party Party
        {
            get { return CurrentSave == null ? null : CurrentSave.Party; }
        }

        public List<ClassStats> ClassLevelStats
        {
            get
            {
                return GameDataCache.Current == null || GameDataCache.Current.ClassLevels == null
                    ? new List<ClassStats>()
                    : GameDataCache.Current.ClassLevels.ToList();
            }
        }

        public ISounds Sounds
        {
            get { return Audio.GetOrCreate(); }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (TitleMenu.IsOpen)
            {
                return;
            }

            if (InputManager.GetCommandDown(InputCommand.QuickSave))
            {
                SaveQuick();
            }
            else if (InputManager.GetCommandDown(InputCommand.QuickLoad))
            {
                LoadQuick();
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
            Party.CurrentMapId = Loader.NormalizeMapId(mapId);
            Party.CurrentMapIsOverWorld = Party.CurrentMapId == "overworld";
            MarkDirty();
        }

        public void SetMap(string mapId = null, string spawnId = null, WorldPosition? point = null)
        {
            EnsureInitialized();

            var normalizedMapId = Loader.NormalizeMapId(string.IsNullOrEmpty(mapId) ? "overworld" : mapId);
            SetCurrentMap(normalizedMapId);

            var mapView = FindAnyObjectByType<View>();
            if (mapView != null)
            {
                mapView.LoadMap(normalizedMapId, spawnId, !point.HasValue);
            }

            if (point.HasValue)
            {
                SetCurrentPosition(point.Value);
                if (mapView != null)
                {
                    mapView.CenterOn(point.Value);
                }

                return;
            }

            if (mapView != null)
            {
                WorldPosition spawnPosition;
                if (mapView.TryGetSpawnPosition(spawnId, out spawnPosition))
                {
                    SetCurrentPosition(spawnPosition);
                }
            }
        }

        public Transition CreateWarpTransition(Warp warp)
        {
            EnsureInitialized();

            var mapId = Loader.NormalizeMapId(warp.MapId);
            SetCurrentMap(mapId);

            return new Transition
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

        public IReadOnlyList<VisitedLocation> GetReturnLocations()
        {
            EnsureInitialized();
            EnsureVisitedLocations();
            return Party.VisitedLocations
                .Where(location => location != null && !string.IsNullOrEmpty(location.MapId))
                .OrderBy(location => location.DisplayName)
                .ThenBy(location => location.MapId)
                .ToList();
        }

        public bool CanCastOutside()
        {
            EnsureInitialized();
            return Party != null && !Party.CurrentMapIsOverWorld;
        }

        public bool CanCastReturn()
        {
            EnsureInitialized();
            return Party != null && Party.CurrentMapIsOverWorld && GetReturnLocations().Count > 0;
        }

        public void RecordVisitedLocation(string mapId, WorldPosition position)
        {
            EnsureInitialized();
            var normalizedMapId = Loader.NormalizeMapId(mapId);
            if (string.IsNullOrEmpty(normalizedMapId) ||
                string.Equals(normalizedMapId, "overworld", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            EnsureVisitedLocations();
            var existing = Party.VisitedLocations.FirstOrDefault(location =>
                string.Equals(location.MapId, normalizedMapId, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                Party.VisitedLocations.Add(new VisitedLocation
                {
                    MapId = normalizedMapId,
                    Position = position,
                    DisplayName = GameSaveFormatter.FormatLocationName(normalizedMapId)
                });
            }
            else
            {
                existing.Position = position;
                if (string.IsNullOrEmpty(existing.DisplayName))
                {
                    existing.DisplayName = GameSaveFormatter.FormatLocationName(normalizedMapId);
                }
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

        public void SetCurrentBiome(Biome biome)
        {
            EnsureInitialized();
            if (Party.CurrentBiome == biome)
            {
                return;
            }

            Party.CurrentBiome = biome;
            MarkDirty();
        }

        public string ApplyMapStepEffects(int damage, Biome biome)
        {
            EnsureInitialized();
            SetCurrentBiome(biome);
            if (Party.ActiveMembers == null)
            {
                return "";
            }

            var message = new StringBuilder();
            var appliedDamage = false;
            var statusChanged = false;
            foreach (var hero in Party.ActiveMembers.Where(member => member != null && !member.IsDead))
            {
                var statusMessage = hero.CheckForExpiredStates(Party.StepCount, DurationType.Distance);
                statusMessage += FilterPassiveMapStepStatusMessages(hero, hero.UpdateStatusEffects(this));
                if (!string.IsNullOrEmpty(statusMessage))
                {
                    message.Append(statusMessage);
                    statusChanged = true;
                }

                if (damage > 0)
                {
                    hero.Health = Math.Max(0, hero.Health - damage);
                    appliedDamage = true;
                }

                if (hero.IsDead)
                {
                    message.AppendLine(hero.Name + " has died.");
                }
            }

            if (appliedDamage)
            {
                Sounds.PlaySoundEffect("receive-damage");
                MarkDirty();
            }
            else if (statusChanged)
            {
                MarkDirty();
            }

            return message.ToString().TrimEnd();
        }

        private static string FilterPassiveMapStepStatusMessages(Hero hero, string message)
        {
            if (hero == null || string.IsNullOrEmpty(message))
            {
                return message;
            }

            var filtered = new StringBuilder();
            var sleepMessage = hero.Name + " is asleep";
            var confusionMessage = hero.Name + " is confused";
            var damageMessagePrefix = hero.Name + " took ";
            const string damageMessageSuffix = " points of damage";
            var lines = message.Replace("\r\n", "\n").Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) ||
                    string.Equals(line, sleepMessage, StringComparison.Ordinal) ||
                    string.Equals(line, confusionMessage, StringComparison.Ordinal) ||
                    (line.StartsWith(damageMessagePrefix, StringComparison.Ordinal) &&
                     line.EndsWith(damageMessageSuffix, StringComparison.Ordinal)))
                {
                    continue;
                }

                filtered.AppendLine(line);
            }

            return filtered.ToString();
        }

        public void TryLogRandomEncounter(string mapId, BiomeInfo biomeInfo)
        {
            EnsureInitialized();
            if (Party == null ||
                Party.AliveMembers == null ||
                SettingsCache.Current.NoMonsters ||
                biomeInfo == null)
            {
                return;
            }

            var randomMonsters = GetRandomMonstersForMap(mapId);
            if (!EncounterRules.CanRollRandomEncounter(
                    Party,
                    biomeInfo,
                    randomMonsters,
                    SettingsCache.Current.NoMonsters,
                    () => Random.NextDouble()))
            {
                return;
            }

            var monsters = BuildRandomEncounter(randomMonsters, biomeInfo);
            if (monsters.Count == 0)
            {
                return;
            }

            CombatWindow.Open(monsters, biomeInfo.Type);
        }

        public string ApplyCombatRewards(IEnumerable<MonsterInstance> monsters)
        {
            EnsureInitialized();
            var defeatedMonsters = monsters == null
                ? new List<MonsterInstance>()
                : monsters.Where(monster => monster != null && monster.IsDead).ToList();
            var aliveMembers = Party == null
                ? new List<Hero>()
                : Party.AliveMembers.Where(member => member != null && !member.IsDead).ToList();
            if (defeatedMonsters.Count == 0 || aliveMembers.Count == 0)
            {
                return "";
            }

            var message = new StringBuilder();
            var monsterName = defeatedMonsters.Count == 1 ? "the " + defeatedMonsters[0].Name : "all the enemies";
            var xp = defeatedMonsters.Sum(monster => (int)monster.Xp) / aliveMembers.Count;
            if (xp == 0)
            {
                xp = 1;
            }

            var gold = defeatedMonsters.Sum(monster => monster.Gold);
            Party.Gold += gold;
            message.AppendLine("You have defeated " + monsterName + ".");
            message.AppendLine("Each party member has gained " + xp + " XP.");
            message.AppendLine("The party got " + gold + " gold.");

            var foundItems = defeatedMonsters
                .SelectMany(monster => monster.Items == null
                    ? Enumerable.Empty<Item>()
                    : monster.Items.Select(item => item.Item))
                .Where(item => item != null)
                .ToList();

            if (Dice.RollD20() > 18)
            {
                foundItems.Add(CreateChestItem(GetAverageActivePartyLevel(), defeatedMonsters.Max(monster => monster.Rarity)));
            }

            AppendCombatItems(message, foundItems);

            foreach (var member in aliveMembers)
            {
                member.Xp += (ulong)xp;
                AppendLevelUpMessages(message, member);
            }

            MarkDirty();
            Sounds.PlaySoundEffect("victory");
            return message.ToString().TrimEnd();
        }

        public string StartQuest(string questId)
        {
            EnsureInitialized();
            Quest quest;
            if (string.IsNullOrEmpty(questId) ||
                GameDataCache.Current == null ||
                !GameDataCache.Current.TryGetQuest(questId, out quest))
            {
                return "";
            }

            bool changed;
            var message = QuestRules.StartQuest(Party, quest, out changed);
            if (changed)
            {
                MarkDirty();
            }

            return message;
        }

        public string AdvanceQuest(string questId, int? nextStage)
        {
            EnsureInitialized();
            Quest quest;
            if (string.IsNullOrEmpty(questId) ||
                GameDataCache.Current == null ||
                !GameDataCache.Current.TryGetQuest(questId, out quest))
            {
                return "";
            }

            bool changed;
            var message = QuestRules.AdvanceQuest(
                Party,
                quest,
                nextStage,
                GameDataCache.Current == null ? null : GameDataCache.Current.ClassLevels,
                GameDataCache.Current == null ? null : GameDataCache.Current.Spells,
                GiveItem,
                out changed);
            if (changed)
            {
                MarkDirty();
            }

            return message;
        }

        private void AppendLevelUpMessages(StringBuilder message, Hero hero)
        {
            if (hero == null)
            {
                return;
            }

            var classLevels = GameDataCache.Current == null ? null : GameDataCache.Current.ClassLevels;
            if (classLevels == null)
            {
                return;
            }

            while (true)
            {
                string levelUpMessage;
                if (!hero.CheckLevelUp(
                        classLevels,
                        GameDataCache.Current == null ? null : GameDataCache.Current.Spells,
                        out levelUpMessage))
                {
                    return;
                }

                MarkDirty();
                Sounds.PlaySoundEffect("level-up");
                if (!string.IsNullOrEmpty(levelUpMessage))
                {
                    message.Append(levelUpMessage);
                }
            }
        }

        private void AppendCombatItems(StringBuilder message, IEnumerable<Item> foundItems)
        {
            if (foundItems == null)
            {
                return;
            }

            var foundItemMessage = new StringBuilder();
            var questMessage = new StringBuilder();
            foreach (var foundItem in foundItems.Where(item => item != null))
            {
                if (foundItem.Type == ItemType.Gold)
                {
                    Party.Gold += foundItem.Cost;
                    foundItemMessage.AppendLine("You found " + foundItem.Cost + " gold.");
                    continue;
                }

                var member = Party.AddItem(new ItemInstance(foundItem));
                if (member == null)
                {
                    foundItemMessage.AppendLine("You found " + foundItem.Name + " but your party did not have enough room.");
                    continue;
                }

                foundItemMessage.AppendLine(member.Name + " found a " + foundItem.Name + ".");
                questMessage.Append(CheckQuest(foundItem));
            }

            if (foundItemMessage.Length == 0)
            {
                return;
            }

            Sounds.PlaySoundEffect("treasure");
            message.AppendLine("You found a chest and opened it!");
            message.Append(foundItemMessage);
            message.Append(questMessage);
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
            if (string.Equals(itemId, "#Random#", StringComparison.OrdinalIgnoreCase))
            {
                return GiveGeneratedItem(CreateRandomItem(GetAverageActivePartyLevel()));
            }

            Item item;
            if (string.IsNullOrEmpty(itemId) ||
                GameDataCache.Current == null ||
                !GameDataCache.Current.TryGetCustomItem(itemId, out item))
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

        public bool HasItem(string itemId)
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(itemId) &&
                   Party != null &&
                   Party.GetItem(itemId) != null;
        }

        private string GiveGeneratedItem(Item item)
        {
            if (item == null)
            {
                return "Could not create a random item.\n";
            }

            var member = Party.AddItem(new ItemInstance(item));
            if (member == null)
            {
                return "No party member can carry " + item.Name + ".\n";
            }

            MarkDirty();
            return member.Name + " got " + item.Name + ".\n";
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

        public bool IsMapObjectRemoved(string mapId, TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            return IsHiddenItemObject(mapObject) && IsObjectOpen(mapId, mapObject.Id);
        }

        public bool IsMapObjectRemoved(string mapId, int objectId, string objectClass)
        {
            EnsureInitialized();
            return IsHiddenItemClass(objectClass) && IsObjectOpen(mapId, objectId);
        }

        public bool CanPickupMapObject(TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            if (mapObject == null)
            {
                return false;
            }

            if (IsMapObjectRemoved(Party.CurrentMapId, mapObject))
            {
                return false;
            }

            return IsPickupQuestConditionMet(GetMapObjectItemId(mapObject)) &&
                   CanOpenMapPickupObject(mapObject);
        }

        public bool CanPickupMapObject(string mapId, int objectId, string objectClass, string itemId)
        {
            EnsureInitialized();
            if (IsMapObjectRemoved(mapId, objectId, objectClass))
            {
                return false;
            }

            return IsPickupQuestConditionMet(itemId);
        }

        public string OpenDoor(int objectId)
        {
            EnsureInitialized();
            if (objectId <= 0 || string.IsNullOrEmpty(Party.CurrentMapId))
            {
                return "";
            }

            var objectState = GetObjectState(Party.CurrentMapId, objectId, true);
            if (objectState.IsOpen == true)
            {
                return "The door is already open.";
            }

            objectState.Type = SpriteType.Door;
            objectState.IsOpen = true;
            objectState.Collideable = false;
            MarkDirty();
            Sounds.PlaySoundEffect("door");
            return "The door opened.";
        }

        public string OpenDoor(TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            if (mapObject == null || string.IsNullOrEmpty(Party.CurrentMapId))
            {
                return "";
            }

            var objectState = GetObjectState(Party.CurrentMapId, mapObject.Id, true);
            if (objectState.IsOpen == true)
            {
                return "The door is already open.";
            }

            if (!IsLockedMapObject(mapObject))
            {
                objectState.Name = mapObject.Name;
                objectState.Type = SpriteType.Door;
                objectState.Level = GetIntProperty(mapObject, "DoorLevel");
                objectState.IsOpen = true;
                objectState.Collideable = false;
                MarkDirty();
                Sounds.PlaySoundEffect("door");
                return "The door opened.";
            }

            if (!CanOpenWithKey(mapObject))
            {
                return "The door is locked.";
            }

            var doorLevel = GetIntProperty(mapObject, "DoorLevel");
            ItemInstance key;
            Hero keyOwner;
            if (!TryFindDoorKey(mapObject, doorLevel, out key, out keyOwner))
            {
                return "You do not have a key for this door.";
            }

            objectState.Name = mapObject.Name;
            objectState.Type = SpriteType.Door;
            objectState.Level = doorLevel;
            objectState.IsOpen = true;
            objectState.Collideable = false;
            MarkDirty();
            Sounds.PlaySoundEffect("door");
            return keyOwner.Name + " used " + key.Name + ".\nThe door opened.";
        }

        public string OpenMapObject(TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            if (mapObject == null)
            {
                return "There is nothing there.";
            }

            if (IsDoorObject(mapObject))
            {
                return OpenDoor(mapObject);
            }

            if (IsChestObject(mapObject))
            {
                return OpenChest(mapObject);
            }

            return "This cannot be opened.";
        }

        public string OpenChest(TiledObjectInfo mapObject)
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

            if (!IsLockedMapObject(mapObject))
            {
                var result = PickupMapObject(mapObject);
                if (objectState.IsOpen == true)
                {
                    Sounds.PlaySoundEffect("treasure");
                }

                return result;
            }

            if (!CanOpenWithKey(mapObject))
            {
                return "The chest is locked.";
            }

            var chestLevel = GetMapPickupLevel(mapObject);
            ItemInstance key;
            Hero keyOwner;
            if (!TryFindMapObjectKey(mapObject, chestLevel, out key, out keyOwner))
            {
                return "You do not have a key for this chest.";
            }

            var pickupMessage = PickupMapObject(mapObject);
            if (objectState.IsOpen == true)
            {
                Sounds.PlaySoundEffect("treasure");
            }

            return keyOwner.Name + " used " + key.Name + ".\n" + pickupMessage;
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

        public bool IsObjectActive(string mapId, int objectId)
        {
            EnsureInitialized();
            var objectState = GetObjectState(mapId, objectId, false);
            return objectState == null || objectState.IsActive;
        }

        public void SetObjectPosition(string mapId, int objectId, WorldPosition position, Direction direction)
        {
            EnsureInitialized();
            var objectState = GetObjectState(mapId, objectId, true);
            objectState.Position = position;
            objectState.Direction = direction;
            MarkDirty();
        }

        public void SetObjectActive(string mapId, TiledObjectInfo mapObject, bool active)
        {
            EnsureInitialized();
            if (mapObject == null)
            {
                return;
            }

            var objectState = GetObjectState(mapId, mapObject.Id, true);
            objectState.Name = mapObject.Name;
            objectState.Type = GetSpriteType(mapObject);
            objectState.IsActive = active;
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

            if (!CanOpenMapPickupObject(mapObject))
            {
                return "You are not experienced enough to open this.";
            }

            if (!CanPickupMapObject(mapObject))
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

        public string RecruitPartyMember(TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            if (mapObject == null || !IsPartyMemberObject(mapObject))
            {
                return "";
            }

            var currentMapId = Party.CurrentMapId;
            if (!IsObjectActive(currentMapId, mapObject.Id))
            {
                return mapObject.Name + " has already joined.";
            }

            var gender = GetEnumProperty(mapObject, "Gender", Gender.Male);
            var memberClass = GetEnumProperty(mapObject, "Class", Class.Fighter);
            var level = Math.Max(1, GetIntProperty(mapObject, "Level", 1));
            var memberName = GetRecruitName(mapObject, gender);
            if (Party.Members.Any(member => string.Equals(member.Name, memberName, StringComparison.OrdinalIgnoreCase)))
            {
                memberName = GenerateUniqueName(gender);
            }

            var hero = CreateHero(memberName, memberClass, gender, level, true, null);
            ApplyMapObjectSprite(hero, currentMapId, mapObject);
            hero.IsActive = false;
            hero.Order = 0;

            Party.Members.Add(hero);
            SetObjectActive(currentMapId, mapObject, false);

            if (Party.ActiveMembers.Count() < GetMaxPartyMembers())
            {
                hero.IsActive = true;
                hero.Order = GetNextPartyOrder();
                MarkDirty();
                return hero.Name + " joined the party.";
            }

            MarkDirty();
            return hero.Name + " joined, but is waiting because the party is full.";
        }

        public bool ActivatePartyMember(Hero hero)
        {
            EnsureInitialized();
            if (hero == null || hero.IsActive || Party.ActiveMembers.Count() >= GetMaxPartyMembers())
            {
                return false;
            }

            hero.IsActive = true;
            hero.Order = GetNextPartyOrder();
            NormalizePartyOrder();
            MarkDirty();
            return true;
        }

        public bool DeactivatePartyMember(Hero hero)
        {
            EnsureInitialized();
            if (hero == null || !hero.IsActive || Party.ActiveMembers.Count() <= 1)
            {
                return false;
            }

            hero.IsActive = false;
            hero.Order = 0;
            NormalizePartyOrder();
            MarkDirty();
            return true;
        }

        public bool MovePartyMemberUp(Hero hero)
        {
            EnsureInitialized();
            if (hero == null || !hero.IsActive)
            {
                return false;
            }

            var activeMembers = Party.ActiveMembers.ToList();
            var index = activeMembers.IndexOf(hero);
            if (index <= 0)
            {
                return false;
            }

            SwapPartyOrder(hero, activeMembers[index - 1]);
            NormalizePartyOrder();
            MarkDirty();
            return true;
        }

        public bool MovePartyMemberDown(Hero hero)
        {
            EnsureInitialized();
            if (hero == null || !hero.IsActive)
            {
                return false;
            }

            var activeMembers = Party.ActiveMembers.ToList();
            var index = activeMembers.IndexOf(hero);
            if (index < 0 || index >= activeMembers.Count - 1)
            {
                return false;
            }

            SwapPartyOrder(hero, activeMembers[index + 1]);
            NormalizePartyOrder();
            MarkDirty();
            return true;
        }

        public bool EquipHeroItem(Hero hero, ItemInstance item)
        {
            EnsureInitialized();
            if (hero == null ||
                item == null ||
                item.Slots == null ||
                item.Slots.Count == 0 ||
                !Party.Members.Contains(hero) ||
                !hero.CanEquipItem(item))
            {
                return false;
            }

            foreach (var slot in item.Slots)
            {
                string equippedItemId;
                if (!hero.Slots.TryGetValue(slot, out equippedItemId) || string.IsNullOrEmpty(equippedItemId))
                {
                    continue;
                }

                var equippedItem = hero.Items.FirstOrDefault(candidate => candidate.Id == equippedItemId);
                if (equippedItem != null)
                {
                    hero.UnEquip(equippedItem);
                }
            }

            item.UnEquip(Party.Members);
            hero.Equip(item);
            MarkDirty();
            return true;
        }

        public bool UnequipHeroItem(Hero hero, ItemInstance item)
        {
            EnsureInitialized();
            if (hero == null || item == null || !Party.Members.Contains(hero) || !item.IsEquipped)
            {
                return false;
            }

            hero.UnEquip(item);
            MarkDirty();
            return true;
        }

        public bool TransferHeroItem(Hero source, Hero target, ItemInstance item)
        {
            EnsureInitialized();
            if (source == null ||
                target == null ||
                item == null ||
                ReferenceEquals(source, target) ||
                !Party.Members.Contains(source) ||
                !Party.Members.Contains(target) ||
                !source.Items.Contains(item) ||
                target.Items.Count >= Party.MaxItems)
            {
                return false;
            }

            item.UnEquip(Party.Members);
            source.Items.Remove(item);
            target.Items.Add(item);
            MarkDirty();
            return true;
        }

        public bool DropHeroItem(Hero hero, ItemInstance item)
        {
            EnsureInitialized();
            if (hero == null ||
                item == null ||
                item.Type == ItemType.Quest ||
                !Party.Members.Contains(hero) ||
                !hero.Items.Contains(item))
            {
                return false;
            }

            item.UnEquip(Party.Members);
            hero.Items.Remove(item);
            MarkDirty();
            return true;
        }

        public string UseHeroItem(Hero source, ItemInstance item, Hero target)
        {
            EnsureInitialized();
            if (!CanUseHeroItem(source, item) || target == null || !Party.Members.Contains(target))
            {
                return "Cannot use item.";
            }

            EnsureItemLinked(item);
            var result = item.Use(source, target, null, this, 0);
            if (result.Item2)
            {
                ConsumeUsedItem(source, item);
                MarkDirty();
            }

            return string.IsNullOrEmpty(result.Item1) ? item.Name + " was used." : result.Item1;
        }

        public string UseHeroItemOnParty(Hero source, ItemInstance item)
        {
            EnsureInitialized();
            if (!CanUseHeroItem(source, item))
            {
                return "Cannot use item.";
            }

            EnsureItemLinked(item);
            var targets = Party.ActiveMembers.ToList();
            if (targets.Count == 0)
            {
                return "No party members can be targeted.";
            }

            var messages = new List<string>();
            var anySuccess = false;
            foreach (var target in targets)
            {
                var result = item.Use(source, target, null, this, 0, true);
                if (!string.IsNullOrEmpty(result.Item1))
                {
                    messages.Add(result.Item1);
                }

                anySuccess = anySuccess || result.Item2;
            }

            if (anySuccess)
            {
                if (item.MaxCharges != 0)
                {
                    item.Charges--;
                }

                ConsumeUsedItem(source, item);
                MarkDirty();
            }

            return messages.Count == 0 ? item.Name + " was used." : string.Join("\n", messages.ToArray());
        }

        public bool CanCastHeroSpell(Hero caster, Spell spell)
        {
            EnsureSpellLinked(spell);
            return caster != null &&
                   spell != null &&
                   Party != null &&
                   GameDataCache.Current != null &&
                   GameDataCache.Current.Spells != null &&
                   Party.Members.Contains(caster) &&
                   !caster.IsDead &&
                   spell.IsNonEncounterSpell &&
                   caster.GetSpells(GameDataCache.Current.Spells).Contains(spell);
        }

        public string CastHeroSpell(Hero caster, Spell spell, Hero target)
        {
            EnsureInitialized();
            if (!CanCastHeroSpell(caster, spell) || !IsValidHeroSpellTarget(spell, target))
            {
                return "Cannot cast spell.";
            }

            var message = spell.Cast(new[] { target }, new BaseState[0], caster, this);
            MarkDirty();
            return string.IsNullOrEmpty(message) ? caster.Name + " casts " + spell.Name + "." : message.TrimEnd();
        }

        public string CastHeroSpellOnParty(Hero caster, Spell spell)
        {
            EnsureInitialized();
            if (!CanCastHeroSpell(caster, spell))
            {
                return "Cannot cast spell.";
            }

            var targets = GetValidHeroSpellTargets(spell).Cast<IFighter>().ToList();
            if (targets.Count == 0)
            {
                return "No party members can be targeted.";
            }

            var message = spell.Cast(targets, new BaseState[0], caster, this);
            MarkDirty();
            return string.IsNullOrEmpty(message) ? caster.Name + " casts " + spell.Name + "." : message.TrimEnd();
        }

        private IEnumerable<Hero> GetValidHeroSpellTargets(Spell spell)
        {
            EnsureSpellLinked(spell);
            return spell != null && spell.Type == SkillType.Revive
                ? Party.DeadMembers
                : Party.AliveMembers;
        }

        private bool IsValidHeroSpellTarget(Spell spell, Hero target)
        {
            return target != null &&
                   Party != null &&
                   Party.Members.Contains(target) &&
                   GetValidHeroSpellTargets(spell).Contains(target);
        }

        public string CastHeroSpellWithoutTarget(Hero caster, Spell spell)
        {
            EnsureInitialized();
            if (!CanCastHeroSpell(caster, spell))
            {
                return "Cannot cast spell.";
            }

            var message = spell.Cast(new IFighter[0], new BaseState[0], caster, this);
            MarkDirty();
            return string.IsNullOrEmpty(message) ? caster.Name + " casts " + spell.Name + "." : message.TrimEnd();
        }

        public string CastOutsideSpell(Hero caster, Spell spell)
        {
            EnsureInitialized();
            EnsureSpellLinked(spell);
            if (!CanCastHeroSpell(caster, spell))
            {
                return "Cannot cast spell.";
            }

            if (spell.Type != SkillType.Outside)
            {
                return "That spell cannot take you outside.";
            }

            if (Party.CurrentMapIsOverWorld)
            {
                return caster.Name + " casts " + spell.Name + "\nbut you are already outside.";
            }

            if (!SpendSpellCost(caster, spell))
            {
                return caster.Name + ": I do not have enough magic to cast " + spell.Name + ".";
            }

            var sourceMapId = Party.CurrentMapId;
            if (Party.CurrentPosition.HasValue)
            {
                RecordVisitedLocation(sourceMapId, Party.CurrentPosition.Value);
            }

            SetMap("overworld", null, Party.OverWorldPosition);
            NotifySaveLoaded();
            SaveAfterMapTransitionIfNeeded(sourceMapId, Party.CurrentMapId);
            return caster.Name + " casts " + spell.Name + ".";
        }

        public string CastReturnSpell(Hero caster, Spell spell, VisitedLocation location)
        {
            EnsureInitialized();
            EnsureSpellLinked(spell);
            if (!CanCastHeroSpell(caster, spell))
            {
                return "Cannot cast spell.";
            }

            if (spell.Type != SkillType.Return)
            {
                return "That spell cannot return you to a known place.";
            }

            if (!Party.CurrentMapIsOverWorld)
            {
                return caster.Name + " casts " + spell.Name + "\nbut you are not outside.";
            }

            if (location == null || string.IsNullOrEmpty(location.MapId))
            {
                return "Choose a place to return to.";
            }

            if (!SpendSpellCost(caster, spell))
            {
                return caster.Name + ": I do not have enough magic to cast " + spell.Name + ".";
            }

            var sourceMapId = Party.CurrentMapId;
            SetMap(location.MapId, null, location.Position);
            RecordVisitedLocation(location.MapId, location.Position);
            NotifySaveLoaded();
            SaveAfterMapTransitionIfNeeded(sourceMapId, Party.CurrentMapId);
            return caster.Name + " casts " + spell.Name + ".";
        }

        public string UseOutsideItem(Hero source, ItemInstance item)
        {
            EnsureInitialized();
            EnsureItemLinked(item);
            if (!CanUseHeroItem(source, item))
            {
                return "That item cannot be used.";
            }

            if (item.Item.Skill == null || item.Item.Skill.Type != SkillType.Outside)
            {
                return "That item cannot take you outside.";
            }

            if (Party.CurrentMapIsOverWorld)
            {
                return source.Name + " used " + item.Name + "\nbut you are already outside.";
            }

            var sourceMapId = Party.CurrentMapId;
            if (Party.CurrentPosition.HasValue)
            {
                RecordVisitedLocation(sourceMapId, Party.CurrentPosition.Value);
            }

            SetMap("overworld", null, Party.OverWorldPosition);
            ConsumeUsedItem(source, item);
            MarkDirty();
            NotifySaveLoaded();
            SaveAfterMapTransitionIfNeeded(sourceMapId, Party.CurrentMapId);
            return source.Name + " used " + item.Name + ".";
        }

        public string UseReturnItem(Hero source, ItemInstance item, VisitedLocation location)
        {
            EnsureInitialized();
            EnsureItemLinked(item);
            if (!CanUseHeroItem(source, item))
            {
                return "That item cannot be used.";
            }

            if (item.Item.Skill == null || item.Item.Skill.Type != SkillType.Return)
            {
                return "That item cannot return you to a known place.";
            }

            if (!Party.CurrentMapIsOverWorld)
            {
                return source.Name + " used " + item.Name + "\nbut you are not outside.";
            }

            if (location == null || string.IsNullOrEmpty(location.MapId))
            {
                return "Choose a place to return to.";
            }

            var sourceMapId = Party.CurrentMapId;
            SetMap(location.MapId, null, location.Position);
            RecordVisitedLocation(location.MapId, location.Position);
            ConsumeUsedItem(source, item);
            MarkDirty();
            NotifySaveLoaded();
            SaveAfterMapTransitionIfNeeded(sourceMapId, Party.CurrentMapId);
            return source.Name + " used " + item.Name + ".";
        }

        public Item GetCustomItem(string itemId)
        {
            Item item;
            return GameDataCache.Current != null &&
                   GameDataCache.Current.TryGetCustomItem(itemId, out item)
                ? item
                : null;
        }

        private List<Item> CreateMapObjectItems(TiledObjectInfo mapObject)
        {
            var items = new List<Item>();
            string itemId;
            if (mapObject.Properties != null &&
                mapObject.Properties.TryGetValue("ItemId", out itemId) &&
                !string.IsNullOrEmpty(itemId))
            {
                if (string.Equals(itemId, "#Random#", StringComparison.OrdinalIgnoreCase))
                {
                    var randomLevel = GetIntProperty(mapObject, mapObject.Class == "Chest" ? "ChestLevel" : "Level");
                    var randomItem = CreateChestItem(randomLevel == 0 ? GetAverageActivePartyLevel() : randomLevel);
                    if (randomItem != null)
                    {
                        items.Add(randomItem);
                    }

                    return items;
                }

                Item item;
                if (GameDataCache.Current != null &&
                    GameDataCache.Current.TryGetCustomItem(itemId, out item) &&
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

            return items;
        }

        private List<Item> CreateInitialStoreInventory(TiledObjectInfo mapObject)
        {
            var level = GetAverageActivePartyLevel();
            return StoreRules.CreateInitialStoreInventory(
                mapObject,
                GameDataCache.Current == null ? null : GameDataCache.Current.CustomItems,
                GetCustomItem,
                () => CreateRandomItem(level));
        }

        private static bool ContainsInvalidStoreItems(TiledObjectInfo mapObject, List<Item> items)
        {
            return StoreRules.ContainsInvalidStoreItems(mapObject, items);
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
            return RandomItemRules.CreateRandomItem(
                maxLevel,
                minLevel,
                rarity,
                GameDataCache.Current == null ? null : GameDataCache.Current.CustomItems,
                GameDataCache.Current == null ? null : GameDataCache.Current.ItemDefinitions,
                GameDataCache.Current == null ? null : GameDataCache.Current.StatNames,
                GameDataCache.Current == null ? null : GameDataCache.Current.Skills,
                () => Random.NextDouble(),
                maxValue => Random.Next(maxValue),
                () => Guid.NewGuid().ToString());
        }

        public Item CreateRandomEquipment(
            int maxLevel,
            int minLevel = 1,
            Rarity? rarity = null,
            ItemType? type = null,
            Class? itemClass = null,
            Slot? slot = null)
        {
            return RandomItemRules.CreateRandomEquipment(
                maxLevel,
                minLevel,
                rarity,
                type,
                itemClass,
                slot,
                GameDataCache.Current == null ? null : GameDataCache.Current.ItemDefinitions,
                GameDataCache.Current == null ? null : GameDataCache.Current.StatNames,
                GameDataCache.Current == null ? null : GameDataCache.Current.Skills,
                maxValue => Random.Next(maxValue),
                () => Guid.NewGuid().ToString());
        }

        private int GetAverageActivePartyLevel()
        {
            return Party == null ? 1 : Party.AverageActiveLevel();
        }

        public Item CreateGold(int gold)
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

        public string HealParty()
        {
            EnsureInitialized();
            if (Party == null || !Party.Members.Any())
            {
                return "There is no party to heal.";
            }

            foreach (var hero in Party.Members)
            {
                hero.Health = hero.MaxHealth;
                hero.Magic = hero.MaxMagic;
                if (hero.Status != null)
                {
                    hero.Status.Clear();
                }
            }

            MarkDirty();
            return "Your party has been fully restored.";
        }

        public string HealHero(Hero hero, int cost)
        {
            EnsureInitialized();
            if (hero == null || !Party.Members.Contains(hero) || hero.IsDead)
            {
                return "That party member cannot be healed.";
            }

            if (!SpendGold(cost))
            {
                return "You do not have " + cost + " gold.";
            }

            hero.Health = hero.MaxHealth;
            MarkDirty();
            return hero.Name + " has been fully healed.";
        }

        public string HealAllHeroes(int cost)
        {
            EnsureInitialized();
            if (!SpendGold(cost))
            {
                return "You do not have " + cost + " gold.";
            }

            foreach (var hero in Party.AliveMembers)
            {
                hero.Health = hero.MaxHealth;
            }

            MarkDirty();
            return "All party members have been healed.";
        }

        public string RenewMagic(int cost)
        {
            EnsureInitialized();
            if (!SpendGold(cost))
            {
                return "You do not have " + cost + " gold.";
            }

            foreach (var hero in Party.AliveMembers)
            {
                hero.Magic = hero.MaxMagic;
            }

            MarkDirty();
            return "All party members' magic has been replenished.";
        }

        public string CureHero(Hero hero, int cost)
        {
            EnsureInitialized();
            if (hero == null || !Party.Members.Contains(hero) || hero.Status == null || hero.Status.Count == 0)
            {
                return "That party member does not need curing.";
            }

            if (!SpendGold(cost))
            {
                return "You do not have " + cost + " gold.";
            }

            hero.Status.Clear();
            MarkDirty();
            return hero.Name + " has been cured.";
        }

        public string ReviveHero(Hero hero, int cost)
        {
            EnsureInitialized();
            if (hero == null || !Party.Members.Contains(hero) || !hero.IsDead)
            {
                return "That party member does not need reviving.";
            }

            if (!SpendGold(cost))
            {
                return "You do not have " + cost + " gold.";
            }

            hero.Health = 1;
            MarkDirty();
            return hero.Name + " has been revived.";
        }

        public string SaveAtCurrentPosition()
        {
            EnsureInitialized();
            if (Party == null)
            {
                return "There is no party to save.";
            }

            Party.SavedMapId = Party.CurrentMapId;
            Party.SavedPoint = Party.CurrentPosition;
            MarkDirty();
            SaveQuick();
            return "Game saved.";
        }

        public List<Item> CreateStoreInventory(int itemCount)
        {
            EnsureInitialized();
            var inventory = new List<Item>();
            var maxLevel = GetAverageActivePartyLevel();
            for (var i = 0; i < itemCount; i++)
            {
                var item = CreateRandomItem(maxLevel + 2, Math.Max(1, maxLevel - 2));
                if (item != null)
                {
                    inventory.Add(item);
                }
            }

            return inventory;
        }

        public List<Item> GetStoreInventory(TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            if (mapObject == null)
            {
                return new List<Item>();
            }

            var spriteState = GetSpriteState(Party.CurrentMapId, mapObject.Id, true);
            spriteState.Name = mapObject.Name;
            spriteState.Type = StoreRules.IsKeyStoreObject(mapObject)
                ? SpriteType.NpcKey
                : SpriteType.NpcStore;
            if (spriteState.Items == null || ContainsInvalidStoreItems(mapObject, spriteState.Items))
            {
                spriteState.Items = CreateInitialStoreInventory(mapObject);
                MarkDirty();
            }

            spriteState.Items = spriteState.Items
                .Where(item => item != null)
                .OrderBy(item => item.Cost)
                .ToList();
            return spriteState.Items;
        }

        public string BuyStoreItem(TiledObjectInfo mapObject, Item item)
        {
            EnsureInitialized();
            var recipient = Party == null
                ? null
                : Party.AliveMembers.FirstOrDefault(partyMember => partyMember.Items.Count < Party.MaxItems);
            ItemInstance purchasedItem;
            return BuyStoreItem(mapObject, item, recipient, out purchasedItem);
        }

        public string BuyStoreItem(TiledObjectInfo mapObject, Item item, Hero recipient, out ItemInstance purchasedItem)
        {
            EnsureInitialized();
            purchasedItem = null;
            if (item == null)
            {
                return "That item is not available.";
            }

            var inventory = mapObject == null ? null : GetStoreInventory(mapObject);
            var message = StoreRules.BuyStoreItem(Party, item, recipient, inventory, out purchasedItem);
            if (purchasedItem != null)
            {
                MarkDirty();
            }

            return message;
        }

        public string SellHeroItem(TiledObjectInfo mapObject, Hero hero, ItemInstance item)
        {
            EnsureInitialized();
            var inventory = mapObject == null ? null : GetStoreInventory(mapObject);
            var hadItem = hero != null && hero.Items != null && hero.Items.Contains(item);
            var message = StoreRules.SellHeroItem(Party, hero, item, inventory);
            if (hadItem && hero != null && hero.Items != null && !hero.Items.Contains(item))
            {
                MarkDirty();
            }

            return message;
        }

        public string BuyStoreItem(Item item)
        {
            return BuyStoreItem(null, item);
        }

        public string SellHeroItem(Hero hero, ItemInstance item)
        {
            return SellHeroItem(null, hero, item);
        }

        public bool CanUseHeroItem(Hero source, ItemInstance item)
        {
            EnsureItemLinked(item);
            return source != null &&
                   item != null &&
                   item.Item != null &&
                   Party != null &&
                   Party.Members.Contains(source) &&
                   source.Items.Contains(item) &&
                   item.HasCharges &&
                   source.CanUseItem(item);
        }

        public string UseHeroItemOnMapObject(Hero source, ItemInstance item, TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            EnsureItemLinked(item);
            if (!CanUseHeroItem(source, item))
            {
                return "That item cannot be used.";
            }

            if (mapObject == null)
            {
                return "There is nothing there.";
            }

            if (item.Item.IsKey || item.Item.Skill != null && item.Item.Skill.Type == SkillType.Open)
            {
                var wasOpen = IsObjectOpen(Party.CurrentMapId, mapObject.Id);
                var wasLocked = IsLockedMapObject(mapObject);
                var result = OpenMapObject(mapObject);
                if (wasLocked && !wasOpen && IsObjectOpen(Party.CurrentMapId, mapObject.Id))
                {
                    ConsumeUsedItem(source, item);
                    MarkDirty();
                }

                return source.Name + " used " + item.Name + ".\n" + result;
            }

            var objectState = GetTargetObjectState(mapObject);
            var useResult = item.Use(source, source, objectState, this, 0);
            if (useResult.Item2)
            {
                ConsumeUsedItem(source, item);
                MarkDirty();
            }

            return useResult.Item1;
        }

        public string CastHeroSpellOnMapObject(Hero caster, Spell spell, TiledObjectInfo mapObject)
        {
            EnsureInitialized();
            EnsureSpellLinked(spell);
            if (!CanCastHeroSpell(caster, spell))
            {
                return "That spell cannot be cast.";
            }

            if (mapObject == null)
            {
                return "There is nothing there.";
            }

            if (spell.Type == SkillType.Open)
            {
                if (caster.Magic < spell.Cost)
                {
                    return caster.Name + ": I do not have enough magic to cast " + spell.Name + ".";
                }

                caster.Magic -= spell.Cost;
                var result = OpenMapObject(mapObject);
                MarkDirty();
                return caster.Name + " casts " + spell.Name + ".\n" + result;
            }

            var objectState = GetTargetObjectState(mapObject);
            var message = spell.Cast(new List<IFighter>(), new[] { objectState }, caster, this);
            MarkDirty();
            return message;
        }

        private static void EnsureItemLinked(ItemInstance item)
        {
            if (item == null || item.Item == null || GameDataCache.Current == null)
            {
                return;
            }

            item.Item.Setup(GameDataCache.Current.Skills);
        }

        private static void EnsureSpellLinked(Spell spell)
        {
            if (spell == null || GameDataCache.Current == null)
            {
                return;
            }

            spell.Setup(GameDataCache.Current.Skills);
        }

        private bool SpendSpellCost(Hero caster, Spell spell)
        {
            if (caster == null || spell == null || caster.Magic < spell.Cost)
            {
                return false;
            }

            caster.Magic -= spell.Cost;
            MarkDirty();
            return true;
        }

        private void ConsumeUsedItem(Hero source, ItemInstance item)
        {
            if (source == null || item == null || item.Type != ItemType.OneUse)
            {
                return;
            }

            item.UnEquip(Party.Members);
            source.Items.Remove(item);
        }

        private static bool Chance(float probability)
        {
            return Random.NextDouble() < probability;
        }

        private static int GetIntProperty(TiledObjectInfo mapObject, string propertyName)
        {
            return GetIntProperty(mapObject, propertyName, 0);
        }

        private bool TryFindMapObjectKey(TiledObjectInfo mapObject, int keyLevel, out ItemInstance key, out Hero keyOwner)
        {
            key = null;
            keyOwner = null;
            if (Party == null)
            {
                return false;
            }

            string requiredKeyId = null;
            var hasRequiredKeyId = mapObject.Properties != null &&
                                   (mapObject.Properties.TryGetValue("KeyId", out requiredKeyId) ||
                                    mapObject.Properties.TryGetValue("KeyItemId", out requiredKeyId));

            foreach (var member in Party.AliveMembers)
            {
                if (member.Items == null)
                {
                    continue;
                }

                foreach (var item in member.Items)
                {
                    if (item == null || item.Item == null || !IsMapObjectKey(item, keyLevel))
                    {
                        continue;
                    }

                    if (hasRequiredKeyId && !IsItemMatch(item, requiredKeyId))
                    {
                        continue;
                    }

                    key = item;
                    keyOwner = member;
                    return true;
                }
            }

            return false;
        }

        private bool TryFindDoorKey(TiledObjectInfo mapObject, int doorLevel, out ItemInstance key, out Hero keyOwner)
        {
            return TryFindMapObjectKey(mapObject, doorLevel, out key, out keyOwner);
        }

        private static bool IsMapObjectKey(ItemInstance item, int keyLevel)
        {
            return item != null &&
                   item.Item != null &&
                   item.MinLevel == keyLevel &&
                   (item.Item.IsKey || string.Equals(item.Item.SkillId, "Open", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsItemMatch(ItemInstance item, string itemId)
        {
            return item != null &&
                   item.Item != null &&
                   !string.IsNullOrEmpty(itemId) &&
                   (string.Equals(item.Item.Id, itemId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Item.Name, itemId, StringComparison.OrdinalIgnoreCase));
        }

        private static bool CanOpenWithKey(TiledObjectInfo mapObject)
        {
            string value;
            return mapObject.Properties == null ||
                   !mapObject.Properties.TryGetValue("OpenWithKey", out value) ||
                   IsTrue(value);
        }

        private static bool IsLockedMapObject(TiledObjectInfo mapObject)
        {
            if (mapObject == null)
            {
                return false;
            }

            string value;
            if (mapObject.Properties != null && mapObject.Properties.TryGetValue("Locked", out value))
            {
                return IsTrue(value);
            }

            return IsDoorObject(mapObject);
        }

        private static bool IsTrue(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }

        private static int GetIntProperty(TiledObjectInfo mapObject, string propertyName, int defaultValue)
        {
            string value;
            int result;
            return mapObject.Properties != null &&
                   mapObject.Properties.TryGetValue(propertyName, out value) &&
                   int.TryParse(value, out result)
                ? result
                : defaultValue;
        }

        private bool SpendGold(int cost)
        {
            cost = Math.Max(0, cost);
            if (Party == null || Party.Gold < cost)
            {
                return false;
            }

            Party.Gold -= cost;
            MarkDirty();
            return true;
        }

        private bool CanOpenMapPickupObject(TiledObjectInfo mapObject)
        {
            if (mapObject == null || Party == null)
            {
                return false;
            }

            var level = GetMapPickupLevel(mapObject);
            return Party.CanOpenChest(level);
        }

        private static int GetMapPickupLevel(TiledObjectInfo mapObject)
        {
            if (mapObject == null)
            {
                return 0;
            }

            return string.Equals(mapObject.Class, "Chest", StringComparison.OrdinalIgnoreCase)
                ? GetIntProperty(mapObject, "ChestLevel", GetIntProperty(mapObject, "Level", 0))
                : GetIntProperty(mapObject, "Level", 0);
        }

        private ObjectState GetTargetObjectState(TiledObjectInfo mapObject)
        {
            var objectState = GetObjectState(Party.CurrentMapId, mapObject.Id, true);
            objectState.Name = mapObject.Name;
            if (IsDoorObject(mapObject))
            {
                objectState.Type = SpriteType.Door;
                objectState.Level = GetIntProperty(mapObject, "DoorLevel", 0);
            }
            else
            {
                objectState.Type = GetSpriteType(mapObject);
            }

            return objectState;
        }

        private static bool IsPickupObject(TiledObjectInfo mapObject)
        {
            return string.Equals(mapObject.Class, "Chest", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mapObject.Class, "HiddenItem", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDoorObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "Door", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsChestObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "Chest", StringComparison.OrdinalIgnoreCase);
        }

        private static SpriteType GetSpriteType(TiledObjectInfo mapObject)
        {
            if (string.Equals(mapObject.Class, "HiddenItem", StringComparison.OrdinalIgnoreCase))
            {
                return SpriteType.HiddenItem;
            }

            if (string.Equals(mapObject.Class, "NpcPartyMember", StringComparison.OrdinalIgnoreCase))
            {
                return SpriteType.NpcPartyMember;
            }

            return SpriteType.Chest;
        }

        private static bool IsPartyMemberObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "NpcPartyMember", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHiddenItemObject(TiledObjectInfo mapObject)
        {
            return mapObject != null && IsHiddenItemClass(mapObject.Class);
        }

        private static bool IsHiddenItemClass(string objectClass)
        {
            return string.Equals(objectClass, "HiddenItem", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPickupQuestConditionMet(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return true;
            }

            var item = GetCustomItem(itemId);
            if (item == null || string.IsNullOrEmpty(item.QuestId) || item.ForStage == null || item.ForStage.Count == 0)
            {
                return true;
            }

            var activeQuest = Party.ActiveQuests.FirstOrDefault(quest => quest.Id == item.QuestId);
            return activeQuest != null &&
                   !activeQuest.Completed &&
                   item.ForStage.Contains(activeQuest.CurrentStage);
        }

        private static string GetMapObjectItemId(TiledObjectInfo mapObject)
        {
            string itemId;
            return mapObject != null &&
                   mapObject.Properties != null &&
                   mapObject.Properties.TryGetValue("ItemId", out itemId)
                ? itemId
                : null;
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

            var normalizedMapId = Loader.NormalizeMapId(mapId);
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

        private SpriteState GetSpriteState(string mapId, int spriteId, bool create)
        {
            var mapState = GetMapState(mapId, create);
            if (mapState == null)
            {
                return null;
            }

            if (mapState.Sprites == null)
            {
                mapState.Sprites = new List<SpriteState>();
            }

            var spriteState = mapState.Sprites.FirstOrDefault(item => item.Id == spriteId);
            if (spriteState == null && create)
            {
                spriteState = new SpriteState { Id = spriteId };
                mapState.Sprites.Add(spriteState);
            }

            return spriteState;
        }

        private MapState GetMapState(string mapId, bool create)
        {
            if (CurrentSave.MapStates == null)
            {
                CurrentSave.MapStates = new List<MapState>();
            }

            var normalizedMapId = Loader.NormalizeMapId(mapId);
            var mapState = CurrentSave.MapStates.FirstOrDefault(item => item.Id == normalizedMapId);
            if (mapState == null && create)
            {
                mapState = new MapState { Id = normalizedMapId };
                CurrentSave.MapStates.Add(mapState);
            }

            return mapState;
        }

        public void SaveQuick()
        {
            EnsureInitialized();
            SaveQuickInternal();
        }

        public void SaveAfterMapTransitionIfNeeded(string sourceMapId, string targetMapId)
        {
            EnsureInitialized();
            if (!saveDirty || !IsOverWorldBoundaryTransition(sourceMapId, targetMapId))
            {
                return;
            }

            SaveQuickInternal();
        }

        public int ManualSaveSlotCount
        {
            get { return GetManualSaveSlots().Count + 1; }
        }

        public IReadOnlyList<GameSave> GetManualSaveSlots()
        {
            var file = GameFile ?? LoadGameFile();
            return file.Saves == null
                ? new List<GameSave>()
                : file.Saves.Where(save => !save.IsQuick && IsUsableSave(save)).ToList();
        }

        public bool SaveManual(int slotIndex)
        {
            EnsureInitialized();
            if (slotIndex < 0)
            {
                return false;
            }

            var save = CloneGameSave(CurrentSave);
            save.IsQuick = false;
            save.Time = DateTime.Now;
            var manualSaves = GameFile.Saves.Where(item => !item.IsQuick && IsUsableSave(item)).ToList();
            if (slotIndex >= manualSaves.Count)
            {
                GameFile.Saves.Add(save);
            }
            else
            {
                var existingIndex = GameFile.Saves.IndexOf(manualSaves[slotIndex]);
                if (existingIndex < 0)
                {
                    return false;
                }

                GameFile.Saves[existingIndex] = save;
            }

            SaveGameFile();
            saveDirty = false;
            autoSaveCountdown = 0f;
            return true;
        }

        public bool LoadManual(int slotIndex)
        {
            GameFile = LoadGameFile();
            if (slotIndex < 0)
            {
                return false;
            }

            var manualSaves = GameFile.Saves.Where(item => !item.IsQuick && IsUsableSave(item)).ToList();
            if (slotIndex >= manualSaves.Count)
            {
                return false;
            }

            var save = manualSaves[slotIndex];
            if (!IsUsableSave(save))
            {
                return false;
            }

            CurrentSave = save;
            ShouldApplyInitialSpawn = false;
            saveDirty = false;
            autoSaveCountdown = 0f;
            if (SaveLoaded != null)
            {
                SaveLoaded(CurrentSave);
            }

            return true;
        }

        public bool DeleteManual(int slotIndex)
        {
            GameFile = GameFile ?? LoadGameFile();
            if (slotIndex < 0)
            {
                return false;
            }

            if (GameFile.Saves == null)
            {
                return false;
            }

            var manualSaves = GameFile.Saves.Where(save => !save.IsQuick && IsUsableSave(save)).ToList();
            if (slotIndex >= manualSaves.Count)
            {
                return false;
            }

            var existing = manualSaves[slotIndex];
            var existingIndex = GameFile.Saves.IndexOf(existing);
            if (existingIndex < 0 || !IsUsableSave(existing))
            {
                return false;
            }

            GameFile.Saves.RemoveAt(existingIndex);
            SaveGameFile();
            return true;
        }

        private void SaveQuickInternal()
        {
            EnsureInitialized();
            var quickSave = CloneGameSave(CurrentSave);
            quickSave.IsQuick = true;
            quickSave.Time = DateTime.Now;
            UpsertQuickSave(quickSave);
            SaveGameFile();
            saveDirty = false;
            autoSaveCountdown = 0f;
        }

        public bool LoadQuick()
        {
            GameFile = LoadGameFile();
            var quickSave = GetQuickSave(GameFile);
            if (!IsUsableSave(quickSave))
            {
                Debug.LogWarning("No quick save found.");
                return false;
            }

            CurrentSave = quickSave;
            ShouldApplyInitialSpawn = false;
            saveDirty = false;
            autoSaveCountdown = 0f;
            if (SaveLoaded != null)
            {
                SaveLoaded(CurrentSave);
            }

            return true;
        }

        public bool HasQuickSave()
        {
            return IsUsableSave(GetQuickSave(LoadGameFile()));
        }

        public GameSave GetQuickSaveSlot()
        {
            return GetQuickSave(LoadGameFile());
        }

        public void RestartNewGame()
        {
            RestartNewGame("Player", Class.Hero, Gender.Male, null);
        }

        public void RestartNewGame(string playerName, Class playerClass, Gender gender)
        {
            RestartNewGame(playerName, playerClass, gender, null);
        }

        public void RestartNewGame(string playerName, Class playerClass, Gender gender, int? spriteFrameIndex)
        {
            GameFile = LoadGameFile();
            CurrentSave = CreateDefaultSave(playerName, playerClass, gender, spriteFrameIndex);
            CurrentSave.IsQuick = false;
            ShouldApplyInitialSpawn = true;
            MarkDirty();
            if (SaveLoaded != null)
            {
                SaveLoaded(CurrentSave);
            }
        }

        public Hero CreatePlayerPreviewHero(string playerName, Class playerClass, Gender gender)
        {
            return CreatePlayerPreviewHero(playerName, playerClass, gender, null);
        }

        public Hero CreatePlayerPreviewHero(string playerName, Class playerClass, Gender gender, int? spriteFrameIndex)
        {
            return CreateHero(playerName, playerClass, gender, 1, false, spriteFrameIndex);
        }

        public void MarkInitialSpawnApplied()
        {
            ShouldApplyInitialSpawn = false;
        }

        private GameSave CreateDefaultSave()
        {
            return CreateDefaultSave("Player", Class.Hero, Gender.Male, null);
        }

        private GameSave CreateDefaultSave(string playerName, Class playerClass, Gender gender, int? spriteFrameIndex)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player";
            }

            var party = new Party
            {
                PlayerName = playerName,
                CurrentMapId = Loader.NormalizeMapId(defaultMapId),
                CurrentPosition = new WorldPosition(defaultColumn, defaultRow)
            };
            party.CurrentMapIsOverWorld = party.CurrentMapId == "overworld";
            party.OverWorldPosition = party.CurrentPosition.Value;
            party.Members.Add(CreateHero(party.PlayerName, playerClass, gender, 1, true, spriteFrameIndex));

            return new GameSave
            {
                Party = party,
                IsQuick = true,
                Time = DateTime.Now
            };
        }

        private Hero CreateHero(string heroName, Class heroClass, Gender gender, int level, bool generateItems, int? spriteFrameIndex)
        {
            var hero = new Hero
            {
                Name = string.IsNullOrEmpty(heroName) ? "Player" : heroName,
                Class = heroClass,
                Gender = gender,
                SpriteFrameIndex = spriteFrameIndex,
                IsActive = true,
                Order = 0,
                Level = 1,
                Xp = 0
            };

            ApplyStartingClassStats(hero);
            var classLevels = GameDataCache.Current == null ? null : GameDataCache.Current.ClassLevels;
            if (classLevels != null && classLevels.Any(item => item.Class == hero.Class))
            {
                while (hero.Level < level)
                {
                    hero.Xp = hero.NextLevel;
                    string ignored;
                    hero.CheckLevelUp(
                        classLevels,
                        GameDataCache.Current == null ? null : GameDataCache.Current.Spells,
                        out ignored);
                }
            }

            if (generateItems)
            {
                AddStartingEquipment(hero);
            }

            return hero;
        }

        private static void ApplyStartingClassStats(Hero hero)
        {
            var classStats = GameDataCache.Current == null ||
                             GameDataCache.Current.ClassLevels == null
                ? null
                : GameDataCache.Current.ClassLevels.FirstOrDefault(item => item.Class == hero.Class);

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
            EquipStartingItem(hero, CreateRandomEquipment(hero.Level, Math.Max(hero.Level - 5, 1), Rarity.Common, ItemType.Armor, hero.Class, Slot.Chest));
            EquipStartingItem(hero, CreateRandomEquipment(hero.Level, Math.Max(hero.Level - 5, 1), Rarity.Common, ItemType.Weapon, hero.Class));
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

        private int GetNextPartyOrder()
        {
            return Party.ActiveMembers.Any() ? Party.ActiveMembers.Max(member => member.Order) + 1 : 0;
        }

        private static void SwapPartyOrder(Hero first, Hero second)
        {
            var order = first.Order;
            first.Order = second.Order;
            second.Order = order;
        }

        private void NormalizePartyOrder()
        {
            var activeMembers = Party.ActiveMembers.ToList();
            for (var i = 0; i < activeMembers.Count; i++)
            {
                activeMembers[i].Order = i;
            }
        }

        private static int GetMaxPartyMembers()
        {
            return SettingsCache.Current == null || SettingsCache.Current.MaxPartyMembers <= 0
                ? 4
                : SettingsCache.Current.MaxPartyMembers;
        }

        private string GetRecruitName(TiledObjectInfo mapObject, Gender gender)
        {
            var name = mapObject.Name;
            if (string.IsNullOrEmpty(name) || string.Equals(name, "#Random#", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateUniqueName(gender);
            }

            return name;
        }

        private string GenerateUniqueName(Gender gender)
        {
            EnsureNameGenerator();
            for (var i = 0; i < 20; i++)
            {
                var generated = nameGenerator == null ? "" : nameGenerator.Generate(gender);
                if (!string.IsNullOrEmpty(generated) &&
                    Party.Members.All(member => !string.Equals(member.Name, generated, StringComparison.OrdinalIgnoreCase)))
                {
                    return generated;
                }
            }

            return "Recruit " + (Party.Members.Count + 1);
        }

        private void EnsureNameGenerator()
        {
            if (nameGenerator != null)
            {
                return;
            }

            var names = GameDataCache.Current == null ? null : GameDataCache.Current.Names;
            if (names != null)
            {
                nameGenerator = new NameGenerator(names);
            }
        }

        private static void ApplyMapObjectSprite(Hero hero, string mapId, TiledObjectInfo mapObject)
        {
            if (hero == null || mapObject == null || mapObject.Gid <= 0)
            {
                return;
            }

            string tilesetPath;
            int tileId;
            if (!TryResolveMapObjectSprite(mapId, mapObject.Gid, out tilesetPath, out tileId))
            {
                return;
            }

            hero.SpriteFrameIndex = null;
            hero.SpriteTilesetPath = tilesetPath;
            hero.SpriteTileId = tileId;
        }

        private static bool TryResolveMapObjectSprite(string mapId, int gid, out string tilesetPath, out int tileId)
        {
            tilesetPath = null;
            tileId = 0;

            var loadedMap = Loader.Load(mapId);
            if (loadedMap == null || loadedMap.Info == null || loadedMap.Info.Tilesets == null)
            {
                return false;
            }

            TiledTilesetInfo selected = null;
            foreach (var tileset in loadedMap.Info.Tilesets.OrderBy(item => item.FirstGid))
            {
                if (tileset.FirstGid <= gid)
                {
                    selected = tileset;
                }
            }

            if (selected == null || string.IsNullOrEmpty(selected.UnityTilesetPath))
            {
                return false;
            }

            tilesetPath = selected.UnityTilesetPath;
            tileId = gid - selected.FirstGid;
            return true;
        }

        private static T GetEnumProperty<T>(TiledObjectInfo mapObject, string propertyName, T defaultValue)
            where T : struct
        {
            string value;
            T result;
            return mapObject.Properties != null &&
                   mapObject.Properties.TryGetValue(propertyName, out value) &&
                   Enum.TryParse(value, true, out result)
                ? result
                : defaultValue;
        }

        private void UpdateAutoSave()
        {
            if (!saveDirty || !IsAutoSaveEnabled())
            {
                return;
            }

            if (IsAutoSaveBlocked())
            {
                return;
            }

            autoSaveCountdown -= Time.deltaTime;
            if (autoSaveCountdown > 0f)
            {
                return;
            }

            SaveQuickInternal();
        }

        private static bool IsAutoSaveEnabled()
        {
            return SettingsCache.Current == null ||
                   SettingsCache.Current.AutoSaveEnabled;
        }

        private static float GetAutoSaveIntervalSeconds()
        {
            var interval = SettingsCache.Current == null
                ? DefaultAutoSaveIntervalSeconds
                : SettingsCache.Current.AutoSaveIntervalSeconds;
            return interval <= 0f ? DefaultAutoSaveIntervalSeconds : interval;
        }

        private static bool IsAutoSaveBlocked()
        {
            return AutoSaveBlocked ||
                   CombatWindow.IsOpen ||
                   TitleMenu.IsOpen ||
                   GameMenu.IsOpen ||
                   StoreWindow.IsOpen ||
                   HealerWindow.IsOpen ||
                   MessageBox.IsAnyVisible;
        }

        private static bool IsOverWorldBoundaryTransition(string sourceMapId, string targetMapId)
        {
            if (string.IsNullOrEmpty(sourceMapId) || string.IsNullOrEmpty(targetMapId))
            {
                return false;
            }

            var source = Loader.NormalizeMapId(sourceMapId);
            var target = Loader.NormalizeMapId(targetMapId);
            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.Equals(source, "overworld", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(target, "overworld", StringComparison.OrdinalIgnoreCase);
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
                    if (file != null && !string.Equals(file.Version, SaveFileVersion, StringComparison.Ordinal))
                    {
                        Debug.LogWarning(
                            "Unsupported save file version '" + file.Version +
                            "'. Expected '" + SaveFileVersion +
                            "'. The existing save will be archived and a new Unity save file will be created.");
                        ArchiveUnsupportedSaveFile(path, file.Version);
                        file = null;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Failed to load save file from " + path + ": " + exception.Message);
                }
            }

            if (file == null)
            {
                file = new GameFile { Version = SaveFileVersion };
            }

            return file;
        }

        private static void ArchiveUnsupportedSaveFile(string path, string version)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            var safeVersion = string.IsNullOrEmpty(version) ? "unknown" : SanitizeFileName(version);
            var archivePath = Path.Combine(
                directory,
                "save.unsupported-" + safeVersion + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json");

            try
            {
                File.Copy(path, archivePath, false);
                Debug.LogWarning("Archived unsupported save file to " + archivePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to archive unsupported save file from " + path + ": " + exception.Message);
            }
        }

        private void NotifySaveLoaded()
        {
            if (SaveLoaded != null)
            {
                SaveLoaded(CurrentSave);
            }
        }

        private void EnsureVisitedLocations()
        {
            if (Party.VisitedLocations == null)
            {
                Party.VisitedLocations = new List<VisitedLocation>();
            }
        }

        private static string SanitizeFileName(string value)
        {
            var result = value;
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(character, '-');
            }

            return result;
        }

        private List<RandomMonster> GetRandomMonstersForMap(string mapId)
        {
            var normalizedMapId = Loader.NormalizeMapId(mapId);
            List<RandomMonster> cached;
            if (randomMonsterCache.TryGetValue(normalizedMapId, out cached))
            {
                return cached;
            }

            var monsters = LoadRandomMonstersForMap(normalizedMapId);
            randomMonsterCache[normalizedMapId] = monsters;
            return monsters;
        }

        private List<RandomMonster> LoadRandomMonstersForMap(string mapId)
        {
            var data = GameDataCache.Current;
            if (data == null || data.Monsters == null)
            {
                return new List<RandomMonster>();
            }

            if (string.Equals(mapId, "overworld", StringComparison.OrdinalIgnoreCase))
            {
                return EncounterRules.CreateOverworldRandomMonsters(data.Monsters);
            }

            var assetPath = "Assets/DungeonEscape/Data/maps/" + mapId + "_monsters.json";
            var fullPath = UnityAssetPath.ToRuntimePath(assetPath);
            if (!File.Exists(fullPath))
            {
                return new List<RandomMonster>();
            }

            try
            {
                var randomMonsters = JsonConvert.DeserializeObject<List<RandomMonster>>(File.ReadAllText(fullPath)) ?? new List<RandomMonster>();
                EncounterRules.LinkRandomMonsters(randomMonsters, data.Monsters);

                return randomMonsters
                    .Where(monster => monster != null && monster.Data != null)
                    .ToList();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to load random monsters from " + assetPath + ": " + exception.Message);
                return new List<RandomMonster>();
            }
        }

        private List<Monster> BuildRandomEncounter(IEnumerable<RandomMonster> randomMonsters, BiomeInfo biomeInfo)
        {
            var aliveMembers = Party == null ? new List<Hero>() : Party.AliveMembers.ToList();
            var repelActive = aliveMembers.Any(partyMember =>
                partyMember.Status != null && partyMember.Status.Any(status => status.Type == EffectType.Repel));
            var repelMaxHealth = aliveMembers.Count == 0 ? 0 : aliveMembers.Max(member => member.MaxHealth);
            return EncounterRules.BuildRandomEncounter(
                randomMonsters,
                biomeInfo,
                GetAverageActivePartyLevel(),
                aliveMembers.Count,
                repelActive,
                repelMaxHealth,
                maxValue => Random.Next(maxValue),
                () => Dice.RollD20(),
                monster => Dice.Roll(monster.HealthRandom, monster.HealthTimes, monster.HealthConst));
        }

        private static GameSave GetQuickSave(GameFile file)
        {
            return file == null ? null : file.Saves.FirstOrDefault(save => save.IsQuick);
        }

        private static void EnsureManualSaveSlots(GameFile file)
        {
            if (file == null)
            {
                return;
            }

            if (file.Saves == null)
            {
                file.Saves = new List<GameSave>();
            }
        }

        private static GameSave CloneGameSave(GameSave save)
        {
            return save == null
                ? new GameSave()
                : JsonConvert.DeserializeObject<GameSave>(JsonConvert.SerializeObject(save));
        }

        private static bool IsUsableSave(GameSave save)
        {
            return GameSaveFormatter.IsUsableSave(save);
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

        public static GameState GetOrCreate()
        {
            var state = FindAnyObjectByType<GameState>();
            if (state != null)
            {
                state.EnsureInitialized();
                return state;
            }

            state = new GameObject("GameState").AddComponent<GameState>();
            state.EnsureInitialized();
            return state;
        }
    }
}
