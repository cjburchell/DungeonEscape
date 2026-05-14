namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class CombatViewModel
    {
        public int State { get; private set; }
        public int SelectedMenuIndex { get; private set; }

        public void Reset()
        {
            State = 0;
            SelectedMenuIndex = 0;
        }

        public void SetState(int value)
        {
            State = value;
        }

        public void SetSelectedMenuIndex(int value)
        {
            SelectedMenuIndex = value;
        }

        public int MoveSelection(int moveY, int count, bool wrap)
        {
            if (count <= 0)
            {
                SelectedMenuIndex = 0;
                return SelectedMenuIndex;
            }

            var nextIndex = SelectedMenuIndex + (moveY > 0 ? 1 : -1);
            SelectedMenuIndex = wrap ? WrapIndex(nextIndex, count) : Clamp(nextIndex, 0, count - 1);
            return SelectedMenuIndex;
        }

        private static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            if (index < 0)
            {
                return count - 1;
            }

            return index >= count ? 0 : index;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
