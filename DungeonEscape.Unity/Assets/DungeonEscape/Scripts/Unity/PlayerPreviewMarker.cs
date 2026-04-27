using System.IO;
using System.Collections.Generic;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class PlayerPreviewMarker : MonoBehaviour
    {
        [SerializeField]
        private TiledMapPreviewRenderer mapPreview;

        [SerializeField]
        private string heroTextureAssetPath = "Assets/DungeonEscape/Images/sprites/hero.png";

        private WorldPosition position;
        private SpriteRenderer spriteRenderer;
        private Dictionary<Direction, Sprite> directionSprites;

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

        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            directionSprites = LoadHeroSprites();
            spriteRenderer.sprite = directionSprites[Direction.Down];
            spriteRenderer.sortingOrder = 1000;
        }

        private void Start()
        {
            if (mapPreview == null)
            {
                mapPreview = FindObjectOfType<TiledMapPreviewRenderer>();
            }

            Position = new WorldPosition(30, 25);
            if (mapPreview != null)
            {
                mapPreview.CenterOn(Position);
                UpdateVisualPosition();
            }
        }

        private void Update()
        {
            var deltaX = 0;
            var deltaY = 0;

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                deltaX = -1;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                deltaX = 1;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                deltaY = -1;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                deltaY = 1;
            }

            if (deltaX == 0 && deltaY == 0)
            {
                return;
            }

            UpdateFacing(deltaX, deltaY);

            var nextX = (int)Position.X + deltaX;
            var nextY = (int)Position.Y + deltaY;
            if (mapPreview != null && !mapPreview.CanMoveTo(nextX, nextY))
            {
                return;
            }

            Position = new WorldPosition(nextX, nextY);
            if (mapPreview != null)
            {
                mapPreview.EnsureVisible(Position);
                UpdateVisualPosition();
            }
        }

        private void UpdateVisualPosition()
        {
            if (mapPreview == null)
            {
                transform.position = new Vector3(position.X, -position.Y, -0.2f);
                return;
            }

            transform.position = new Vector3(
                position.X - mapPreview.StartColumn,
                -(position.Y - mapPreview.StartRow),
                -0.2f);
        }

        private void UpdateFacing(int deltaX, int deltaY)
        {
            if (deltaX < 0)
            {
                spriteRenderer.sprite = directionSprites[Direction.Left];
            }
            else if (deltaX > 0)
            {
                spriteRenderer.sprite = directionSprites[Direction.Right];
            }
            else if (deltaY < 0)
            {
                spriteRenderer.sprite = directionSprites[Direction.Up];
            }
            else if (deltaY > 0)
            {
                spriteRenderer.sprite = directionSprites[Direction.Down];
            }
        }

        private Dictionary<Direction, Sprite> LoadHeroSprites()
        {
            var path = ToFullAssetPath(heroTextureAssetPath);
            if (!File.Exists(path))
            {
                Debug.LogError("Hero texture not found: " + heroTextureAssetPath);
                var fallback = CreateFallbackSprite();
                return new Dictionary<Direction, Sprite>
                {
                    { Direction.Up, fallback },
                    { Direction.Right, fallback },
                    { Direction.Down, fallback },
                    { Direction.Left, fallback }
                };
            }

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.LoadImage(bytes);

            const int heroWidth = 32;
            const int heroHeight = 48;
            return new Dictionary<Direction, Sprite>
            {
                { Direction.Up, CreateHeroSprite(texture, 0, heroWidth, heroHeight) },
                { Direction.Right, CreateHeroSprite(texture, 2, heroWidth, heroHeight) },
                { Direction.Down, CreateHeroSprite(texture, 4, heroWidth, heroHeight) },
                { Direction.Left, CreateHeroSprite(texture, 6, heroWidth, heroHeight) }
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

        private enum Direction
        {
            Up,
            Right,
            Down,
            Left
        }
    }
}
