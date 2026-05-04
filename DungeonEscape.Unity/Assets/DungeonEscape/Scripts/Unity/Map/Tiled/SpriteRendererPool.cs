using System.Collections.Generic;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Map.Tiled
{
    public sealed class SpriteRendererPool
    {
        private sealed class GeneratedSpriteOwner : MonoBehaviour
        {
            private Sprite sprite;
            private Texture2D texture;

            public void Replace(Sprite newSprite, Texture2D newTexture)
            {
                Clear();
                sprite = newSprite;
                texture = newTexture;
            }

            public void Clear()
            {
                if (sprite != null)
                {
                    Object.Destroy(sprite);
                    sprite = null;
                }

                if (texture != null)
                {
                    Object.Destroy(texture);
                    texture = null;
                }
            }

            private void OnDestroy()
            {
                Clear();
            }
        }

        private readonly Transform parent;
        private readonly Dictionary<string, SpriteRenderer> renderers = new Dictionary<string, SpriteRenderer>();
        private readonly HashSet<string> activeKeys = new HashSet<string>();

        public SpriteRendererPool(Transform parent)
        {
            this.parent = parent;
        }

        public void Begin()
        {
            activeKeys.Clear();
        }

        public void Show(string key, Sprite sprite, Vector3 localPosition, int sortingOrder, string name)
        {
            Show(key, sprite, null, localPosition, Vector3.one, sortingOrder, name);
        }

        public void Show(string key, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder, string name)
        {
            Show(key, sprite, null, localPosition, localScale, sortingOrder, name);
        }

        public void Show(
            string key,
            Sprite sprite,
            List<SpriteAnimationFrame> animationFrames,
            Vector3 localPosition,
            int sortingOrder,
            string name)
        {
            Show(key, sprite, animationFrames, localPosition, Vector3.one, sortingOrder, name);
        }

        public void Show(
            string key,
            Sprite sprite,
            List<SpriteAnimationFrame> animationFrames,
            Vector3 localPosition,
            Vector3 localScale,
            int sortingOrder,
            string name)
        {
            activeKeys.Add(key);
            var renderer = GetRenderer(key);
            renderer.gameObject.name = name;
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = localScale;
            ClearGeneratedSprite(renderer);
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            ApplyAnimation(renderer, animationFrames);
            renderer.gameObject.SetActive(true);
        }

        public void ShowGeneratedTexture(
            string key,
            Texture2D texture,
            Vector3 localPosition,
            int sortingOrder,
            string name,
            int pixelsPerUnit)
        {
            activeKeys.Add(key);
            var renderer = GetRenderer(key);
            renderer.gameObject.name = name;
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = Vector3.one;
            ApplyAnimation(renderer, null);

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                1,
                SpriteMeshType.FullRect);
            var owner = renderer.GetComponent<GeneratedSpriteOwner>();
            if (owner == null)
            {
                owner = renderer.gameObject.AddComponent<GeneratedSpriteOwner>();
            }

            owner.Replace(sprite, texture);
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.gameObject.SetActive(true);
        }

        public void End()
        {
            foreach (var entry in renderers)
            {
                if (!activeKeys.Contains(entry.Key))
                {
                    entry.Value.gameObject.SetActive(false);
                }
            }
        }

        public void Clear()
        {
            foreach (var entry in renderers)
            {
                if (entry.Value != null)
                {
                    Object.Destroy(entry.Value.gameObject);
                }
            }

            renderers.Clear();
            activeKeys.Clear();
        }

        private SpriteRenderer GetRenderer(string key)
        {
            SpriteRenderer renderer;
            if (renderers.TryGetValue(key, out renderer))
            {
                return renderer;
            }

            var spriteObject = new GameObject("PooledTile");
            spriteObject.transform.SetParent(parent, false);
            renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderers[key] = renderer;
            return renderer;
        }

        private static void ApplyAnimation(SpriteRenderer renderer, List<SpriteAnimationFrame> animationFrames)
        {
            var player = renderer.GetComponent<SpriteAnimationPlayer>();
            if (animationFrames == null || animationFrames.Count <= 1)
            {
                if (player != null)
                {
                    player.Clear();
                    player.enabled = false;
                }

                return;
            }

            if (player == null)
            {
                player = renderer.gameObject.AddComponent<SpriteAnimationPlayer>();
            }

            player.enabled = true;
            player.Configure(renderer, animationFrames);
        }

        private static void ClearGeneratedSprite(SpriteRenderer renderer)
        {
            var owner = renderer.GetComponent<GeneratedSpriteOwner>();
            if (owner != null)
            {
                owner.Clear();
            }
        }
    }
}
