// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Redpoint.DungeonEscape.State
{
    public class StatName
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public StatType Type { get; set; }
        public List<string> Prefix { get; set; }
        public List<string> Suffix { get; set; }
    }
}