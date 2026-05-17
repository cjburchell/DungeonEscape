namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class TitleLoadSlotRow
    {
        public int SlotIndex { get; set; }
        public int LoadIndex { get; set; }
        public int DeleteIndex { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string ButtonText { get; set; }
        public string DeleteButtonText { get; set; }
        public bool LoadSelected { get; set; }
        public bool DeleteSelected { get; set; }
    }
}
