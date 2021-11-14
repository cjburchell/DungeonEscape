using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DungeonEscape.State
{
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