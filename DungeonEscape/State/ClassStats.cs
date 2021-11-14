namespace DungeonEscape.State
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

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