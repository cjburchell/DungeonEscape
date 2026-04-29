using System;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class TiledMapViewport
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

        public bool PanBy(int columnDelta, int rowDelta, out Vector3 startOffset)
        {
            return SetViewport(StartColumn + columnDelta, StartRow + rowDelta, out startOffset);
        }

        public bool CenterOn(WorldPosition position, out Vector3 startOffset)
        {
            return SetViewport(
                (int)position.X - Columns / 2,
                (int)position.Y - Rows / 2,
                out startOffset);
        }

        public bool EnsureVisible(WorldPosition position, out Vector3 startOffset)
        {
            const int margin = 4;
            var column = (int)position.X;
            var row = (int)position.Y;
            var newStartColumn = StartColumn;
            var newStartRow = StartRow;

            if (column < StartColumn + margin)
            {
                newStartColumn = column - margin;
            }
            else if (column >= StartColumn + Columns - margin)
            {
                newStartColumn = column - Columns + margin + 1;
            }

            if (row < StartRow + margin)
            {
                newStartRow = row - margin;
            }
            else if (row >= StartRow + Rows - margin)
            {
                newStartRow = row - Rows + margin + 1;
            }

            return SetViewport(newStartColumn, newStartRow, out startOffset);
        }

        private bool SetViewport(int newStartColumn, int newStartRow, out Vector3 startOffset)
        {
            var oldStartColumn = StartColumn;
            var oldStartRow = StartRow;
            StartColumn = ClampColumn(newStartColumn);
            StartRow = ClampRow(newStartRow);

            startOffset = new Vector3(
                StartColumn - oldStartColumn,
                oldStartRow - StartRow,
                0f);

            return oldStartColumn != StartColumn || oldStartRow != StartRow;
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
