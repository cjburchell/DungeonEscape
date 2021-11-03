using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GameFile
{
    public class Item
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType Type { get; set; }

        public string Name { get; set; }
        public int Defence { get; set; }
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Agility { get; set; }
        public int Cost { get; set; }
        
        [JsonIgnore]
        public TileInfo Info { get; set; }

        public int Id => this.Info.Id;
        public int MinLevel { get; set; }
    }
}