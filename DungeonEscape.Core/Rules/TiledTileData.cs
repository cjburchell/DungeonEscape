using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Redpoint.DungeonEscape.Rules
{
    public static class TiledTileData
    {
        private const uint TiledGidMask = 0x1FFFFFFF;

        public static List<int> ParseCsvTileData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new List<int>();
            }

            return data
                .Split(new[] { ',', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseGid)
                .ToList();
        }

        public static int ParseGid(string value)
        {
            uint result;
            return uint.TryParse(value, out result) ? (int)(result & TiledGidMask) : 0;
        }

        public static bool IsTrue(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }

        public static List<int> GetObjectBoundsTileIndexes(
            TiledObjectInfo mapObject,
            int tileWidth,
            int tileHeight,
            int mapWidth,
            int mapHeight)
        {
            var result = new List<int>();
            if (mapObject == null || tileWidth <= 0 || tileHeight <= 0 || mapWidth <= 0 || mapHeight <= 0)
            {
                return result;
            }

            var width = mapObject.Width <= 0f ? tileWidth : mapObject.Width;
            var height = mapObject.Height <= 0f ? tileHeight : mapObject.Height;
            var minColumn = FloorToInt(mapObject.X / tileWidth);
            var maxColumn = FloorToInt((mapObject.X + width - 0.001f) / tileWidth);
            var top = mapObject.Gid == 0 ? mapObject.Y : mapObject.Y - height;
            var bottom = mapObject.Gid == 0 ? mapObject.Y + height : mapObject.Y;
            var minRow = FloorToInt(top / tileHeight);
            var maxRow = FloorToInt((bottom - 0.001f) / tileHeight);

            for (var row = minRow; row <= maxRow; row++)
            {
                for (var column = minColumn; column <= maxColumn; column++)
                {
                    if (column < 0 || row < 0 || column >= mapWidth || row >= mapHeight)
                    {
                        continue;
                    }

                    result.Add(row * mapWidth + column);
                }
            }

            return result;
        }

        private static int FloorToInt(float value)
        {
            return (int)Math.Floor(value);
        }
    }
}
