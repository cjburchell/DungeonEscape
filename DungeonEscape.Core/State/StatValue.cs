using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Redpoint.DungeonEscape.State
{
    public class StatValue
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public StatType Type { get; set; }
        public int Value { get; set; }
    }
}