using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Tools;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeGameState : MonoBehaviour, IGame
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
        public static bool AutoSaveBlocked { get; set; }
        public event Action<GameSave> SaveLoaded;
        private bool saveDirty;
        private float autoSaveCountdown;
        private NameGenerator nameGenerator;

        public Party Party
        {
            get { return CurrentSave == null ? null : CurrentSave.Party; }
        }

        public List<ClassStats> ClassLevelStats
        {
            get
            {
                return DungeonEscapeGameDataCache.Current == null || DungeonEscapeGameDataCache.Current.ClassLevels == null
                    ? new List<ClassStats>()
                    : DungeonEscapeGameDataCache.Current.ClassLevels.ToList();
            }
        }

        public ISounds Sounds
        {
            get { return NullSounds.Instance; }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (DungeonEscapeTitleMenu.IsOpen)
            {
                return;
            }

            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.QuickSave))
            {
                SaveQuick();
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.QuickLoad))
            {
                LoadQuick();
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Restart))
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

        public void SetMap(string mapId = null, string spawnId = null, WorldPosition? point = null)
        {
            EnsureInitialized();

            var normalizedMapId = TiledMapLoader.NormalizeMapId(string.IsNullOrEmpty(mapId) ? "overworld" : mapId);
            SetCurrentMap(normalizedMapId);

            var mapView = FindAnyObjectByType<TiledMapView>();
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
            var normalizedMapId = TiledMapLoader.NormalizeMapId(mapId);
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
                    DisplayName = FormatLocationName(normalizedMapId)
                });
            }
            else
            {
                existing.Position = position;
                if (string.IsNullOrEmpty(existing.DisplayName))
                {
                    existing.DisplayName = FormatLocationName(normalizedMapId);
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
            if (damage <= 0 || Party.ActiveMembers == null)
            {
                return "";
            }

            var message = new StringBuilder();
            foreach (var hero in Party.ActiveMembers.Where(member => member != null && !member.IsDead))
            {
                hero.Health = Math.Max(0, hero.Health - damage);
                if (hero.IsDead)
                {
                    message.AppendLine(hero.Name + " has died.");
                }
            }

            if (damage > 0)
            {
                MarkDirty();
            }

            return message.ToString().TrimEnd();
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

            if (quest.Xp != 0)
            {
                AppendQuestXpReward(message, quest.Xp);
            }

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

        private void AppendQuestXpReward(StringBuilder message, int xp)
        {
            var activeMembers = Party.ActiveMembers.ToList();
            if (activeMembers.Count == 0)
            {
                return;
            }

            var xpReward = (ulong)Math.Max(0, xp);
            foreach (var hero in activeMembers)
            {
                hero.Xp += xpReward;
            }

            MarkDirty();
            message.AppendLine("The party got " + xp + " XP.");
            foreach (var hero in activeMembers)
            {
                AppendLevelUpMessages(message, hero);
            }
        }

        private void AppendLevelUpMessages(StringBuilder message, Hero hero)
        {
            if (hero == null)
            {
                return;
            }

            var classLevels = DungeonEscapeGameDataCache.Current == null ? null : DungeonEscapeGameDataCache.Current.ClassLevels;
            if (classLevels == null)
            {
                return;
            }

            while (true)
            {
                string levelUpMessage;
                if (!hero.CheckLevelUp(
                        classLevels,
                        DungeonEscapeGameDataCache.Current == null ? null : DungeonEscapeGameDataCache.Current.Spells,
                        out levelUpMessage))
                {
                    return;
                }

                MarkDirty();
                if (!string.IsNullOrEmpty(levelUpMessage))
                {
                    message.Append(levelUpMessage);
                }
            }
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
                return GiveGeneratedItem(CreateRandomItem(GetPartyMaxLevel()));
            }

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
            return keyOwner.Name + " used " + key.Name + ".\nThe door opened.";
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

            var hero = CreateHero(memberName, memberClass, gender, level, true);
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
                   DungeonEscapeGameDataCache.Current != null &&
                   DungeonEscapeGameDataCache.Current.Spells != null &&
                   Party.Members.Contains(caster) &&
                   spell.IsNonEncounterSpell &&
                   caster.GetSpells(DungeonEscapeGameDataCache.Current.Spells).Contains(spell);
        }

        public string CastHeroSpell(Hero caster, Spell spell, Hero target)
        {
            EnsureInitialized();
            if (!CanCastHeroSpell(caster, spell) || target == null || !Party.Members.Contains(target))
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

            var targets = Party.ActiveMembers.Cast<IFighter>().ToList();
            if (targets.Count == 0)
            {
                return "No party members can be targeted.";
            }

            var message = spell.Cast(targets, new BaseState[0], caster, this);
            MarkDirty();
            return string.IsNullOrEmpty(message) ? caster.Name + " casts " + spell.Name + "." : message.TrimEnd();
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
            return DungeonEscapeGameDataCache.Current != null &&
                   DungeonEscapeGameDataCache.Current.TryGetCustomItem(itemId, out item)
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
                    var randomItem = CreateChestItem(randomLevel == 0 ? GetPartyMaxLevel() : randomLevel);
                    if (randomItem != null)
                    {
                        items.Add(randomItem);
                    }

                    return items;
                }

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

            return items;
        }

        private List<Item> CreateInitialStoreInventory(TiledObjectInfo mapObject)
        {
            var items = new List<Item>();
            if (mapObject == null)
            {
                return items;
            }

            if (string.Equals(mapObject.Class, "NpcKey", StringComparison.OrdinalIgnoreCase))
            {
                return DungeonEscapeGameDataCache.Current == null || DungeonEscapeGameDataCache.Current.CustomItems == null
                    ? items
                    : DungeonEscapeGameDataCache.Current.CustomItems
                        .Where(item => item != null && item.IsKey)
                        .OrderBy(item => item.MinLevel)
                        .ThenBy(item => item.Cost)
                        .ToList();
            }

            string itemListString;
            if (mapObject.Properties != null &&
                mapObject.Properties.TryGetValue("Items", out itemListString) &&
                !string.IsNullOrWhiteSpace(itemListString))
            {
                foreach (var itemId in itemListString.Split(',').Select(value => value.Trim()))
                {
                    var item = GetCustomItem(itemId);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }

                return items.OrderBy(item => item.Cost).ToList();
            }

            var level = GetPartyMaxLevel();
            for (var i = 0; i < 10; i++)
            {
                var item = CreateRandomItem(level);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items.OrderBy(item => item.Cost).ToList();
        }

        private static bool ContainsInvalidStoreItems(TiledObjectInfo mapObject, List<Item> items)
        {
            if (items == null)
            {
                return true;
            }

            if (string.Equals(mapObject.Class, "NpcKey", StringComparison.OrdinalIgnoreCase))
            {
                return items.Any(item => item == null || !item.IsKey);
            }

            string itemListString;
            var hasFixedInventory = mapObject.Properties != null &&
                                    mapObject.Properties.TryGetValue("Items", out itemListString) &&
                                    !string.IsNullOrWhiteSpace(itemListString);
            if (hasFixedInventory)
            {
                return false;
            }

            return items.Any(item =>
                item == null ||
                item.Type == ItemType.Gold ||
                item.Type == ItemType.Quest ||
                item.Type == ItemType.Unknown);
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
            var maxLevel = GetPartyMaxLevel();
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
            spriteState.Type = string.Equals(mapObject.Class, "NpcKey", StringComparison.OrdinalIgnoreCase)
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

            if (recipient == null || Party == null || !Party.Members.Contains(recipient) || recipient.Items.Count >= Party.MaxItems)
            {
                return "No one has room to carry that.";
            }

            if (Party.Gold < item.Cost)
            {
                return "You do not have enough gold.";
            }

            purchasedItem = new ItemInstance(item);
            recipient.Items.Add(purchasedItem);

            Party.Gold -= item.Cost;
            if (mapObject != null)
            {
                var inventory = GetStoreInventory(mapObject);
                inventory.Remove(item);
            }

            MarkDirty();
            return recipient.Name + " bought " + item.Name + " for " + item.Cost + " gold.";
        }

        public string SellHeroItem(TiledObjectInfo mapObject, Hero hero, ItemInstance item)
        {
            EnsureInitialized();
            if (hero == null || item == null || item.Item == null || !Party.Members.Contains(hero) || !hero.Items.Contains(item))
            {
                return "That item cannot be sold.";
            }

            if (!item.Item.CanBeSoldInStore || item.Type == ItemType.Quest)
            {
                return item.Name + " cannot be sold.";
            }

            var salePrice = Math.Max(1, item.Gold * 3 / 4);
            item.UnEquip(Party.Members);
            hero.Items.Remove(item);
            Party.Gold += salePrice;
            if (mapObject != null)
            {
                var inventory = GetStoreInventory(mapObject);
                if (inventory.Count <= 15)
                {
                    inventory.Add(item.Item);
                    inventory.Sort((left, right) => left.Cost.CompareTo(right.Cost));
                }
            }

            MarkDirty();
            return hero.Name + " sold " + item.Name + " for " + salePrice + " gold.";
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
                var result = OpenDoor(mapObject);
                if (result.IndexOf("opened", StringComparison.OrdinalIgnoreCase) >= 0)
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
                var result = OpenDoor(mapObject);
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
            if (item == null || item.Item == null || DungeonEscapeGameDataCache.Current == null)
            {
                return;
            }

            item.Item.Setup(DungeonEscapeGameDataCache.Current.Skills);
        }

        private static void EnsureSpellLinked(Spell spell)
        {
            if (spell == null || DungeonEscapeGameDataCache.Current == null)
            {
                return;
            }

            spell.Setup(DungeonEscapeGameDataCache.Current.Skills);
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
            return GetIntProperty(mapObject, propertyName, 0);
        }

        private bool TryFindDoorKey(TiledObjectInfo mapObject, int doorLevel, out ItemInstance key, out Hero keyOwner)
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
                    if (item == null || item.Item == null || !IsDoorKey(item, doorLevel))
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

        private static bool IsDoorKey(ItemInstance item, int doorLevel)
        {
            return item != null &&
                   item.Item != null &&
                   item.MinLevel == doorLevel &&
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

            var normalizedMapId = TiledMapLoader.NormalizeMapId(mapId);
            var mapState = CurrentSave.MapStates.FirstOrDefault(item => item.Id == normalizedMapId);
            if (mapState == null && create)
            {
                mapState = new MapState { Id = normalizedMapId };
                CurrentSave.MapStates.Add(mapState);
            }

            return mapState;
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
            SaveQuick(false, "Quick saved to ");
        }

        public void SaveAfterMapTransitionIfNeeded(string sourceMapId, string targetMapId)
        {
            EnsureInitialized();
            if (!saveDirty || !IsOverWorldBoundaryTransition(sourceMapId, targetMapId))
            {
                return;
            }

            SaveQuick(true, "Transition saved to ");
        }

        public int ManualSaveSlotCount
        {
            get { return MaxSaveSlots; }
        }

        public IReadOnlyList<GameSave> GetManualSaveSlots()
        {
            EnsureInitialized();
            EnsureManualSaveSlots(GameFile);
            return GameFile.Saves.Where(save => !save.IsQuick).Take(MaxSaveSlots).ToList();
        }

        public bool SaveManual(int slotIndex)
        {
            EnsureInitialized();
            EnsureManualSaveSlots(GameFile);
            if (slotIndex < 0 || slotIndex >= MaxSaveSlots)
            {
                return false;
            }

            var manualSaves = GameFile.Saves.Where(save => !save.IsQuick).ToList();
            var existing = manualSaves[slotIndex];
            var existingIndex = GameFile.Saves.IndexOf(existing);
            if (existingIndex < 0)
            {
                return false;
            }

            var save = CloneGameSave(CurrentSave);
            save.IsQuick = false;
            save.Time = DateTime.Now;
            GameFile.Saves[existingIndex] = save;
            SaveGameFile();
            saveDirty = false;
            autoSaveCountdown = 0f;
            Debug.Log("Saved manual slot " + (slotIndex + 1) + " to " + GetSaveFilePath());
            return true;
        }

        public bool LoadManual(int slotIndex)
        {
            GameFile = LoadGameFile();
            EnsureManualSaveSlots(GameFile);
            if (slotIndex < 0 || slotIndex >= MaxSaveSlots)
            {
                return false;
            }

            var save = GameFile.Saves.Where(item => !item.IsQuick).ToList()[slotIndex];
            if (!IsUsableSave(save))
            {
                return false;
            }

            CurrentSave = save;
            ShouldApplyInitialSpawn = false;
            saveDirty = false;
            autoSaveCountdown = 0f;
            Debug.Log("Loaded manual slot " + (slotIndex + 1) + ": " + CurrentSave.Name);
            if (SaveLoaded != null)
            {
                SaveLoaded(CurrentSave);
            }

            return true;
        }

        public bool DeleteManual(int slotIndex)
        {
            EnsureInitialized();
            EnsureManualSaveSlots(GameFile);
            if (slotIndex < 0 || slotIndex >= MaxSaveSlots)
            {
                return false;
            }

            var manualSaves = GameFile.Saves.Where(save => !save.IsQuick).ToList();
            var existing = manualSaves[slotIndex];
            var existingIndex = GameFile.Saves.IndexOf(existing);
            if (existingIndex < 0 || !IsUsableSave(existing))
            {
                return false;
            }

            GameFile.Saves[existingIndex] = new GameSave();
            SaveGameFile();
            Debug.Log("Deleted manual slot " + (slotIndex + 1) + ".");
            return true;
        }

        private void SaveQuick(bool autoSave, string logPrefix)
        {
            EnsureInitialized();
            var quickSave = CloneGameSave(CurrentSave);
            quickSave.IsQuick = true;
            quickSave.Time = DateTime.Now;
            UpsertQuickSave(quickSave);
            SaveGameFile();
            saveDirty = false;
            autoSaveCountdown = 0f;
            Debug.Log((string.IsNullOrEmpty(logPrefix) ? autoSave ? "Auto saved to " : "Quick saved to " : logPrefix) + GetSaveFilePath());
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
            Debug.Log("Quick loaded: " + CurrentSave.Name);
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

        public static bool IsUsableGameSave(GameSave save)
        {
            return IsUsableSave(save);
        }

        public static string GetGameSaveTitle(GameSave save)
        {
            return IsUsableSave(save) ? save.Name : "Empty";
        }

        public static string GetGameSaveSummary(GameSave save)
        {
            if (!IsUsableSave(save))
            {
                return "No save data.";
            }

            var time = save.Time.HasValue ? save.Time.Value.ToString("g") : "Unknown time";
            var level = save.Level.HasValue ? "Level " + save.Level.Value : "No level";
            var map = save.Party == null || string.IsNullOrEmpty(save.Party.CurrentMapId) ? "Unknown map" : save.Party.CurrentMapId;
            return time + "    " + level + "    " + map;
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
            party.Members.Add(CreateHero(party.PlayerName, Class.Hero, Gender.Male, 1, true));

            return new GameSave
            {
                Party = party,
                IsQuick = true,
                Time = DateTime.Now
            };
        }

        private Hero CreateHero(string heroName, Class heroClass, Gender gender, int level, bool generateItems)
        {
            var hero = new Hero
            {
                Name = string.IsNullOrEmpty(heroName) ? "Player" : heroName,
                Class = heroClass,
                Gender = gender,
                IsActive = true,
                Order = 0,
                Level = 1,
                Xp = 0
            };

            ApplyStartingClassStats(hero);
            var classLevels = DungeonEscapeGameDataCache.Current == null ? null : DungeonEscapeGameDataCache.Current.ClassLevels;
            if (classLevels != null && classLevels.Any(item => item.Class == hero.Class))
            {
                while (hero.Level < level)
                {
                    hero.Xp = hero.NextLevel;
                    string ignored;
                    hero.CheckLevelUp(
                        classLevels,
                        DungeonEscapeGameDataCache.Current == null ? null : DungeonEscapeGameDataCache.Current.Spells,
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
            return DungeonEscapeSettingsCache.Current == null || DungeonEscapeSettingsCache.Current.MaxPartyMembers <= 0
                ? 4
                : DungeonEscapeSettingsCache.Current.MaxPartyMembers;
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

            var names = DungeonEscapeGameDataCache.Current == null ? null : DungeonEscapeGameDataCache.Current.Names;
            if (names != null)
            {
                nameGenerator = new NameGenerator(names);
            }
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

            SaveQuick(true, "Auto saved to ");
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

        private static bool IsAutoSaveBlocked()
        {
            return AutoSaveBlocked ||
                   DungeonEscapeTitleMenu.IsOpen ||
                   DungeonEscapeGameMenu.IsOpen ||
                   DungeonEscapeStoreWindow.IsOpen ||
                   DungeonEscapeMessageBox.IsAnyVisible;
        }

        private static bool IsOverWorldBoundaryTransition(string sourceMapId, string targetMapId)
        {
            if (string.IsNullOrEmpty(sourceMapId) || string.IsNullOrEmpty(targetMapId))
            {
                return false;
            }

            var source = TiledMapLoader.NormalizeMapId(sourceMapId);
            var target = TiledMapLoader.NormalizeMapId(targetMapId);
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

            EnsureManualSaveSlots(file);

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

        private static string FormatLocationName(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return "Unknown";
            }

            var name = mapId.Replace('\\', '/');
            var slashIndex = name.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex < name.Length - 1)
            {
                name = name.Substring(slashIndex + 1);
            }

            return string.Join(
                " ",
                name.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part.Substring(1))
                    .ToArray());
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

            for (var i = file.Saves.Count(save => !save.IsQuick); i < MaxSaveSlots; i++)
            {
                file.Saves.Add(new GameSave());
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
            var state = FindAnyObjectByType<DungeonEscapeGameState>();
            if (state != null)
            {
                state.EnsureInitialized();
                return state;
            }

            state = new GameObject("DungeonEscapeGameState").AddComponent<DungeonEscapeGameState>();
            state.EnsureInitialized();
            return state;
        }

        private sealed class NullSounds : ISounds
        {
            public static readonly NullSounds Instance = new NullSounds();

            public void PlaySoundEffect(string name, bool stopCurrent = false)
            {
            }
        }
    }
}
