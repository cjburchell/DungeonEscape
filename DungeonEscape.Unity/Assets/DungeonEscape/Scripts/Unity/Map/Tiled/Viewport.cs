using System;
using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Map.Tiled
{
    public sealed class Viewport
    {
        private int mapWidth;
        private int mapHeight;

        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public int StartColumn { get; private set; }
        public int StartRow { get; private set; }

        public void Initialize(int width, int height, int columns, int rows, int startColumn, int startRow)
        {
            mapWidth = width;
            mapHeight = height;
            Columns = columns;
            Rows = rows;
            StartColumn = ClampColumn(startColumn);
            StartRow = ClampRow(startRow);
        }

        public bool SetStart(int newStartColumn, int newStartRow)
        {
            var changed = StartColumn != ClampColumn(newStartColumn) || StartRow != ClampRow(newStartRow);
            StartColumn = ClampColumn(newStartColumn);
            StartRow = ClampRow(newStartRow);
            return changed;
        }

        private int ClampColumn(int value)
        {
            return Math.Max(0, Math.Min(value, Math.Max(0, mapWidth - Columns)));
        }

        private int ClampRow(int value)
        {
            return Math.Max(0, Math.Min(value, Math.Max(0, mapHeight - Rows)));
        }
    }
}
