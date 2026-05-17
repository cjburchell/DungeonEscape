using System.Collections.Generic;

namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class GameMenuSaveModalData
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public List<string> Choices { get; set; }
        public List<GameMenuSaveAction> Actions { get; set; }

        public GameMenuSaveAction GetAction(int index)
        {
            return Actions != null && index >= 0 && index < Actions.Count
                ? Actions[index]
                : GameMenuSaveAction.None;
        }
    }
}
