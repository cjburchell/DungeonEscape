// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Redpoint.DungeonEscape.State
{
    public class Choice
    {
        public string Text { get; set; }
        public List<DialogText> Dialogs { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public QuestAction Action { get; set; } = QuestAction.None;

        public List<string> Items { get; set; }
        
        public string ItemId { get; set; }
        
        public int? MonsterId { get; set; }
        public int? NextQuestStage { get; set; }
        public int? MapId { get; set; }
        public int? SpawnId { get; set; }
    }
}