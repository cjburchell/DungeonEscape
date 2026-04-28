using System.Collections;
using System.Collections.Generic;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class TiledNpcController : MonoBehaviour
    {
        private static readonly System.Random Random = new System.Random();

        private TiledMapView mapView;
        private IList<TiledTilesetSpriteSet> spriteSets;
        private SpriteRenderer spriteRenderer;
        private TiledSpriteAnimationPlayer animationPlayer;
        private int gid;
        private int tileWidth;
        private int tileHeight;
        private int column;
        private int row;
        private int homeColumn;
        private int homeRow;
        private int moveRadius = 3;
        private string mapId;
        private DungeonEscapeGameState gameState;
        private bool moving;
        private float nextMoveDelay;
        private Direction direction = Direction.Down;

        public int ObjectId { get; private set; }
        public TiledObjectInfo MapObject { get; private set; }
        public int Column { get { return column; } }
        public int Row { get { return row; } }

        public void Configure(
            TiledMapView view,
            TiledObjectInfo mapObject,
            IList<TiledTilesetSpriteSet> mapSpriteSets,
            int mapTileWidth,
            int mapTileHeight,
            int sortingOrder,
            string currentMapId,
            DungeonEscapeGameState currentGameState)
        {
            mapView = view;
            spriteSets = mapSpriteSets;
            mapId = currentMapId;
            gameState = currentGameState;
            MapObject = mapObject;
            gid = mapObject.Gid;
            tileWidth = mapTileWidth;
            tileHeight = mapTileHeight;
            ObjectId = mapObject.Id;
            column = Mathf.FloorToInt(mapObject.X / tileWidth);
            row = Mathf.FloorToInt((mapObject.Y - mapObject.Height) / tileHeight);
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

            spriteRenderer.sortingOrder = sortingOrder;
            animationPlayer = gameObject.GetComponent<TiledSpriteAnimationPlayer>();
            if (animationPlayer == null)
            {
                animationPlayer = gameObject.AddComponent<TiledSpriteAnimationPlayer>();
            }

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

            transform.localPosition = new Vector3(
                column - mapView.StartColumn,
                -(row - mapView.StartRow),
                -0.15f);
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
            var end = new Vector3(
                nextColumn - mapView.StartColumn,
                -(nextRow - mapView.StartRow),
                -0.15f);
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
            if (gameState != null)
            {
                gameState.SetObjectPosition(mapId, ObjectId, new WorldPosition(column, row), direction);
            }

            UpdateVisualPosition();
            PlayIdleAnimation();
            moving = false;
        }

        private void PlayIdleAnimation()
        {
            PlayDirectionAnimation(direction);
        }

        private void PlayDirectionAnimation(Direction selectedDirection)
        {
            List<TiledSpriteAnimationFrame> frames;
            if (TiledTilesetSprites.TryGetDirectionalAnimation(gid, spriteSets, selectedDirection, out frames) ||
                TiledTilesetSprites.TryGetDirectionalAnimation(gid, spriteSets, out frames))
            {
                animationPlayer.enabled = true;
                animationPlayer.Configure(spriteRenderer, frames);
                return;
            }

            Sprite sprite;
            if (TiledTilesetSprites.TryGetSprite(gid, spriteSets, out sprite))
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
