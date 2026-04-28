using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class PartyFollowerController : MonoBehaviour
    {
        private TiledMapView mapView;
        private SpriteRenderer spriteRenderer;
        private DirectionalSpriteSet sprites;
        private WorldPosition position;
        private Direction direction = Direction.Down;

        public int Row
        {
            get { return (int)position.Y; }
        }

        public void Configure(
            TiledMapView view,
            DirectionalSpriteSet spriteSet,
            WorldPosition initialPosition,
            Direction initialDirection)
        {
            mapView = view;
            sprites = spriteSet;
            position = initialPosition;
            direction = initialDirection;

            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            ApplySprite();
            UpdateVisualPosition();
        }

        public void SetPosition(WorldPosition nextPosition, Direction nextDirection, float progress)
        {
            direction = nextDirection;
            transform.position = Vector3.Lerp(
                GetVisualPosition(position),
                GetVisualPosition(nextPosition),
                Mathf.Clamp01(progress));
            ApplySprite();
        }

        public void CommitPosition(WorldPosition nextPosition, Direction nextDirection)
        {
            position = nextPosition;
            direction = nextDirection;
            ApplySprite();
            UpdateVisualPosition();
        }

        public void UpdateVisualPosition()
        {
            transform.position = GetVisualPosition(position);
        }

        private void ApplySprite()
        {
            if (spriteRenderer == null || sprites == null)
            {
                return;
            }

            spriteRenderer.sprite = sprites.GetIdle(direction);
            spriteRenderer.sortingOrder = mapView == null ? Row : mapView.GetObjectSortingOrder(Row);
        }

        private Vector3 GetVisualPosition(WorldPosition value)
        {
            if (mapView == null)
            {
                return new Vector3(value.X, -value.Y, -0.21f);
            }

            return new Vector3(
                value.X - mapView.StartColumn,
                -(value.Y - mapView.StartRow),
                -0.21f) + mapView.ViewportOffset;
        }
    }
}
