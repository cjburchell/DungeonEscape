
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Redpoint.DungeonEscape.State
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;


    public class RandomMonster
    {
        public int Id { get; set; }
        
        [JsonIgnore]
        public Monster Data { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Biome Biome { get; set; }
        public int Probability { get; set; }
    }
}