using System.Collections.Generic;
using System.IO;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class DirectionalSpriteSheet
    {
        public const int CharacterFrameCount = 8;

        public static DirectionalSpriteSet LoadCharacterSet(
            string textureAssetPath,
            int frameWidth,
            int frameHeight,
            int baseFrameIndex,
            Sprite fallback)
        {
            var path = ToFullAssetPath(textureAssetPath);
            if (!File.Exists(path))
            {
                Debug.LogError("Character texture not found: " + textureAssetPath);
                return CreateFallbackSet(fallback);
            }

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.LoadImage(bytes);

            return CreateCharacterSet(texture, frameWidth, frameHeight, baseFrameIndex, fallback);
        }

        public static DirectionalSpriteSet CreateCharacterSet(
            Texture2D texture,
            int frameWidth,
            int frameHeight,
            int baseFrameIndex,
            Sprite fallback)
        {
            if (texture == null)
            {
                return CreateFallbackSet(fallback);
            }

            var sprites = new Dictionary<Direction, Sprite[]>
            {
                { Direction.Up, CreateDirectionFrames(texture, frameWidth, frameHeight, baseFrameIndex + 0) },
                { Direction.Right, CreateDirectionFrames(texture, frameWidth, frameHeight, baseFrameIndex + 2) },
                { Direction.Down, CreateDirectionFrames(texture, frameWidth, frameHeight, baseFrameIndex + 4) },
                { Direction.Left, CreateDirectionFrames(texture, frameWidth, frameHeight, baseFrameIndex + 6) }
            };

            return new DirectionalSpriteSet(sprites, fallback);
        }

        public static int GetHeroBaseFrameIndex(Class heroClass, Gender gender)
        {
            return ((int)heroClass * 16) + ((int)gender * CharacterFrameCount);
        }

        private static Sprite[] CreateDirectionFrames(Texture2D texture, int frameWidth, int frameHeight, int startFrameIndex)
        {
            return new[]
            {
                CreateSprite(texture, startFrameIndex, frameWidth, frameHeight),
                CreateSprite(texture, startFrameIndex + 1, frameWidth, frameHeight)
            };
        }

        private static Sprite CreateSprite(Texture2D texture, int frameIndex, int frameWidth, int frameHeight)
        {
            var columns = texture.width / frameWidth;
            var frameX = frameIndex % columns;
            var frameY = frameIndex / columns;
            var rect = new Rect(
                frameX * frameWidth,
                texture.height - ((frameY + 1) * frameHeight),
                frameWidth,
                frameHeight);

            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.33f), frameWidth);
        }

        private static DirectionalSpriteSet CreateFallbackSet(Sprite fallback)
        {
            return new DirectionalSpriteSet(
                new Dictionary<Direction, Sprite[]>
                {
                    { Direction.Up, new[] { fallback, fallback } },
                    { Direction.Right, new[] { fallback, fallback } },
                    { Direction.Down, new[] { fallback, fallback } },
                    { Direction.Left, new[] { fallback, fallback } }
                },
                fallback);
        }

        private static string ToFullAssetPath(string assetPath)
        {
            return Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
        }
    }
}
