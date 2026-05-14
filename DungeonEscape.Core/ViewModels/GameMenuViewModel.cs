namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class GameMenuViewModel
    {
        public int CurrentScreen { get; private set; }
        public int PreviousScreen { get; private set; }
        public int CurrentFocus { get; private set; }
        public int CurrentTab { get; private set; }
        public int CurrentSettingsTab { get; private set; }
        public int CurrentPartyDetailTab { get; private set; }
        public int SelectedHeroIndex { get; private set; }
        public int SelectedPartyItemIndex { get; private set; }
        public int SelectedDetailIndex { get; private set; }
        public int SelectedEquipmentItemIndex { get; private set; }
        public int SelectedMainActionIndex { get; private set; }
        public int SelectedPreviousScreenRowIndex { get; private set; }
        public int DetailPageIndex { get; private set; }
        public int SelectedRowIndex { get; private set; }
        public int SelectedBindingSlotIndex { get; private set; }

        public void Reset()
        {
            CurrentScreen = 0;
            PreviousScreen = 0;
            CurrentFocus = 0;
            CurrentTab = 0;
            CurrentSettingsTab = 0;
            CurrentPartyDetailTab = 0;
            SelectedHeroIndex = 0;
            SelectedPartyItemIndex = 0;
            SelectedDetailIndex = 0;
            SelectedEquipmentItemIndex = 0;
            SelectedMainActionIndex = 0;
            SelectedPreviousScreenRowIndex = 0;
            DetailPageIndex = 0;
            SelectedRowIndex = 0;
            SelectedBindingSlotIndex = 0;
        }

        public int ClampSelectedRowIndex(int rowCount)
        {
            SelectedRowIndex = Clamp(SelectedRowIndex, 0, rowCount <= 0 ? 0 : rowCount - 1);
            return SelectedRowIndex;
        }

        public int ClampSelectedDetailIndex(int detailCount)
        {
            SelectedDetailIndex = Clamp(SelectedDetailIndex, 0, detailCount <= 0 ? 0 : detailCount - 1);
            return SelectedDetailIndex;
        }

        public int ClampSelectedEquipmentItemIndex(int itemCount)
        {
            SelectedEquipmentItemIndex = Clamp(SelectedEquipmentItemIndex, 0, itemCount <= 0 ? 0 : itemCount - 1);
            return SelectedEquipmentItemIndex;
        }

        public int ClampSelectedMainActionIndex(int actionCount)
        {
            SelectedMainActionIndex = Clamp(SelectedMainActionIndex, 0, actionCount <= 0 ? 0 : actionCount - 1);
            return SelectedMainActionIndex;
        }

        public int MoveSelectedRowIndex(int delta, int rowCount)
        {
            SelectedRowIndex = rowCount <= 0 ? 0 : Clamp(SelectedRowIndex + delta, 0, rowCount - 1);
            return SelectedRowIndex;
        }

        public int MoveSelectedDetailIndex(int delta, int detailCount, int pageSize)
        {
            SelectedDetailIndex = detailCount <= 0 ? 0 : Clamp(SelectedDetailIndex + delta, 0, detailCount - 1);
            DetailPageIndex = pageSize <= 0 ? 0 : SelectedDetailIndex / pageSize;
            return SelectedDetailIndex;
        }

        public int MoveSelectedEquipmentItemIndex(int delta, int itemCount)
        {
            SelectedEquipmentItemIndex = itemCount <= 0 ? 0 : Clamp(SelectedEquipmentItemIndex + delta, 0, itemCount - 1);
            return SelectedEquipmentItemIndex;
        }

        public int SelectDetailPage(int pageIndex, int detailCount, int pageSize)
        {
            DetailPageIndex = pageIndex < 0 ? 0 : pageIndex;
            SelectedDetailIndex = detailCount <= 0 ? 0 : Clamp(DetailPageIndex * pageSize, 0, detailCount - 1);
            return SelectedDetailIndex;
        }

        public void SetCurrentScreen(int value)
        {
            CurrentScreen = value;
        }

        public void SetPreviousScreen(int value)
        {
            PreviousScreen = value;
        }

        public void SetCurrentFocus(int value)
        {
            CurrentFocus = value;
        }

        public void SetCurrentTab(int value)
        {
            CurrentTab = value;
        }

        public void SetCurrentSettingsTab(int value)
        {
            CurrentSettingsTab = value;
        }

        public void SetCurrentPartyDetailTab(int value)
        {
            CurrentPartyDetailTab = value;
        }

        public void SetSelectedHeroIndex(int value)
        {
            SelectedHeroIndex = value;
        }

        public void SetSelectedPartyItemIndex(int value)
        {
            SelectedPartyItemIndex = value;
        }

        public void SetSelectedDetailIndex(int value)
        {
            SelectedDetailIndex = value;
        }

        public void SetSelectedEquipmentItemIndex(int value)
        {
            SelectedEquipmentItemIndex = value;
        }

        public void SetSelectedMainActionIndex(int value)
        {
            SelectedMainActionIndex = value;
        }

        public void SetSelectedPreviousScreenRowIndex(int value)
        {
            SelectedPreviousScreenRowIndex = value;
        }

        public void SetDetailPageIndex(int value)
        {
            DetailPageIndex = value;
        }

        public void SetSelectedRowIndex(int value)
        {
            SelectedRowIndex = value;
        }

        public void SetSelectedBindingSlotIndex(int value)
        {
            SelectedBindingSlotIndex = value;
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
