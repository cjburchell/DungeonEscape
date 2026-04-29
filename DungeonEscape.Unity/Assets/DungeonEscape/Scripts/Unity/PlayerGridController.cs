using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using System.Text;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class PlayerGridController : MonoBehaviour
    {
        [SerializeField]
        private TiledMapView mapView;

        [SerializeField]
        private string heroTextureAssetPath = "Assets/DungeonEscape/Images/sprites/hero.png";

        private WorldPosition position;
        private SpriteRenderer spriteRenderer;
        private DirectionalSpriteSet directionSprites;
        private Direction currentDirection = Direction.Down;
        private Class loadedHeroClass = Class.Hero;
        private Gender loadedHeroGender = Gender.Male;
        private Coroutine stepAnimation;
        private DungeonEscapeGameState gameState;
        private DungeonEscapeMessageBox messageBox;
        private readonly List<PartyFollowerController> followers = new List<PartyFollowerController>();
        private readonly List<WorldPosition> partyTrailPositions = new List<WorldPosition>();
        private readonly List<Direction> partyTrailDirections = new List<Direction>();
        private bool hasPendingTurnMove;
        private Direction pendingTurnMoveDirection;
        private float pendingTurnMoveDelay;
        private bool isMoving;

        public WorldPosition Position
        {
            get { return position; }
            set
            {
                position = value;
                UpdateVisualPosition();
            }
        }

        public int Column
        {
            get { return (int)position.X; }
        }

        public int Row
        {
            get { return (int)position.Y; }
        }

        public Direction FacingDirection
        {
            get { return currentDirection; }
        }

        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            directionSprites = LoadHeroSprites();
            spriteRenderer.sprite = directionSprites.GetIdle(currentDirection);
        }

        private void Start()
        {
            if (mapView == null)
            {
                mapView = FindAnyObjectByType<TiledMapView>();
            }

            gameState = DungeonEscapeGameState.GetOrCreate();
            gameState.SaveLoaded += OnSaveLoaded;
            messageBox = FindAnyObjectByType<DungeonEscapeMessageBox>();
            if (messageBox == null)
            {
                messageBox = new GameObject("DungeonEscapeMessageBox").AddComponent<DungeonEscapeMessageBox>();
            }

            ApplyGameStateToView();
        }

        private void OnDestroy()
        {
            if (gameState != null)
            {
                gameState.SaveLoaded -= OnSaveLoaded;
            }
        }

        private void OnSaveLoaded(GameSave save)
        {
            ApplyGameStateToView();
        }

        private void ApplyGameStateToView()
        {
            var party = gameState == null ? null : gameState.Party;
            if (party == null)
            {
                return;
            }

            if (mapView != null && !string.IsNullOrEmpty(party.CurrentMapId))
            {
                mapView.LoadMap(party.CurrentMapId, null, false);
                ApplyInitialSpawnIfNeeded();
            }

            Position = party.CurrentPosition ?? WorldPosition.Zero;
            ApplyPartySprite(party);
            SetFacing(party.CurrentDirection);
            ResetPartyTrail();
            SyncPartyFollowers();
            if (mapView != null)
            {
                mapView.CenterOn(Position);
                UpdateVisualPosition();
                UpdateFollowerVisualPositions();
            }
        }

        private void ApplyPartySprite(Party party)
        {
            var hero = party.ActiveMembers.FirstOrDefault();
            if (hero == null ||
                directionSprites == null ||
                hero.Class == loadedHeroClass && hero.Gender == loadedHeroGender)
            {
                return;
            }

            loadedHeroClass = hero.Class;
            loadedHeroGender = hero.Gender;
            directionSprites = LoadHeroSprites(loadedHeroClass, loadedHeroGender);
        }

        private void ApplyInitialSpawnIfNeeded()
        {
            if (gameState == null || mapView == null || !gameState.ShouldApplyInitialSpawn)
            {
                return;
            }

            WorldPosition spawnPosition;
            if (mapView.TryGetFirstSpawnPosition(out spawnPosition))
            {
                gameState.SetCurrentPosition(spawnPosition);
            }

            gameState.MarkInitialSpawnApplied();
        }

        private void Update()
        {
            if (DungeonEscapeGameMenu.IsOpen)
            {
                return;
            }

            if (isMoving)
            {
                return;
            }

            if (messageBox != null && messageBox.IsVisible)
            {
                if (!messageBox.HasChoices &&
                    (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact) ||
                     DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel)))
                {
                    messageBox.Hide();
                }

                return;
            }

            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact))
            {
                TryInteract();
                return;
            }

            var deltaX = DungeonEscapeInput.GetMoveX();
            var deltaY = deltaX == 0 ? DungeonEscapeInput.GetMoveY() : 0;

            if (deltaX == 0 && deltaY == 0)
            {
                hasPendingTurnMove = false;
                return;
            }

            var direction = GetDirection(deltaX, deltaY);
            if (direction != currentDirection)
            {
                SetFacing(direction);
                hasPendingTurnMove = true;
                pendingTurnMoveDirection = direction;
                pendingTurnMoveDelay = 0.18f;
                return;
            }

            if (hasPendingTurnMove)
            {
                if (direction != pendingTurnMoveDirection)
                {
                    hasPendingTurnMove = false;
                    return;
                }

                pendingTurnMoveDelay -= Time.deltaTime;
                if (pendingTurnMoveDelay > 0f)
                {
                    return;
                }

                hasPendingTurnMove = false;
            }

            var nextX = (int)Position.X + deltaX;
            var nextY = (int)Position.Y + deltaY;
            if (mapView != null && !mapView.CanMoveTo(nextX, nextY))
            {
                SetFacing(direction);
                return;
            }

            SetFacing(direction);
            StartCoroutine(MoveOneTile(direction, nextX, nextY));
        }

        private void LateUpdate()
        {
            UpdateSortingOrder();

            if (!isMoving)
            {
                UpdateVisualPosition();
            }
        }

        private void UpdateSortingOrder()
        {
            if (spriteRenderer == null || mapView == null)
            {
                return;
            }

            spriteRenderer.sortingOrder = mapView.GetObjectSortingOrder(Row);
        }

        private void UpdateVisualPosition()
        {
            transform.position = GetVisualPosition(position);
        }

        private Vector3 GetVisualPosition(WorldPosition value)
        {
            if (mapView == null)
            {
                return new Vector3(value.X, -value.Y, -0.2f);
            }

            return new Vector3(
                value.X - mapView.StartColumn,
                -(value.Y - mapView.StartRow),
                -0.2f) + mapView.ViewportOffset;
        }

        private IEnumerator MoveOneTile(Direction direction, int nextX, int nextY)
        {
            isMoving = true;
            PlayStepAnimation(direction);

            var startPosition = position;
            var nextPosition = new WorldPosition(nextX, nextY);
            if (mapView != null)
            {
                mapView.EnsureVisible(nextPosition);
            }

            var duration = GetMoveDuration();
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var start = GetVisualPosition(startPosition);
                var end = GetVisualPosition(nextPosition);
                transform.position = Vector3.Lerp(start, end, progress);
                UpdatePartyFollowers(nextPosition, direction, progress);
                yield return null;
            }

            position = nextPosition;
            if (gameState != null)
            {
                gameState.SetCurrentPosition(position);
                gameState.IncrementStepCount();
            }

            CommitPartyFollowers(nextPosition, direction);
            TryApplyWarp();
            UpdateVisualPosition();
            isMoving = false;
        }

        private static float GetMoveDuration()
        {
            const float baseDuration = 0.15f;
            if (!IsSprintHeld())
            {
                return baseDuration;
            }

            var boost = DungeonEscapeSettingsCache.Current == null
                ? 1.5f
                : DungeonEscapeSettingsCache.Current.SprintBoost;
            if (boost <= 0f)
            {
                boost = 1f;
            }

            return baseDuration / boost;
        }

        private static bool IsSprintHeld()
        {
            return DungeonEscapeInput.GetCommand(DungeonEscapeInputCommand.Sprint);
        }

        private void TryApplyWarp()
        {
            if (mapView == null)
            {
                return;
            }

            TiledMapWarp warp;
            if (!mapView.TryGetWarpAt(position, out warp))
            {
                return;
            }

            Debug.Log("Warping to " + warp.MapId + (string.IsNullOrEmpty(warp.SpawnId) ? "" : " at " + warp.SpawnId));
            ApplyMapTransition(warp);
        }

        private void ApplyMapTransition(TiledMapWarp warp)
        {
            if (mapView == null || warp == null || string.IsNullOrEmpty(warp.MapId))
            {
                return;
            }

            var transition = gameState == null
                ? CreateFallbackTransition(warp)
                : gameState.CreateWarpTransition(warp);

            mapView.LoadMap(transition.MapId, transition.SpawnId, !transition.UseSavedOverWorldPosition);
            if (transition.UseSavedOverWorldPosition && gameState != null && gameState.Party != null)
            {
                position = gameState.Party.OverWorldPosition;
                gameState.SetCurrentPosition(position);
                mapView.CenterOn(position);
                SnapPartyFollowersToPlayer();
                return;
            }

            WorldPosition spawnPosition;
            if (mapView.TryGetSpawnPosition(transition.SpawnId, out spawnPosition))
            {
                position = spawnPosition;
                if (gameState != null)
                {
                    gameState.SetCurrentPosition(position);
                }

                mapView.CenterOn(position);
                SnapPartyFollowersToPlayer();
            }
            else if (gameState != null && gameState.Party != null && gameState.Party.CurrentMapIsOverWorld)
            {
                position = gameState.Party.OverWorldPosition;
                gameState.SetCurrentPosition(position);
                mapView.CenterOn(position);
                SnapPartyFollowersToPlayer();
            }
        }

        private static DungeonEscapeMapTransition CreateFallbackTransition(TiledMapWarp warp)
        {
            var mapId = TiledMapLoader.NormalizeMapId(warp.MapId);
            return new DungeonEscapeMapTransition
            {
                MapId = mapId,
                SpawnId = warp.SpawnId,
                UseSavedOverWorldPosition = mapId == "overworld" && string.IsNullOrEmpty(warp.SpawnId)
            };
        }

        private void TryInteract()
        {
            if (mapView == null)
            {
                return;
            }

            var target = GetFacingPosition();
            TiledObjectInfo mapObject;
            if (!mapView.TryGetObjectAt(target, out mapObject))
            {
                Debug.Log("No interactable object at " + target.X + "," + target.Y + ".");
                return;
            }

            mapView.FaceNpcAt(target, GetOppositeDirection(currentDirection));
            Debug.Log(BuildInteractionMessage(mapObject, target));
            ShowInteractionMessage(mapObject);
        }

        private WorldPosition GetFacingPosition()
        {
            var x = (int)position.X;
            var y = (int)position.Y;

            switch (currentDirection)
            {
                case Direction.Up:
                    y--;
                    break;
                case Direction.Right:
                    x++;
                    break;
                case Direction.Down:
                    y++;
                    break;
                case Direction.Left:
                    x--;
                    break;
            }

            return new WorldPosition(x, y);
        }

        private void ShowInteractionMessage(TiledObjectInfo mapObject)
        {
            if (messageBox == null)
            {
                return;
            }

            if (IsPartyMemberObject(mapObject))
            {
                ShowRecruitDialog(mapObject);
                return;
            }

            string text;
            if (TryGetProperty(mapObject, "Text", out text) && !string.IsNullOrEmpty(text))
            {
                messageBox.Show(mapObject.Name, text);
                return;
            }

            string dialogId;
            if (TryGetProperty(mapObject, "Dialog", out dialogId) && !string.IsNullOrEmpty(dialogId))
            {
                DialogText dialogText;
                if (DungeonEscapeGameDataCache.Current != null &&
                    DungeonEscapeGameDataCache.Current.TryGetDialog(
                        dialogId,
                        gameState == null ? null : gameState.Party,
                        out dialogText))
                {
                    ShowDialog(mapObject.Name, dialogText, null);
                }
                else
                {
                    messageBox.Show(mapObject.Name, "Missing dialog: " + dialogId);
                }

                return;
            }

            if (IsPickupObject(mapObject))
            {
                var pickupMessage = gameState == null
                    ? "Cannot pick up item without game state."
                    : gameState.PickupMapObject(mapObject);
                if (mapView != null)
                {
                    mapView.RefreshRender();
                }

                messageBox.Show(mapObject.Name, pickupMessage);
                return;
            }

            messageBox.Show(mapObject.Name, string.IsNullOrEmpty(mapObject.Class) ? "Nothing happens." : mapObject.Class);
        }

        private void ShowRecruitDialog(TiledObjectInfo mapObject)
        {
            string text;
            if (!TryGetProperty(mapObject, "Text", out text) || string.IsNullOrEmpty(text))
            {
                text = "Can I join you?";
            }

            messageBox.Show(
                GetObjectDisplayName(mapObject),
                text,
                new[] { "Yes", "No" },
                selectedIndex =>
                {
                    if (selectedIndex != 0)
                    {
                        return;
                    }

                    var result = gameState == null
                        ? "Cannot recruit without game state."
                        : gameState.RecruitPartyMember(mapObject);
                    if (mapView != null)
                    {
                        mapView.RemoveRuntimeNpc(mapObject);
                    }

                    SyncPartyFollowers();

                    if (messageBox != null)
                    {
                        messageBox.Show(GetObjectDisplayName(mapObject), result);
                    }
                });
        }

        private void ShowDialog(string speakerName, DialogText dialog, string questContext)
        {
            if (messageBox == null || dialog == null)
            {
                return;
            }

            var choices = dialog.Choices == null
                ? new List<Choice>()
                : dialog.Choices.Where(IsVisibleDialogChoice).ToList();

            var dialogHead = dialog as DialogHead;
            var effectiveQuest = dialogHead != null && !string.IsNullOrEmpty(dialogHead.Quest)
                ? dialogHead.Quest
                : questContext;
            if (dialogHead != null && dialogHead.StartQuest && gameState != null)
            {
                var startMessage = gameState.StartQuest(dialogHead.Quest);
                if (!string.IsNullOrEmpty(startMessage))
                {
                    Debug.Log(startMessage);
                }
            }

            if (choices.Count == 0)
            {
                messageBox.Show(speakerName, dialog.Text);
                return;
            }

            messageBox.Show(
                speakerName,
                dialog.Text,
                choices.Select(choice => choice.Text),
                selectedIndex => ProcessDialogChoice(speakerName, effectiveQuest, choices[selectedIndex]));
        }

        private void ProcessDialogChoice(string speakerName, string questId, Choice choice)
        {
            if (choice == null)
            {
                return;
            }

            var resultMessage = new StringBuilder();
            var effectiveQuest = string.IsNullOrEmpty(choice.Quest) ? questId : choice.Quest;
            if (gameState != null && !string.IsNullOrEmpty(effectiveQuest) && choice.NextQuestStage.HasValue)
            {
                AppendMessage(resultMessage, gameState.AdvanceQuest(effectiveQuest, choice.NextQuestStage));
            }

            if (choice.Actions != null && choice.Actions.Contains(QuestAction.GiveItem))
            {
                AppendMessage(resultMessage, gameState == null ? "" : gameState.GiveItems(choice.Items));
            }

            var requiredItemWasTaken = true;
            if (choice.Actions != null && choice.Actions.Contains(QuestAction.TakeItem))
            {
                requiredItemWasTaken = gameState != null && gameState.HasItem(choice.ItemId);
                AppendMessage(resultMessage, gameState == null ? "" : gameState.TakeItem(choice.ItemId, speakerName));
            }

            if (requiredItemWasTaken &&
                choice.Actions != null &&
                choice.Actions.Contains(QuestAction.OpenDoor) &&
                choice.ObjectId.HasValue)
            {
                AppendMessage(resultMessage, gameState == null ? "" : gameState.OpenDoor(choice.ObjectId.Value));
                if (mapView != null)
                {
                    mapView.RefreshObjectState();
                }
            }

            if (choice.Dialog != null)
            {
                ShowDialog(speakerName, choice.Dialog, effectiveQuest);
                return;
            }

            if (choice.Actions != null && choice.Actions.Contains(QuestAction.Warp) && !string.IsNullOrEmpty(choice.MapId))
            {
                if (resultMessage.Length > 0 && messageBox != null)
                {
                    messageBox.Show(speakerName, resultMessage.ToString());
                }

                ApplyMapTransition(new TiledMapWarp { MapId = choice.MapId, SpawnId = choice.SpawnId });
                return;
            }

            if (choice.Actions != null && choice.Actions.Contains(QuestAction.GiveItem))
            {
                AppendMessage(resultMessage, gameState == null ? "" : gameState.GiveItems(choice.Items));
            }

            if (choice.Actions != null && choice.Actions.Contains(QuestAction.TakeItem))
            {
                AppendMessage(resultMessage, gameState == null ? "" : gameState.TakeItem(choice.ItemId, speakerName));
            }

            if (resultMessage.Length > 0)
            {
                messageBox.Show(speakerName, resultMessage.ToString());
            }
        }

        private bool IsVisibleDialogChoice(Choice choice)
        {
            if (choice == null || string.IsNullOrEmpty(choice.Text))
            {
                return false;
            }

            if (choice.Actions != null &&
                choice.Actions.Contains(QuestAction.TakeItem) &&
                !string.IsNullOrEmpty(choice.ItemId) &&
                gameState != null &&
                gameState.Party != null)
            {
                return gameState.Party.GetItem(choice.ItemId) != null;
            }

            return true;
        }

        private static void AppendMessage(StringBuilder builder, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(message.TrimEnd());
        }

        private static bool TryGetProperty(TiledObjectInfo mapObject, string propertyName, out string value)
        {
            value = null;
            return mapObject.Properties != null && mapObject.Properties.TryGetValue(propertyName, out value);
        }

        private static bool IsPickupObject(TiledObjectInfo mapObject)
        {
            return string.Equals(mapObject.Class, "Chest", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mapObject.Class, "HiddenItem", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPartyMemberObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "NpcPartyMember", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string GetObjectDisplayName(TiledObjectInfo mapObject)
        {
            if (mapObject == null || string.IsNullOrEmpty(mapObject.Name) ||
                string.Equals(mapObject.Name, "#Random#", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Traveler";
            }

            return mapObject.Name;
        }

        private static string BuildInteractionMessage(TiledObjectInfo mapObject, WorldPosition target)
        {
            var message = new StringBuilder();
            message.Append("Interact ");
            message.Append(target.X);
            message.Append(",");
            message.Append(target.Y);
            message.Append(": ");
            message.Append(string.IsNullOrEmpty(mapObject.Name) ? "(unnamed)" : mapObject.Name);
            message.Append(" [");
            message.Append(string.IsNullOrEmpty(mapObject.Class) ? "no class" : mapObject.Class);
            message.Append("]");

            if (mapObject.Properties != null && mapObject.Properties.Count > 0)
            {
                message.Append(" properties: ");
                var first = true;
                foreach (var property in mapObject.Properties)
                {
                    if (!first)
                    {
                        message.Append(", ");
                    }

                    message.Append(property.Key);
                    message.Append("=");
                    message.Append(property.Value);
                    first = false;
                }
            }

            return message.ToString();
        }

        private Direction GetDirection(int deltaX, int deltaY)
        {
            if (deltaX < 0)
            {
                return Direction.Left;
            }

            if (deltaX > 0)
            {
                return Direction.Right;
            }

            if (deltaY < 0)
            {
                return Direction.Up;
            }

            return Direction.Down;
        }

        private static Direction GetOppositeDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                default:
                    return Direction.Down;
            }
        }

        private void SetFacing(Direction direction)
        {
            currentDirection = direction;
            spriteRenderer.sprite = directionSprites.GetIdle(currentDirection);
            if (gameState != null)
            {
                gameState.SetCurrentDirection(direction);
            }
        }

        public void RefreshPartyFollowers()
        {
            var party = gameState == null ? null : gameState.Party;
            if (party != null)
            {
                ApplyPartySprite(party);
                SetFacing(currentDirection);
            }

            ResetPartyTrail();
            SyncPartyFollowers();
            UpdateFollowerVisualPositions();
        }

        private void ResetPartyTrail()
        {
            partyTrailPositions.Clear();
            partyTrailDirections.Clear();
            var maxFollowers = GetFollowerCount();
            for (var i = 0; i < maxFollowers + 1; i++)
            {
                partyTrailPositions.Add(position);
                partyTrailDirections.Add(currentDirection);
            }
        }

        private void SyncPartyFollowers()
        {
            var party = gameState == null ? null : gameState.Party;
            var members = party == null
                ? new List<Hero>()
                : party.ActiveMembers.Skip(1).ToList();

            while (followers.Count > members.Count)
            {
                var index = followers.Count - 1;
                if (followers[index] != null)
                {
                    Destroy(followers[index].gameObject);
                }

                followers.RemoveAt(index);
            }

            for (var i = 0; i < members.Count; i++)
            {
                if (i >= followers.Count)
                {
                    var followerObject = new GameObject("PartyFollower_" + members[i].Name);
                    followerObject.transform.SetParent(transform.parent, false);
                    var follower = followerObject.AddComponent<PartyFollowerController>();
                    followers.Add(follower);
                }

                followers[i].Configure(
                    mapView,
                    LoadHeroSprites(members[i].Class, members[i].Gender),
                    GetTrailPosition(i + 1),
                    GetTrailDirection(i + 1));
            }

            EnsureTrailLength(members.Count + 1);
        }

        private void UpdatePartyFollowers(WorldPosition nextPosition, Direction direction, float progress)
        {
            EnsureTrailLength(GetFollowerCount() + 1);
            for (var i = 0; i < followers.Count; i++)
            {
                if (followers[i] == null)
                {
                    continue;
                }

                followers[i].SetPosition(GetTrailPosition(i), GetTrailDirection(i), progress);
            }
        }

        private void CommitPartyFollowers(WorldPosition nextPosition, Direction direction)
        {
            EnsureTrailLength(GetFollowerCount() + 1);
            partyTrailPositions.Insert(0, nextPosition);
            partyTrailDirections.Insert(0, direction);

            while (partyTrailPositions.Count > followers.Count + 1)
            {
                partyTrailPositions.RemoveAt(partyTrailPositions.Count - 1);
            }

            while (partyTrailDirections.Count > followers.Count + 1)
            {
                partyTrailDirections.RemoveAt(partyTrailDirections.Count - 1);
            }

            for (var i = 0; i < followers.Count; i++)
            {
                if (followers[i] != null)
                {
                    followers[i].CommitPosition(GetTrailPosition(i + 1), GetTrailDirection(i + 1));
                }
            }
        }

        private void UpdateFollowerVisualPositions()
        {
            foreach (var follower in followers)
            {
                if (follower != null)
                {
                    follower.UpdateVisualPosition();
                }
            }
        }

        private void SnapPartyFollowersToPlayer()
        {
            transform.position = GetVisualPosition(position);
            ResetPartyTrail();
            SyncPartyFollowers();
            UpdateFollowerVisualPositions();
        }

        private int GetFollowerCount()
        {
            var party = gameState == null ? null : gameState.Party;
            return party == null ? 0 : Math.Max(0, party.ActiveMembers.Count() - 1);
        }

        private void EnsureTrailLength(int length)
        {
            while (partyTrailPositions.Count < length)
            {
                partyTrailPositions.Add(position);
            }

            while (partyTrailDirections.Count < length)
            {
                partyTrailDirections.Add(currentDirection);
            }
        }

        private WorldPosition GetTrailPosition(int index)
        {
            EnsureTrailLength(index + 1);
            return partyTrailPositions[index];
        }

        private Direction GetTrailDirection(int index)
        {
            EnsureTrailLength(index + 1);
            return partyTrailDirections[index];
        }

        private void PlayStepAnimation(Direction direction)
        {
            if (stepAnimation != null)
            {
                StopCoroutine(stepAnimation);
            }

            stepAnimation = StartCoroutine(AnimateStep(direction));
        }

        private IEnumerator AnimateStep(Direction direction)
        {
            spriteRenderer.sprite = directionSprites.GetStep(direction, 1);
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.sprite = directionSprites.GetIdle(direction);
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.sprite = directionSprites.GetStep(direction, 1);
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.sprite = directionSprites.GetIdle(direction);
            stepAnimation = null;
        }

        private DirectionalSpriteSet LoadHeroSprites()
        {
            return LoadHeroSprites(loadedHeroClass, loadedHeroGender);
        }

        private DirectionalSpriteSet LoadHeroSprites(Class heroClass, Gender gender)
        {
            const int heroWidth = 32;
            const int heroHeight = 48;
            return DirectionalSpriteSheet.LoadCharacterSet(
                heroTextureAssetPath,
                heroWidth,
                heroHeight,
                DirectionalSpriteSheet.GetHeroBaseFrameIndex(heroClass, gender),
                CreateFallbackSprite());
        }

        private static Sprite CreateFallbackSprite()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.cyan);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

    }
}
