using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GameFile
{
    public class Monster
    {
        public int Id { get; set; }
        public int Chance { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Biome Biome { get; set; }
        
        public int Heath { get; set; }
        public int HeathConst { get; set; }
        public int Attack { get; set; }
        public int XP { get; set; }
        public int Gold { get; set; }
        public List<SpriteSpell> Spells { get; set; }
        public string Name { get; set; }
    }
}