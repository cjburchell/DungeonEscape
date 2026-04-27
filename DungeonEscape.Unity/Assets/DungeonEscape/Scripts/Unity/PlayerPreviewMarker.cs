using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class PlayerPreviewMarker : MonoBehaviour
    {
        [SerializeField]
        private TiledMapPreviewRenderer mapPreview;

        private WorldPosition position;
        private SpriteRenderer spriteRenderer;

        public WorldPosition Position
        {
            get { return position; }
            set
            {
                position = value;
                UpdateVisualPosition();
            }
        }

        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateMarkerSprite();
            spriteRenderer.color = Color.cyan;
            spriteRenderer.sortingOrder = 1000;
            transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        }

        private void Start()
        {
            if (mapPreview == null)
            {
                mapPreview = FindObjectOfType<TiledMapPreviewRenderer>();
            }

            Position = new WorldPosition(30, 25);
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

            Position = new WorldPosition(Position.X + deltaX, Position.Y + deltaY);
            if (mapPreview != null)
            {
                mapPreview.CenterOn(Position);
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

        private static Sprite markerSprite;

        private static Sprite CreateMarkerSprite()
        {
            if (markerSprite != null)
            {
                return markerSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            markerSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return markerSprite;
        }
    }
}
