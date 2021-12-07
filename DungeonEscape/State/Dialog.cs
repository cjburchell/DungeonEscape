// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Newtonsoft.Json.Converters;

    public enum QuestAction
    {
        None,
        StartQuest,
        GiveItem,
        GetItem
    }

    public class Dialog
    {
        public int? Id { get; set; }
        public string Text { get; set; }
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public string Text { get; set; }
        public Dialog Dialog { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public QuestAction Action { get; set; } = QuestAction.None;
        
        public int? ItemId { get; set; }
        public int? QuestId { get; set; }
    }
}