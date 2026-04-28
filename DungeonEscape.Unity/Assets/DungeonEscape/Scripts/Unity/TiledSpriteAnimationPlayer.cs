using System.Collections.Generic;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class TiledSpriteAnimationPlayer : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private List<TiledSpriteAnimationFrame> frames;
        private int frameIndex;
        private float elapsed;

        public void Configure(SpriteRenderer renderer, List<TiledSpriteAnimationFrame> animationFrames)
        {
            spriteRenderer = renderer;
            frames = animationFrames;
            frameIndex = 0;
            elapsed = 0f;

            if (spriteRenderer != null && frames != null && frames.Count > 0)
            {
                spriteRenderer.sprite = frames[0].Sprite;
            }
        }

        public void Clear()
        {
            frames = null;
            frameIndex = 0;
            elapsed = 0f;
        }

        private void Update()
        {
            if (spriteRenderer == null || frames == null || frames.Count <= 1)
            {
                return;
            }

            elapsed += Time.deltaTime;
            while (elapsed >= frames[frameIndex].DurationSeconds)
            {
                elapsed -= frames[frameIndex].DurationSeconds;
                frameIndex = (frameIndex + 1) % frames.Count;
                spriteRenderer.sprite = frames[frameIndex].Sprite;
            }
        }
    }

    public sealed class TiledSpriteAnimationFrame
    {
        public Sprite Sprite { get; set; }
        public float DurationSeconds { get; set; }
    }
}
