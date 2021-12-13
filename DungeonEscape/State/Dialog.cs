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
        GetItem,
        LookingForItem
    }

    public class Dialog
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int? Id { get; set; }
        public string Text { get; set; }
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public string Text { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public Dialog Dialog { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public QuestAction Action { get; set; } = QuestAction.None;

        public int? ItemId { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int? QuestStage { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int? QuestId { get; set; }
    }
}