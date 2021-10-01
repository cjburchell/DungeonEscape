namespace GameFile
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class Tile
    {
        public string Id { get; set; }
        public Point Position { get; set; } = new Point();

        public Warp Warp { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public TileType Type { get; set; }
    }
}