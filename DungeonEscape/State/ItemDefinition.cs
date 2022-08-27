using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CollectionNeverUpdated.Global

namespace Redpoint.DungeonEscape.State
{
    public class ItemDefinition
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType Type { get; set; }
        
        [JsonProperty("Slots", ItemConverterType=typeof(StringEnumConverter))]
        public List<Slot> Slots { get; set; }
        
        [JsonProperty("Classes", ItemConverterType=typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }
        public List<ItemName> Names { get; set; }
        
    }
}