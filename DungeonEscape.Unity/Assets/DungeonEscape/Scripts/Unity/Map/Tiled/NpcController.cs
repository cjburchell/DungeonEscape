using Redpoint.DungeonEscape.Data;
using System.Collections;
using System.Collections.Generic;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Map.Tiled
{
    public sealed class NpcController : MonoBehaviour
    {
        private static readonly System.Random Random = new System.Random();

        private View mapView;
        private IList<TilesetSpriteSet> spriteSets;
        private SpriteRenderer spriteRenderer;
        private SpriteAnimationPlayer animationPlayer;
        private MessageBox messageBox;
        private int gid;
        private int tileWidth;
        private int tileHeight;
        private int column;
        private int row;
        private int homeColumn;
        private int homeRow;
        private int moveRadius = 3;
        private float visualYOffset;
        private string mapId;
        private GameState gameState;
        private bool moving;
        private bool restoreFacingAfterDialog;
        private Direction restoreDirection;
        private float nextMoveDelay;
        private Direction direction = Direction.Down;

        public int ObjectId { get; private set; }
        public TiledObjectInfo MapObject { get; private set; }
        public int Column { get { return column; } }
        public int Row { get { return row; } }

        public void Configure(
            View view,
            TiledObjectInfo mapObject,
            IList<TilesetSpriteSet> mapSpriteSets,
            int mapTileWidth,
            int mapTileHeight,
            int sortingOrder,
            string currentMapId,
            GameState currentGameState)
        {
            mapView = view;
            spriteSets = mapSpriteSets;
            mapId = currentMapId;
            gameState = currentGameState;
            MapObject = mapObject;
            gid = mapObject.Gid;
            tileWidth = mapTileWidth;
            tileHeight = mapTileHeight;
            visualYOffset = GetVisualYOffset(mapObject, tileHeight);
            ObjectId = mapObject.Id;
            column = Mathf.FloorToInt(mapObject.X / tileWidth);
            row = Mathf.FloorToInt((mapObject.Y - 0.001f) / tileHeight);
            homeColumn = column;
            homeRow = row;
            moveRadius = GetIntProperty(mapObject, "MoveRadius", 0);
            direction = GetDirection(mapObject);
            ApplySavedState();

            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sortingOrder = mapView.GetObjectSortingOrder(row);
            animationPlayer = gameObject.GetComponent<SpriteAnimationPlayer>();
            if (animationPlayer == null)
            {
                animationPlayer = gameObject.AddComponent<SpriteAnimationPlayer>();
            }

            messageBox = FindAnyObjectByType<MessageBox>();
            PlayIdleAnimation();
            UpdateVisualPosition();
            nextMoveDelay = RandomDelay();
        }

        private void ApplySavedState()
        {
            if (gameState == null)
            {
                return;
            }

            WorldPosition savedPosition;
            if (gameState.TryGetObjectPosition(mapId, ObjectId, out savedPosition))
            {
                column = Mathf.FloorToInt(savedPosition.X);
                row = Mathf.FloorToInt(savedPosition.Y);
            }

            var savedDirection = gameState.GetObjectDirection(mapId, ObjectId);
            if (savedDirection.HasValue)
            {
                direction = savedDirection.Value;
            }
        }

        private void Update()
        {
            if (mapView == null || moving)
            {
                return;
            }

            if (restoreFacingAfterDialog && (messageBox == null || !messageBox.IsVisible))
            {
                restoreFacingAfterDialog = false;
                direction = restoreDirection;
                SaveState();
                PlayIdleAnimation();
            }

            if (moveRadius == 0 || IsInteractionLocked())
            {
                return;
            }

            nextMoveDelay -= Time.deltaTime;
            if (nextMoveDelay > 0f)
            {
                return;
            }

            nextMoveDelay = RandomDelay();
            TryMove();
        }

        public void UpdateVisualPosition()
        {
            if (mapView == null)
            {
                return;
            }

            transform.localPosition = GetLocalPosition(column, row);
        }

        private void TryMove()
        {
            var options = new[]
            {
                Direction.Up,
                Direction.Right,
                Direction.Down,
                Direction.Left
            };

            for (var i = 0; i < options.Length; i++)
            {
                var index = Random.Next(options.Length);
                var selected = options[index];
                options[index] = options[i];
                options[i] = selected;
            }

            foreach (var option in options)
            {
                int nextColumn;
                int nextRow;
                GetDelta(option, out nextColumn, out nextRow);
                nextColumn += column;
                nextRow += row;
                if (!mapView.CanNpcMoveTo(nextColumn, nextRow, this))
                {
                    continue;
                }

                if (!IsWithinHomeRadius(nextColumn, nextRow))
                {
                    continue;
                }

                StartCoroutine(MoveTo(option, nextColumn, nextRow));
                return;
            }
        }

        private bool IsInteractionLocked()
        {
            return messageBox != null && messageBox.IsVisible ||
                   StoreWindow.IsOpenFor(mapId, ObjectId) ||
                   HealerWindow.IsOpenFor(mapId, ObjectId);
        }

        public void Face(Direction selectedDirection)
        {
            if (moving)
            {
                return;
            }

            if (messageBox == null)
            {
                messageBox = FindAnyObjectByType<MessageBox>();
            }

            if (moveRadius == 0 && !restoreFacingAfterDialog)
            {
                restoreDirection = direction;
                restoreFacingAfterDialog = true;
            }

            direction = selectedDirection;
            SaveState();
            PlayIdleAnimation();
        }

        private bool IsWithinHomeRadius(int nextColumn, int nextRow)
        {
            if (moveRadius < 0)
            {
                return true;
            }

            return Mathf.Abs(nextColumn - homeColumn) + Mathf.Abs(nextRow - homeRow) <= moveRadius;
        }

        private IEnumerator MoveTo(Direction nextDirection, int nextColumn, int nextRow)
        {
            moving = true;
            direction = nextDirection;
            PlayDirectionAnimation(direction);
            mapView.UpdateNpcTile(this, column, row, nextColumn, nextRow);

            var start = transform.localPosition;
            var end = GetLocalPosition(nextColumn, nextRow);
            const float duration = 0.28f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            column = nextColumn;
            row = nextRow;
            SaveState();

            spriteRenderer.sortingOrder = mapView.GetObjectSortingOrder(row);
            UpdateVisualPosition();
            PlayIdleAnimation();
            moving = false;
        }

        private void SaveState()
        {
            if (gameState != null)
            {
                gameState.SetObjectPosition(mapId, ObjectId, new WorldPosition(column, row), direction);
            }
        }

        private Vector3 GetLocalPosition(int targetColumn, int targetRow)
        {
            return new Vector3(
                targetColumn,
                -targetRow + visualYOffset,
                -0.15f);
        }

        private static float GetVisualYOffset(TiledObjectInfo mapObject, int mapTileHeight)
        {
            if (mapObject == null || mapTileHeight <= 0)
            {
                return 0f;
            }

            var objectHeight = mapObject.Height <= 0f ? mapTileHeight : mapObject.Height;
            return Mathf.Max(0f, objectHeight - mapTileHeight) / (mapTileHeight * 2f);
        }

        private void PlayIdleAnimation()
        {
            PlayDirectionAnimation(direction);
        }

        private void PlayDirectionAnimation(Direction selectedDirection)
        {
            List<SpriteAnimationFrame> frames;
            if (TilesetSprites.TryGetDirectionalAnimation(gid, spriteSets, selectedDirection, out frames) ||
                TilesetSprites.TryGetDirectionalAnimation(gid, spriteSets, out frames))
            {
                animationPlayer.enabled = true;
                animationPlayer.Configure(spriteRenderer, frames);
                return;
            }

            Sprite sprite;
            if (TilesetSprites.TryGetSprite(gid, spriteSets, out sprite))
            {
                animationPlayer.Clear();
                animationPlayer.enabled = false;
                spriteRenderer.sprite = sprite;
            }
        }

        private static Direction GetDirection(TiledObjectInfo mapObject)
        {
            string value;
            if (mapObject.Properties != null &&
                (mapObject.Properties.TryGetValue("Direction", out value) ||
                 mapObject.Properties.TryGetValue("Facing", out value)))
            {
                if (string.Equals(value, "North", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Up", System.StringComparison.OrdinalIgnoreCase))
                {
                    return Direction.Up;
                }

                if (string.Equals(value, "East", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Right", System.StringComparison.OrdinalIgnoreCase))
                {
                    return Direction.Right;
                }

                if (string.Equals(value, "West", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Left", System.StringComparison.OrdinalIgnoreCase))
                {
                    return Direction.Left;
                }
            }

            return Direction.Down;
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

        private static void GetDelta(Direction selectedDirection, out int deltaColumn, out int deltaRow)
        {
            deltaColumn = 0;
            deltaRow = 0;
            switch (selectedDirection)
            {
                case Direction.Up:
                    deltaRow = -1;
                    break;
                case Direction.Right:
                    deltaColumn = 1;
                    break;
                case Direction.Down:
                    deltaRow = 1;
                    break;
                case Direction.Left:
                    deltaColumn = -1;
                    break;
            }
        }

        private static float RandomDelay()
        {
            return 1.2f + (float)Random.NextDouble() * 1.8f;
        }
    }
}
