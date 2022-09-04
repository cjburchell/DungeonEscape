// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ClassStats
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Class Class { get; set; }
        public List<Stats> Stats { get; set; } = new();
        public ulong FirstLevel { get; set; }

        public List<string> Skills { get; set; } = new();
    }
}