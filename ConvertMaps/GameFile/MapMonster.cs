namespace GameFile
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class MapMonster
    {
        public int Id { get; set; }
        public int Probability { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Biome Biome { get; set; }
    }
}