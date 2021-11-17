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
        public List<Stats> Stats { get; set; } = new List<Stats>();
        public int FirstLevel { get; set; }
        public int NextLevelFactor { get; set; }
        public int NextLevelRandomPercent { get; set; }
    }
}