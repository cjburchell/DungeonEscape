using System.Collections.Generic;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Redpoint.DungeonEscape.State
{
    public class DialogText
    {
        public string Text { get; set; }
        public List<Choice> Choices { get; set; }
    }
    
    public class DialogHead : DialogText
    {
        public string Quest { get; set; }
        public List<int> QuestStage { get; set; }
        public bool StartQuest { get; set; }
    }
}