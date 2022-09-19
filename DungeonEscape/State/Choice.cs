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
        
        public DialogText Dialog { get; set; }

        [JsonProperty("Actions", ItemConverterType = typeof(StringEnumConverter))]
        public List<QuestAction> Actions { get; set; } = new();

        // ReSharper disable once CollectionNeverUpdated.Global
        public List<string> Items { get; set; }
        
        public string ItemId { get; set; }
        
        public int? MonsterId { get; set; }
        public int? NextQuestStage { get; set; }
        public string MapId { get; set; }
        public string SpawnId { get; set; }
        public int? ObjectId { get; set; }
    }
}