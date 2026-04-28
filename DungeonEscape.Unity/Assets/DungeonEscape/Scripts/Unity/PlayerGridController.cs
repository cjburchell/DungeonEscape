using System.IO;
using System.Collections.Generic;
using System.Collections;
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
        private Dictionary<Direction, Sprite[]> directionSprites;
        private Direction currentDirection = Direction.Down;
        private Coroutine stepAnimation;
        private DungeonEscapeGameState gameState;
        private DungeonEscapeMessageBox messageBox;
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
            spriteRenderer.sprite = directionSprites[currentDirection][0];
        }

        private void Start()
        {
            if (mapView == null)
            {
                mapView = FindObjectOfType<TiledMapView>();
            }

            gameState = DungeonEscapeGameState.GetOrCreate();
            messageBox = FindObjectOfType<DungeonEscapeMessageBox>();
            if (messageBox == null)
            {
                messageBox = new GameObject("DungeonEscapeMessageBox").AddComponent<DungeonEscapeMessageBox>();
            }

            var party = gameState.Party;
            if (mapView != null && !string.IsNullOrEmpty(party.CurrentMapId))
            {
                mapView.LoadMap(party.CurrentMapId, null);
            }

            Position = party.CurrentPosition ?? WorldPosition.Zero;
            if (mapView != null)
            {
                mapView.CenterOn(Position);
                UpdateVisualPosition();
            }
        }

        private void Update()
        {
            if (isMoving)
            {
                return;
            }

            if (messageBox != null && messageBox.IsVisible)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
                {
                    messageBox.Hide();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                TryInteract();
                return;
            }

            var deltaX = 0;
            var deltaY = 0;

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                deltaX = -1;
            }
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                deltaX = 1;
            }
            else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                deltaY = -1;
            }
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                deltaY = 1;
            }

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

            spriteRenderer.sortingOrder = mapView.ObjectSortingOrder;
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

            const float duration = 0.15f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var start = GetVisualPosition(startPosition);
                var end = GetVisualPosition(nextPosition);
                transform.position = Vector3.Lerp(start, end, progress);
                yield return null;
            }

            position = nextPosition;
            if (gameState != null)
            {
                gameState.SetCurrentPosition(position);
                gameState.IncrementStepCount();
            }

            TryApplyWarp();
            UpdateVisualPosition();
            isMoving = false;
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
            mapView.LoadMap(warp.MapId, warp.SpawnId);
            if (gameState != null)
            {
                gameState.SetCurrentMap(warp.MapId);
            }

            WorldPosition spawnPosition;
            if (mapView.TryGetSpawnPosition(warp.SpawnId, out spawnPosition))
            {
                position = spawnPosition;
                if (gameState != null)
                {
                    gameState.SetCurrentPosition(position);
                }

                mapView.CenterOn(position);
            }
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

            string text;
            if (TryGetProperty(mapObject, "Text", out text) && !string.IsNullOrEmpty(text))
            {
                messageBox.Show(mapObject.Name, text);
                return;
            }

            string dialogId;
            if (TryGetProperty(mapObject, "Dialog", out dialogId) && !string.IsNullOrEmpty(dialogId))
            {
                messageBox.Show(mapObject.Name, "Dialog: " + dialogId);
                return;
            }

            if (mapObject.Class == "Chest")
            {
                string itemId;
                var itemText = TryGetProperty(mapObject, "ItemId", out itemId) && !string.IsNullOrEmpty(itemId)
                    ? "Chest contains: " + itemId
                    : "Chest";
                messageBox.Show(mapObject.Name, itemText);
                return;
            }

            messageBox.Show(mapObject.Name, string.IsNullOrEmpty(mapObject.Class) ? "Nothing happens." : mapObject.Class);
        }

        private static bool TryGetProperty(TiledObjectInfo mapObject, string propertyName, out string value)
        {
            value = null;
            return mapObject.Properties != null && mapObject.Properties.TryGetValue(propertyName, out value);
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

        private void SetFacing(Direction direction)
        {
            currentDirection = direction;
            spriteRenderer.sprite = directionSprites[currentDirection][0];
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
            spriteRenderer.sprite = directionSprites[direction][1];
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.sprite = directionSprites[direction][0];
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.sprite = directionSprites[direction][1];
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.sprite = directionSprites[direction][0];
            stepAnimation = null;
        }

        private Dictionary<Direction, Sprite[]> LoadHeroSprites()
        {
            var path = ToFullAssetPath(heroTextureAssetPath);
            if (!File.Exists(path))
            {
                Debug.LogError("Hero texture not found: " + heroTextureAssetPath);
                var fallback = CreateFallbackSprite();
                return new Dictionary<Direction, Sprite[]>
                {
                    { Direction.Up, new[] { fallback, fallback } },
                    { Direction.Right, new[] { fallback, fallback } },
                    { Direction.Down, new[] { fallback, fallback } },
                    { Direction.Left, new[] { fallback, fallback } }
                };
            }

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.LoadImage(bytes);

            const int heroWidth = 32;
            const int heroHeight = 48;
            return new Dictionary<Direction, Sprite[]>
            {
                { Direction.Up, new[] { CreateHeroSprite(texture, 0, heroWidth, heroHeight), CreateHeroSprite(texture, 1, heroWidth, heroHeight) } },
                { Direction.Right, new[] { CreateHeroSprite(texture, 2, heroWidth, heroHeight), CreateHeroSprite(texture, 3, heroWidth, heroHeight) } },
                { Direction.Down, new[] { CreateHeroSprite(texture, 4, heroWidth, heroHeight), CreateHeroSprite(texture, 5, heroWidth, heroHeight) } },
                { Direction.Left, new[] { CreateHeroSprite(texture, 6, heroWidth, heroHeight), CreateHeroSprite(texture, 7, heroWidth, heroHeight) } }
            };
        }

        private static Sprite CreateHeroSprite(Texture2D texture, int frameIndex, int heroWidth, int heroHeight)
        {
            var columns = texture.width / heroWidth;
            var frameX = frameIndex % columns;
            var frameY = frameIndex / columns;
            var rect = new Rect(
                frameX * heroWidth,
                texture.height - ((frameY + 1) * heroHeight),
                heroWidth,
                heroHeight);

            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.33f), heroWidth);
        }

        private static Sprite CreateFallbackSprite()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.cyan);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static string ToFullAssetPath(string assetPath)
        {
            return Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
        }

    }
}
