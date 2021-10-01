namespace GameFile
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class Monster
    {
        public string Id { get; set; }
        public int Chance { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Biome Biome { get; set; }
    }
}