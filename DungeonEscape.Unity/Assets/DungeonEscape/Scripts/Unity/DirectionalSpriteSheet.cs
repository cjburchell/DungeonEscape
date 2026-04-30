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

        public static DirectionalSpriteSet LoadDirectionalSet(
            string textureAssetPath,
            int frameWidth,
            int frameHeight,
            string directionOrder,
            int frameCount,
            Sprite fallback)
        {
            var path = ToFullAssetPath(textureAssetPath);
            if (!File.Exists(path))
            {
                Debug.LogError("Directional texture not found: " + textureAssetPath);
                return CreateFallbackSet(fallback);
            }

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.LoadImage(bytes);

            return CreateDirectionalSet(texture, frameWidth, frameHeight, directionOrder, frameCount, fallback);
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

        public static DirectionalSpriteSet CreateDirectionalSet(
            Texture2D texture,
            int frameWidth,
            int frameHeight,
            string directionOrder,
            int frameCount,
            Sprite fallback)
        {
            if (texture == null || string.IsNullOrEmpty(directionOrder) || frameCount <= 0)
            {
                return CreateFallbackSet(fallback);
            }

            var sprites = new Dictionary<Direction, Sprite[]>();
            AddDirectionFrames(sprites, texture, frameWidth, frameHeight, directionOrder, frameCount, Direction.Up, 'N');
            AddDirectionFrames(sprites, texture, frameWidth, frameHeight, directionOrder, frameCount, Direction.Right, 'E');
            AddDirectionFrames(sprites, texture, frameWidth, frameHeight, directionOrder, frameCount, Direction.Down, 'S');
            AddDirectionFrames(sprites, texture, frameWidth, frameHeight, directionOrder, frameCount, Direction.Left, 'W');
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

        private static void AddDirectionFrames(
            IDictionary<Direction, Sprite[]> sprites,
            Texture2D texture,
            int frameWidth,
            int frameHeight,
            string directionOrder,
            int frameCount,
            Direction direction,
            char directionCode)
        {
            var directionIndex = directionOrder.IndexOf(directionCode);
            if (directionIndex < 0)
            {
                return;
            }

            var frames = new Sprite[frameCount];
            var startFrame = directionIndex * frameCount;
            for (var i = 0; i < frameCount; i++)
            {
                frames[i] = CreateSprite(texture, startFrame + i, frameWidth, frameHeight);
            }

            sprites[direction] = frames;
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
            return UnityAssetPath.ToRuntimePath(assetPath);
        }
    }
}
