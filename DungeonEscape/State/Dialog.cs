// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Newtonsoft.Json.Converters;

    public enum QuestAction
    {
        None,
        GiveItem,
        LookingForItem,
        Fight,
        Warp,
        Join
    }
    
    public class Dialog
    {

        public int? Id { get; set; }
        public int? Quest { get; set; }
        public List<DialogText> Dialogs { get; set; }
    }
    
    public class DialogText
    {
        public int? ForQuestStage { get; set; }
        public string Text { get; set; }
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public string Text { get; set; }
        public List<DialogText> Dialogs { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public QuestAction Action { get; set; } = QuestAction.None;

        public int? ItemId { get; set; }
        
        public int? MonsterId { get; set; }
        public int? NextQuestStage { get; set; }
        public int? MapId { get; set; }
        public int? SpawnId { get; set; }
    }
}