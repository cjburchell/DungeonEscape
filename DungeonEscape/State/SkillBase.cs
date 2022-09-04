// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeProtected.Global
namespace Redpoint.DungeonEscape.State
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    
    public class SkillBase {
        public string Name { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public Target Targets { get; set; }
        public int MaxTargets { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public SkillType Type { get; set; }
        public bool IsPiercing { get; set; }
        public int StatConst { get; set; }
        public int StatTimes { get; set; } = 1;
        public int StatRandom { get; set; }
        public int DurationConst { get; set; }
        public int DurationTimes { get; set; } = 1;
        public int DurationRandom { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public DurationType DurationType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public StatType StatType { get; set; } = StatType.None;
        public string EffectName { get; set; }
        
        public override string ToString()
        {
            return this.Name;
        }
    }
}