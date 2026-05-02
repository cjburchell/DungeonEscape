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
        private const int DeadHeroBaseFrameIndex = 144;

        private sealed class ShopSellEntry
        {
            public Hero Hero { get; set; }
            public ItemInstance Item { get; set; }
        }

        [SerializeField]
        private TiledMapView mapView;

        [SerializeField]
        private string heroTextureAssetPath = "Assets/DungeonEscape/Images/sprites/hero.png";

        [SerializeField]
        private string shipTextureAssetPath = "Assets/DungeonEscape/Images/sprites/ship2.png";

        [SerializeField]
        private string cartTextureAssetPath = "Assets/DungeonEscape/Images/sprites/cart.png";

        private WorldPosition position;
        private SpriteRenderer spriteRenderer;
        private DirectionalSpriteSet directionSprites;
        private DirectionalSpriteSet heroDirectionSprites;
        private DirectionalSpriteSet shipDirectionSprites;
        private DirectionalSpriteSet cartDirectionSprites;
        private DirectionalSpriteSet deadHeroDirectionSprites;
        private Direction currentDirection = Direction.Down;
        private Class loadedHeroClass = Class.Hero;
        private Gender loadedHeroGender = Gender.Male;
        private bool loadedHeroIsDead;
        private DungeonEscapeGameState gameState;
        private DungeonEscapeMessageBox messageBox;
        private readonly List<PartyFollowerController> followers = new List<PartyFollowerController>();
        private PartyFollowerController cartFollower;
        private readonly List<WorldPosition> partyTrailPositions = new List<WorldPosition>();
        private readonly List<Direction> partyTrailDirections = new List<Direction>();
        private readonly Dictionary<string, DirectionalSpriteSet> heroSpriteCache = new Dictionary<string, DirectionalSpriteSet>();
        private bool hasPendingTurnMove;
        private Direction pendingTurnMoveDirection;
        private float pendingTurnMoveDelay;
        private bool isMoving;
        private bool showingShip;
        private bool isTransitioningMap;

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

        public bool IsMoving
        {
            get { return isMoving; }
        }

        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            heroDirectionSprites = LoadHeroSprites();
            deadHeroDirectionSprites = LoadDeadHeroSprites();
            directionSprites = heroDirectionSprites;
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
                hero.Class == loadedHeroClass &&
                hero.Gender == loadedHeroGender &&
                hero.IsDead == loadedHeroIsDead)
            {
                return;
            }

            loadedHeroClass = hero.Class;
            loadedHeroGender = hero.Gender;
            loadedHeroIsDead = hero.IsDead;
            heroDirectionSprites = LoadHeroSprites(hero);
            if (!showingShip)
            {
                directionSprites = heroDirectionSprites;
            }
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

            if (DungeonEscapeTitleMenu.IsOpen)
            {
                return;
            }

            if (DungeonEscapeStoreWindow.IsOpen)
            {
                return;
            }

            if (isMoving)
            {
                return;
            }

            if (isTransitioningMap)
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
                pendingTurnMoveDelay = GetTurnMoveDelay();
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
            UpdateTravelVisualState();
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

            var startPosition = position;
            var nextPosition = new WorldPosition(nextX, nextY);
            var duration = GetMoveDuration();
            if (mapView != null)
            {
                mapView.EnsureVisible(nextPosition, duration);
            }

            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var start = GetVisualPosition(startPosition);
                var end = GetVisualPosition(nextPosition);
                transform.position = Vector3.Lerp(start, end, progress);
                ApplyMoveSprite(direction, progress);
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
            if (!TryApplyWarp())
            {
                ApplyMapStepEffects();
            }

            UpdateVisualPosition();
            spriteRenderer.sprite = directionSprites.GetIdle(direction);
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

        private static float GetTurnMoveDelay()
        {
            var delay = DungeonEscapeSettingsCache.Current == null
                ? 0.12f
                : DungeonEscapeSettingsCache.Current.TurnMoveDelaySeconds;
            return Mathf.Clamp(delay, 0f, 0.3f);
        }

        private bool TryApplyWarp()
        {
            if (mapView == null)
            {
                return false;
            }

            TiledMapWarp warp;
            if (!mapView.TryGetWarpAt(position, out warp))
            {
                return false;
            }

            Debug.Log("Warping to " + warp.MapId + (string.IsNullOrEmpty(warp.SpawnId) ? "" : " at " + warp.SpawnId));
            ApplyMapTransition(warp);
            return true;
        }

        private void ApplyMapStepEffects()
        {
            if (mapView == null || gameState == null)
            {
                return;
            }

            var message = gameState.ApplyMapStepEffects(
                mapView.GetDamageAt(position),
                mapView.GetBiomeAt(position));
            RefreshPartyVisuals();
            if (!string.IsNullOrEmpty(message) && messageBox != null)
            {
                messageBox.Show("Terrain", message);
            }
        }

        private void ApplyMapTransition(TiledMapWarp warp)
        {
            if (mapView == null || warp == null || string.IsNullOrEmpty(warp.MapId))
            {
                return;
            }

            isTransitioningMap = true;
            DungeonEscapeScreenFade.GetOrCreate().FadeTransition(this, () => ApplyMapTransitionImmediate(warp));
        }

        private void ApplyMapTransitionImmediate(TiledMapWarp warp)
        {
            var sourceMapId = gameState == null || gameState.Party == null
                ? null
                : gameState.Party.CurrentMapId;
            string targetMapId = null;

            try
            {
                var transition = gameState == null
                    ? CreateFallbackTransition(warp)
                    : gameState.CreateWarpTransition(warp);
                targetMapId = transition.MapId;

                mapView.LoadMap(transition.MapId, transition.SpawnId, !transition.UseSavedOverWorldPosition);
                if (transition.UseSavedOverWorldPosition && gameState != null && gameState.Party != null)
                {
                    position = gameState.Party.OverWorldPosition;
                    gameState.SetCurrentPosition(position);
                    mapView.CenterOn(position);
                    SnapPartyFollowersToPlayer();
                }
                else
                {
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

                if (gameState != null)
                {
                    gameState.RecordVisitedLocation(targetMapId, position);
                    gameState.SaveAfterMapTransitionIfNeeded(sourceMapId, targetMapId);
                }
            }
            finally
            {
                isTransitioningMap = false;
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

        public string UseItemOnFacingObject(Hero hero, ItemInstance item)
        {
            if (gameState == null || mapView == null)
            {
                return "Cannot use that without game state.";
            }

            TiledObjectInfo mapObject;
            if (!mapView.TryGetObjectAt(GetFacingPosition(), out mapObject))
            {
                return "There is nothing there.";
            }

            var result = gameState.UseHeroItemOnMapObject(hero, item, mapObject);
            RefreshMapAfterObjectTarget();
            return result;
        }

        public string CastSpellOnFacingObject(Hero hero, Spell spell)
        {
            if (gameState == null || mapView == null)
            {
                return "Cannot cast that without game state.";
            }

            TiledObjectInfo mapObject;
            if (!mapView.TryGetObjectAt(GetFacingPosition(), out mapObject))
            {
                return "There is nothing there.";
            }

            var result = gameState.CastHeroSpellOnMapObject(hero, spell, mapObject);
            RefreshMapAfterObjectTarget();
            return result;
        }

        private void RefreshMapAfterObjectTarget()
        {
            if (mapView == null)
            {
                return;
            }

            mapView.RefreshObjectState();
            mapView.RefreshRender();
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

            if (IsServiceNpc(mapObject))
            {
                ShowServiceDialog(mapObject);
                return;
            }

            if (IsDoorObject(mapObject))
            {
                var result = gameState == null
                    ? "Cannot open door without game state."
                    : gameState.OpenDoor(mapObject);
                if (mapView != null)
                {
                    mapView.RefreshObjectState();
                    mapView.RefreshRender();
                }

                messageBox.Show(GetObjectDisplayName(mapObject), result);
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
                    : IsChestObject(mapObject)
                        ? gameState.OpenChest(mapObject)
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

        private void ShowServiceDialog(TiledObjectInfo mapObject)
        {
            if (IsHealerObject(mapObject))
            {
                ShowHealerDialog(mapObject);
                return;
            }

            if (IsSaveObject(mapObject))
            {
                ShowSaveDialog(mapObject);
                return;
            }

            if (IsStoreObject(mapObject))
            {
                DungeonEscapeStoreWindow.Show(gameState, mapObject);
            }
        }

        private void ShowHealerDialog(TiledObjectInfo mapObject)
        {
            if (gameState == null || gameState.Party == null)
            {
                messageBox.Show(GetObjectDisplayName(mapObject), "Cannot heal without game state.");
                return;
            }

            var cost = GetIntProperty(mapObject, "Cost", 25);
            var wounded = gameState.Party.AliveMembers.Where(member => member.Health != member.MaxHealth).ToList();
            var magicMissing = gameState.Party.AliveMembers.Where(member => member.Magic != member.MaxMagic).ToList();
            var statusMembers = gameState.Party.AliveMembers.Where(member => member.Status != null && member.Status.Count != 0).ToList();
            var dead = gameState.Party.DeadMembers.ToList();
            var labels = new List<string>();

            if (wounded.Count > 0)
            {
                labels.Add("Heal " + cost + "g");
                if (wounded.Count > 1)
                {
                    labels.Add("Heal All " + cost * wounded.Count + "g");
                }
            }

            if (magicMissing.Count > 0)
            {
                labels.Add("Renew Magic " + cost * 2 * magicMissing.Count + "g");
            }

            if (statusMembers.Count > 0)
            {
                labels.Add("Cure " + cost * 2 + "g");
            }

            if (dead.Count > 0)
            {
                labels.Add("Revive " + cost * 10 + "g");
            }

            if (labels.Count == 0)
            {
                messageBox.Show(GetObjectDisplayName(mapObject), "You do not require any of my services.");
                return;
            }

            labels.Add("Leave");
            messageBox.Show(
                GetObjectDisplayName(mapObject),
                "Do you require my services as a healer?\nGold: " + gameState.Party.Gold,
                labels,
                selectedIndex =>
                {
                    if (selectedIndex < 0 || selectedIndex >= labels.Count - 1)
                    {
                        return;
                    }

                    var selected = labels[selectedIndex];
                    if (selected.StartsWith("Heal All", StringComparison.OrdinalIgnoreCase))
                    {
                        messageBox.Show(GetObjectDisplayName(mapObject), ApplyHealerAction(() => gameState.HealAllHeroes(cost * wounded.Count)));
                    }
                    else if (selected.StartsWith("Heal ", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowHealerHeroPicker(mapObject, wounded, "Heal", hero => gameState.HealHero(hero, cost));
                    }
                    else if (selected.StartsWith("Renew Magic", StringComparison.OrdinalIgnoreCase))
                    {
                        messageBox.Show(GetObjectDisplayName(mapObject), ApplyHealerAction(() => gameState.RenewMagic(cost * 2 * magicMissing.Count)));
                    }
                    else if (selected.StartsWith("Cure", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowHealerHeroPicker(mapObject, statusMembers, "Cure", hero => gameState.CureHero(hero, cost * 2));
                    }
                    else if (selected.StartsWith("Revive", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowHealerHeroPicker(mapObject, dead, "Revive", hero => gameState.ReviveHero(hero, cost * 10));
                    }
                });
        }

        private void ShowHealerHeroPicker(TiledObjectInfo mapObject, List<Hero> heroes, string action, Func<Hero, string> apply)
        {
            if (heroes.Count == 1)
            {
                messageBox.Show(GetObjectDisplayName(mapObject), ApplyHealerAction(() => apply(heroes[0])));
                return;
            }

            var labels = heroes.Select(hero => hero.Name).ToList();
            labels.Add("Back");
            messageBox.Show(GetObjectDisplayName(mapObject), action + " who?", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= heroes.Count)
                {
                    ShowHealerDialog(mapObject);
                    return;
                }

                messageBox.Show(GetObjectDisplayName(mapObject), ApplyHealerAction(() => apply(heroes[selectedIndex])));
            });
        }

        private string ApplyHealerAction(Func<string> apply)
        {
            var message = apply == null ? "" : apply();
            RefreshPartyVisuals();
            return message;
        }

        private void ShowSaveDialog(TiledObjectInfo mapObject)
        {
            messageBox.Show(
                GetObjectDisplayName(mapObject),
                "Do you want to save your progress here?",
                new[] { "Save", "Leave" },
                selectedIndex =>
                {
                    if (selectedIndex != 0)
                    {
                        return;
                    }

                    messageBox.Show(
                        GetObjectDisplayName(mapObject),
                        gameState == null ? "Cannot save without game state." : gameState.SaveAtCurrentPosition());
                });
        }

        private void ShowStoreDialog(TiledObjectInfo mapObject)
        {
            var willBuyItems = GetBoolProperty(mapObject, "WillBuyItems", true) && !IsKeyStoreObject(mapObject);
            var choices = willBuyItems
                ? new[] { "Buy", "Sell", "Leave" }
                : new[] { "Buy", "Leave" };
            var text = IsKeyStoreObject(mapObject)
                ? "Would you like to buy a key?"
                : GetStringProperty(mapObject, "Text", "Welcome to my store.\nI buy and sell items. What can I do for you?");

            messageBox.Show(
                GetObjectDisplayName(mapObject),
                text,
                choices,
                selectedIndex =>
                {
                    if (selectedIndex == 0)
                    {
                        ShowBuyDialog(mapObject);
                    }
                    else if (willBuyItems && selectedIndex == 1)
                    {
                        ShowSellDialog(mapObject);
                    }
                });
        }

        private void ShowBuyDialog(TiledObjectInfo mapObject)
        {
            if (gameState == null)
            {
                messageBox.Show(GetObjectDisplayName(mapObject), "Cannot buy without game state.");
                return;
            }

            var inventory = gameState.GetStoreInventory(mapObject);
            if (inventory.Count == 0)
            {
                messageBox.Show(GetObjectDisplayName(mapObject), "I have nothing to sell right now.");
                return;
            }

            var labels = inventory.Select(item => item.NameWithStats + "  " + item.Cost + "g").ToList();
            labels.Add("Back");
            messageBox.Show(
                GetObjectDisplayName(mapObject),
                "Gold: " + gameState.Party.Gold,
                labels,
                selectedIndex =>
                {
                    if (selectedIndex < 0 || selectedIndex >= inventory.Count)
                    {
                        ShowStoreDialog(mapObject);
                        return;
                    }

                    messageBox.Show(GetObjectDisplayName(mapObject), gameState.BuyStoreItem(mapObject, inventory[selectedIndex]));
                });
        }

        private void ShowSellDialog(TiledObjectInfo mapObject)
        {
            if (gameState == null || gameState.Party == null)
            {
                messageBox.Show(GetObjectDisplayName(mapObject), "Cannot sell without game state.");
                return;
            }

            var entries = GetSellableItems().ToList();
            if (entries.Count == 0)
            {
                messageBox.Show(GetObjectDisplayName(mapObject), "You have nothing I can buy.");
                return;
            }

            var labels = entries
                .Select(entry => entry.Hero.Name + ": " + entry.Item.NameWithStats + "  " + Math.Max(1, entry.Item.Gold * 3 / 4) + "g")
                .ToList();
            labels.Add("Back");
            messageBox.Show(
                GetObjectDisplayName(mapObject),
                "What do you want to sell?",
                labels,
                selectedIndex =>
                {
                    if (selectedIndex < 0 || selectedIndex >= entries.Count)
                    {
                        ShowStoreDialog(mapObject);
                        return;
                    }

                    var entry = entries[selectedIndex];
                    messageBox.Show(GetObjectDisplayName(mapObject), gameState.SellHeroItem(mapObject, entry.Hero, entry.Item));
                });
        }

        private IEnumerable<ShopSellEntry> GetSellableItems()
        {
            if (gameState == null || gameState.Party == null)
            {
                yield break;
            }

            foreach (var hero in gameState.Party.Members)
            {
                if (hero.Items == null)
                {
                    continue;
                }

                foreach (var item in hero.Items)
                {
                    if (item != null && item.Item != null && item.Item.CanBeSoldInStore && item.Type != ItemType.Quest)
                    {
                        yield return new ShopSellEntry { Hero = hero, Item = item };
                    }
                }
            }
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

        private static string GetStringProperty(TiledObjectInfo mapObject, string propertyName, string defaultValue)
        {
            string value;
            return TryGetProperty(mapObject, propertyName, out value) && !string.IsNullOrEmpty(value)
                ? value
                : defaultValue;
        }

        private static int GetIntProperty(TiledObjectInfo mapObject, string propertyName, int defaultValue)
        {
            string value;
            int result;
            return TryGetProperty(mapObject, propertyName, out value) && int.TryParse(value, out result)
                ? result
                : defaultValue;
        }

        private static bool GetBoolProperty(TiledObjectInfo mapObject, string propertyName, bool defaultValue)
        {
            string value;
            bool result;
            return TryGetProperty(mapObject, propertyName, out value) && bool.TryParse(value, out result)
                ? result
                : defaultValue;
        }

        private static bool IsPickupObject(TiledObjectInfo mapObject)
        {
            return string.Equals(mapObject.Class, "Chest", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mapObject.Class, "HiddenItem", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsChestObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "Chest", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDoorObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "Door", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPartyMemberObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "NpcPartyMember", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsServiceNpc(TiledObjectInfo mapObject)
        {
            return IsHealerObject(mapObject) || IsSaveObject(mapObject) || IsStoreObject(mapObject);
        }

        private static bool IsHealerObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "NpcHeal", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSaveObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "NpcSave", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStoreObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   (string.Equals(mapObject.Class, "NpcStore", System.StringComparison.OrdinalIgnoreCase) ||
                    IsKeyStoreObject(mapObject));
        }

        private static bool IsKeyStoreObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "NpcKey", System.StringComparison.OrdinalIgnoreCase);
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
            UpdateTravelVisualState();
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

        private void RefreshPartyVisuals()
        {
            var party = gameState == null ? null : gameState.Party;
            if (party == null)
            {
                return;
            }

            ApplyPartySprite(party);
            UpdateTravelVisualState();
            if (spriteRenderer != null && directionSprites != null)
            {
                spriteRenderer.sprite = directionSprites.GetIdle(currentDirection);
            }

            SyncPartyFollowers();
            UpdateFollowerVisualPositions();
        }

        private void ResetPartyTrail()
        {
            partyTrailPositions.Clear();
            partyTrailDirections.Clear();
            var maxFollowers = GetTrailFollowerCount();
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
                    LoadHeroSprites(members[i]),
                    GetTrailPosition(i + 1),
                    GetTrailDirection(i + 1));
            }

            EnsureTrailLength(GetTrailFollowerCount() + 1);
            SyncCartFollower();
            UpdateFollowerVisibility();
        }

        private void UpdatePartyFollowers(WorldPosition nextPosition, Direction direction, float progress)
        {
            EnsureTrailLength(GetTrailFollowerCount() + 1);
            for (var i = 0; i < followers.Count; i++)
            {
                if (followers[i] == null)
                {
                    continue;
                }

                followers[i].SetPosition(GetTrailPosition(i), GetTrailDirection(i), progress);
            }

            if (cartFollower != null)
            {
                var cartIndex = followers.Count;
                cartFollower.SetPosition(GetTrailPosition(cartIndex), GetTrailDirection(cartIndex), progress);
            }
        }

        private void CommitPartyFollowers(WorldPosition nextPosition, Direction direction)
        {
            EnsureTrailLength(GetTrailFollowerCount() + 1);
            partyTrailPositions.Insert(0, nextPosition);
            partyTrailDirections.Insert(0, direction);

            var maxTrailLength = GetTrailFollowerCount() + 1;
            while (partyTrailPositions.Count > maxTrailLength)
            {
                partyTrailPositions.RemoveAt(partyTrailPositions.Count - 1);
            }

            while (partyTrailDirections.Count > maxTrailLength)
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

            if (cartFollower != null)
            {
                var cartIndex = followers.Count + 1;
                cartFollower.CommitPosition(GetTrailPosition(cartIndex), GetTrailDirection(cartIndex));
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

            if (cartFollower != null)
            {
                cartFollower.UpdateVisualPosition();
            }
        }

        private void SetFollowersVisible(bool visible)
        {
            foreach (var follower in followers)
            {
                if (follower != null)
                {
                    follower.gameObject.SetActive(visible);
                }
            }
        }

        private void SyncCartFollower()
        {
            if (!HasReserveMembers())
            {
                if (cartFollower != null)
                {
                    Destroy(cartFollower.gameObject);
                    cartFollower = null;
                }

                return;
            }

            if (cartFollower == null)
            {
                var cartObject = new GameObject("PartyCartFollower");
                cartObject.transform.SetParent(transform.parent, false);
                cartFollower = cartObject.AddComponent<PartyFollowerController>();
            }

            if (cartDirectionSprites == null)
            {
                cartDirectionSprites = LoadCartSprites();
            }

            var cartTrailIndex = followers.Count + 1;
            cartFollower.Configure(
                mapView,
                cartDirectionSprites,
                GetTrailPosition(cartTrailIndex),
                GetTrailDirection(cartTrailIndex));
        }

        private void UpdateFollowerVisibility()
        {
            SetFollowersVisible(!showingShip);
            if (cartFollower != null)
            {
                cartFollower.gameObject.SetActive(ShouldShowCart());
            }
        }

        private void SnapPartyFollowersToPlayer()
        {
            transform.position = GetVisualPosition(position);
            ResetPartyTrail();
            SyncPartyFollowers();
            UpdateFollowerVisualPositions();
        }

        private int GetPartyFollowerCount()
        {
            var party = gameState == null ? null : gameState.Party;
            return party == null ? 0 : Math.Max(0, party.ActiveMembers.Count() - 1);
        }

        private int GetTrailFollowerCount()
        {
            return GetPartyFollowerCount() + (HasReserveMembers() ? 1 : 0);
        }

        private bool HasReserveMembers()
        {
            var party = gameState == null ? null : gameState.Party;
            return party != null && party.InactiveMembers.Any();
        }

        private bool ShouldShowCart()
        {
            var party = gameState == null ? null : gameState.Party;
            return party != null &&
                   party.CurrentMapIsOverWorld &&
                   HasReserveMembers() &&
                   mapView != null &&
                   !mapView.IsWaterAt(position);
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

        private void ApplyMoveSprite(Direction direction, float progress)
        {
            if (spriteRenderer == null || directionSprites == null)
            {
                return;
            }

            var frame = progress < 0.5f ? 1 : 0;
            spriteRenderer.sprite = directionSprites.GetStep(direction, frame);
        }

        private DirectionalSpriteSet LoadHeroSprites()
        {
            return LoadHeroSprites(loadedHeroClass, loadedHeroGender);
        }

        private DirectionalSpriteSet LoadHeroSprites(Hero hero)
        {
            if (hero != null && hero.IsDead)
            {
                return deadHeroDirectionSprites ?? LoadDeadHeroSprites();
            }

            return hero == null ? LoadHeroSprites() : LoadHeroSprites(hero.Class, hero.Gender);
        }

        private DirectionalSpriteSet LoadHeroSprites(Class heroClass, Gender gender)
        {
            var cacheKey = "hero:" + heroClass + ":" + gender;
            DirectionalSpriteSet spriteSet;
            if (heroSpriteCache.TryGetValue(cacheKey, out spriteSet))
            {
                return spriteSet;
            }

            const int heroWidth = 32;
            const int heroHeight = 48;
            spriteSet = DirectionalSpriteSheet.LoadCharacterSet(
                heroTextureAssetPath,
                heroWidth,
                heroHeight,
                DirectionalSpriteSheet.GetHeroBaseFrameIndex(heroClass, gender),
                CreateFallbackSprite());
            heroSpriteCache[cacheKey] = spriteSet;
            return spriteSet;
        }

        private DirectionalSpriteSet LoadDeadHeroSprites()
        {
            DirectionalSpriteSet spriteSet;
            if (heroSpriteCache.TryGetValue("hero:dead", out spriteSet))
            {
                return spriteSet;
            }

            const int heroWidth = 32;
            const int heroHeight = 48;
            spriteSet = DirectionalSpriteSheet.LoadCharacterSet(
                heroTextureAssetPath,
                heroWidth,
                heroHeight,
                DeadHeroBaseFrameIndex,
                CreateFallbackSprite());
            heroSpriteCache["hero:dead"] = spriteSet;
            return spriteSet;
        }

        private DirectionalSpriteSet LoadShipSprites()
        {
            return DirectionalSpriteSheet.LoadDirectionalSet(
                shipTextureAssetPath,
                32,
                56,
                "SENW",
                2,
                CreateFallbackSprite());
        }

        private DirectionalSpriteSet LoadCartSprites()
        {
            return DirectionalSpriteSheet.LoadDirectionalSet(
                cartTextureAssetPath,
                48,
                54,
                "NESW",
                3,
                CreateFallbackSprite());
        }

        private void UpdateTravelVisualState()
        {
            var party = gameState == null ? null : gameState.Party;
            if (party != null)
            {
                ApplyPartySprite(party);
            }

            var shouldShowShip = party != null &&
                                 party.HasShip &&
                                 mapView != null &&
                                 mapView.IsWaterAt(position);

            if (shouldShowShip == showingShip && directionSprites != null)
            {
                UpdateFollowerVisibility();
                return;
            }

            showingShip = shouldShowShip;
            if (showingShip)
            {
                if (shipDirectionSprites == null)
                {
                    shipDirectionSprites = LoadShipSprites();
                }

                directionSprites = shipDirectionSprites;
            }
            else
            {
                directionSprites = heroDirectionSprites ?? LoadHeroSprites();
            }

            UpdateFollowerVisibility();
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
