using System.Collections.Generic;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Redpoint.DungeonEscape.State
{
    public class DialogText
    {
        public List<int> ForQuestStage { get; set; }
        public string Text { get; set; }
        public List<Choice> Choices { get; set; }
    }
}