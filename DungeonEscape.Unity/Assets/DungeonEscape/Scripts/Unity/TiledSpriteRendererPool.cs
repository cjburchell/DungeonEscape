using System.Collections.Generic;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class TiledSpriteRendererPool
    {
        private readonly Transform parent;
        private readonly Dictionary<string, SpriteRenderer> renderers = new Dictionary<string, SpriteRenderer>();
        private readonly HashSet<string> activeKeys = new HashSet<string>();

        public TiledSpriteRendererPool(Transform parent)
        {
            this.parent = parent;
        }

        public void Begin()
        {
            activeKeys.Clear();
        }

        public void Show(string key, Sprite sprite, Vector3 localPosition, int sortingOrder, string name)
        {
            Show(key, sprite, localPosition, Vector3.one, sortingOrder, name);
        }

        public void Show(string key, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder, string name)
        {
            activeKeys.Add(key);
            var renderer = GetRenderer(key);
            renderer.gameObject.name = name;
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = localScale;
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
    }
}
