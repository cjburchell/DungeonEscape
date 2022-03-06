namespace Redpoint.DungeonEscape.State
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public enum EffectType
    {
        OverTime,
        Sleep,
        Confusion,
        StopSpell,
        Buff
    }

    public enum DurationType
    {
        Distance,
        Rounds
    }

    public class StatusEffect
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public EffectType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public StatType StatType { get; set; } = StatType.None;

        public int StatValue { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DurationType DurationType { get; set; }
        public int Duration { get; set; }
        public string Name { get; set; }
        public int StartTime { get; set; }
    }
}