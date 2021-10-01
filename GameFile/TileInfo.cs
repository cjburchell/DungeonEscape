namespace GameFile
{
    using Newtonsoft.Json;

    public class TileInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Image { get; set; }
        
        [JsonIgnore]
        public int OldId { get; set; }
    }
}