using System.Collections.Generic;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Rendering
{
    public sealed class DirectionalSpriteSet
    {
        private readonly Dictionary<Direction, Sprite[]> sprites;
        private readonly Sprite fallback;

        public DirectionalSpriteSet(Dictionary<Direction, Sprite[]> sprites, Sprite fallback)
        {
            this.sprites = sprites;
            this.fallback = fallback;
        }

        public Sprite GetIdle(Direction direction)
        {
            var frames = GetFrames(direction);
            return frames.Length == 0 ? fallback : frames[0];
        }

        public Sprite GetStep(Direction direction, int step)
        {
            var frames = GetFrames(direction);
            if (frames.Length == 0)
            {
                return fallback;
            }

            return frames[Mathf.Abs(step) % frames.Length];
        }

        public Sprite[] GetFrames(Direction direction)
        {
            Sprite[] frames;
            return sprites != null && sprites.TryGetValue(direction, out frames) && frames != null
                ? frames
                : new[] { fallback };
        }
    }
}
