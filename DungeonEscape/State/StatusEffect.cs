﻿// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

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

        public bool IsNegativeEffect =>
            Type is EffectType.Buff or EffectType.OverTime && StatValue <= 0 ||
            Type is EffectType.Confusion or EffectType.Sleep or EffectType.StopSpell;
    }
}