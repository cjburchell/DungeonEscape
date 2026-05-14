using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class TitleViewModel
    {
        public const int CreateNameIndex = 0;
        public const int CreateGenerateNameIndex = 1;
        public const int CreateGenderIndex = 2;
        public const int CreateClassIndex = 3;
        public const int CreateImageIndex = 4;
        public const int CreateRerollIndex = 5;
        public const int CreateStartIndex = 6;
        public const int CreateBackIndex = 7;
        public const int FirstBlockedCreateSpriteIndex = 18;
        public const int SecondBlockedCreateSpriteIndex = 19;

        public TitleMode Mode { get; private set; }
        public int SelectedIndex { get; private set; }
        public string CreatePlayerName { get; private set; }
        public bool CreatePlayerNameInitialized { get; private set; }
        public Class CreatePlayerClass { get; private set; }
        public Gender CreatePlayerGender { get; private set; }
        public int CreatePlayerSpriteIndex { get; private set; }
        public TitleCreateDropdown ActiveCreateDropdown { get; private set; }
        public int SelectedDropdownIndex { get; private set; }

        public TitleViewModel()
        {
            Reset();
        }

        public void Reset()
        {
            Mode = TitleMode.Main;
            SelectedIndex = 0;
            CreatePlayerName = "Player";
            CreatePlayerNameInitialized = false;
            CreatePlayerClass = Class.Hero;
            CreatePlayerGender = Gender.Male;
            CreatePlayerSpriteIndex = 0;
            ActiveCreateDropdown = TitleCreateDropdown.None;
            SelectedDropdownIndex = 0;
        }

        public void ShowMainMenu()
        {
            Mode = TitleMode.Main;
            SelectedIndex = 0;
            ActiveCreateDropdown = TitleCreateDropdown.None;
        }

        public void ShowLoadMenu()
        {
            Mode = TitleMode.Load;
            SelectedIndex = 0;
            ActiveCreateDropdown = TitleCreateDropdown.None;
        }

        public void ShowCreateMenu()
        {
            Mode = TitleMode.Create;
            SelectedIndex = 0;
            ActiveCreateDropdown = TitleCreateDropdown.None;
        }

        public void MarkCreatePlayerNameInitialized()
        {
            CreatePlayerNameInitialized = true;
        }

        public void SetSelectedIndex(int index)
        {
            SelectedIndex = index;
        }

        public void SetCreatePlayerName(string value)
        {
            CreatePlayerName = value;
        }

        public void SetCreatePlayerGender(Gender value)
        {
            CreatePlayerGender = value;
        }

        public void SetCreatePlayerClass(Class value)
        {
            CreatePlayerClass = value;
        }

        public void SetCreatePlayerSpriteIndex(int value)
        {
            CreatePlayerSpriteIndex = value;
        }

        public void SetActiveCreateDropdown(TitleCreateDropdown value)
        {
            ActiveCreateDropdown = value;
        }

        public void SetSelectedDropdownIndex(int value)
        {
            SelectedDropdownIndex = value;
        }

        public List<TitleRow> GetMainRows(bool hasQuickSave, int manualSaveCount)
        {
            var rows = new List<TitleRow>();
            if (hasQuickSave)
            {
                rows.Add(new TitleRow { Label = "Continue", Enabled = true, Action = TitleMainAction.Continue });
            }

            rows.Add(new TitleRow { Label = "New Quest", Enabled = true, Action = TitleMainAction.NewQuest });
            if (manualSaveCount > 0)
            {
                rows.Add(new TitleRow { Label = "Load Quest", Enabled = true, Action = TitleMainAction.LoadQuest });
            }

            rows.Add(new TitleRow { Label = "Quit", Enabled = true, Action = TitleMainAction.Quit });
            return rows;
        }

        public int GetOptionCount(bool hasQuickSave, int manualSaveCount)
        {
            if (Mode == TitleMode.Load)
            {
                return manualSaveCount * 2 + 1;
            }

            if (Mode == TitleMode.Create)
            {
                return 8;
            }

            return GetMainRows(hasQuickSave, manualSaveCount).Count;
        }

        public int GetMainNavigationIndex(int moveY, bool hasQuickSave, int manualSaveCount)
        {
            return moveY == 0
                ? SelectedIndex
                : Clamp(SelectedIndex + moveY, 0, Math.Max(GetOptionCount(hasQuickSave, manualSaveCount) - 1, 0));
        }

        public int GetCreateNavigationIndex(int moveX, int moveY)
        {
            if (moveY < 0)
            {
                return GetCreateUpIndex(SelectedIndex);
            }

            if (moveY > 0)
            {
                return GetCreateDownIndex(SelectedIndex);
            }

            if (moveX < 0)
            {
                return GetCreateLeftIndex(SelectedIndex);
            }

            return moveX > 0 ? GetCreateRightIndex(SelectedIndex) : SelectedIndex;
        }

        public int GetLoadNavigationIndex(int moveX, int moveY, int saveCount)
        {
            var backIndex = GetLoadBackIndex(saveCount);
            if (moveY != 0)
            {
                if (saveCount == 0)
                {
                    return backIndex;
                }

                if (SelectedIndex >= backIndex)
                {
                    return moveY < 0 ? GetLoadSaveIndex(saveCount - 1) : backIndex;
                }

                var row = SelectedIndex / 2;
                if (moveY < 0)
                {
                    return row <= 0 ? GetLoadSaveIndex(0) : GetLoadSaveIndex(row - 1);
                }

                return row >= saveCount - 1 ? backIndex : GetLoadSaveIndex(row + 1);
            }

            if (moveX == 0 || SelectedIndex >= backIndex)
            {
                return SelectedIndex;
            }

            var saveIndex = SelectedIndex / 2;
            return moveX > 0 ? GetLoadDeleteIndex(saveIndex) : GetLoadSaveIndex(saveIndex);
        }

        public bool CanCycleCreateSelection()
        {
            return SelectedIndex == CreateGenderIndex ||
                   SelectedIndex == CreateClassIndex ||
                   SelectedIndex == CreateImageIndex;
        }

        public void CycleCreateGender(int delta)
        {
            var values = Enum.GetValues(typeof(Gender));
            var currentIndex = Array.IndexOf(values, CreatePlayerGender);
            var nextIndex = WrapIndex(currentIndex + delta, values.Length);
            CreatePlayerGender = (Gender)values.GetValue(nextIndex);
        }

        public void CycleCreateClass(int delta)
        {
            var values = Enum.GetValues(typeof(Class));
            var currentIndex = Array.IndexOf(values, CreatePlayerClass);
            var nextIndex = WrapIndex(currentIndex + delta, values.Length);
            CreatePlayerClass = (Class)values.GetValue(nextIndex);
        }

        public void CycleCreateImage(int delta, int heroCharacterCount)
        {
            CreatePlayerSpriteIndex = GetNextSelectableCreateImageIndex(CreatePlayerSpriteIndex, delta, heroCharacterCount);
        }

        public void EnsureSelectableCreateImageIndex(int heroCharacterCount)
        {
            if (!IsSelectableCreateImageIndex(CreatePlayerSpriteIndex))
            {
                CreatePlayerSpriteIndex = GetNextSelectableCreateImageIndex(CreatePlayerSpriteIndex, 1, heroCharacterCount);
            }
        }

        public int ClampDropdownIndex(int count)
        {
            SelectedDropdownIndex = Clamp(SelectedDropdownIndex, 0, Math.Max(count - 1, 0));
            return SelectedDropdownIndex;
        }

        public static int GetCreateUpIndex(int index)
        {
            switch (index)
            {
                case CreateGenderIndex:
                    return CreateNameIndex;
                case CreateClassIndex:
                    return CreateGenderIndex;
                case CreateRerollIndex:
                    return CreateImageIndex;
                case CreateImageIndex:
                    return CreateClassIndex;
                case CreateStartIndex:
                case CreateBackIndex:
                    return CreateRerollIndex;
                default:
                    return index;
            }
        }

        public static int GetCreateDownIndex(int index)
        {
            switch (index)
            {
                case CreateNameIndex:
                    return CreateGenderIndex;
                case CreateGenderIndex:
                    return CreateClassIndex;
                case CreateClassIndex:
                    return CreateImageIndex;
                case CreateImageIndex:
                case CreateGenerateNameIndex:
                    return CreateRerollIndex;
                case CreateRerollIndex:
                    return CreateStartIndex;
                default:
                    return index;
            }
        }

        public static int GetCreateLeftIndex(int index)
        {
            switch (index)
            {
                case CreateGenerateNameIndex:
                    return CreateNameIndex;
                case CreateBackIndex:
                    return CreateStartIndex;
                default:
                    return index;
            }
        }

        public static int GetCreateRightIndex(int index)
        {
            switch (index)
            {
                case CreateNameIndex:
                    return CreateGenerateNameIndex;
                case CreateStartIndex:
                    return CreateBackIndex;
                default:
                    return index;
            }
        }

        public static int GetNextSelectableCreateImageIndex(int currentIndex, int delta, int heroCharacterCount)
        {
            if (heroCharacterCount <= 0)
            {
                return 0;
            }

            var step = delta == 0 ? 1 : delta;
            var nextIndex = currentIndex;
            for (var i = 0; i < heroCharacterCount; i++)
            {
                nextIndex = WrapIndex(nextIndex + step, heroCharacterCount);
                if (IsSelectableCreateImageIndex(nextIndex))
                {
                    return nextIndex;
                }
            }

            return 0;
        }

        public static bool IsSelectableCreateImageIndex(int index)
        {
            return index != FirstBlockedCreateSpriteIndex &&
                   index != SecondBlockedCreateSpriteIndex;
        }

        public static int GetLoadSaveIndex(int saveIndex)
        {
            return saveIndex * 2;
        }

        public static int GetLoadDeleteIndex(int saveIndex)
        {
            return saveIndex * 2 + 1;
        }

        public static int GetLoadBackIndex(int saveCount)
        {
            return saveCount * 2;
        }

        public static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            while (index < 0)
            {
                index += count;
            }

            return index % count;
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
