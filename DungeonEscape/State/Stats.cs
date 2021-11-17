// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Redpoint.DungeonEscape.State
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Nez;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Stats
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public StatType Type { get; set; }
        public int StartConst { get; set; }
        public int Roll { get; set; }
        public int RollConst { get; set; }

        public int RollStartValue()
        {
            return Random.NextInt(this.Roll) + this.StartConst;
        }
            
        public int RollNextValue()
        {
            return Random.NextInt(this.Roll) + this.RollConst;
        }
    }
}