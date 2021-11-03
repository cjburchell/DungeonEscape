using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GameFile
{
    public enum SpellType
    {
        Heal = 0,
        Outside = 1,
        Fireball = 2,
        Lighting =  3
    }
    
    public class Spell
    {
        public string Name { get; set; }
        
        [JsonIgnore]
        public TileInfo Info { get; set; }

        public int Id => this.Info.Id;
        
        [JsonConverter(typeof(StringEnumConverter))]
        public SpellType Type { get; set; }
        public int Power { get; set; }
        public int Cost { get; set; }
        public int MinLevel { get; set; }
    }
}