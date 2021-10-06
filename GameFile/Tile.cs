namespace GameFile
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class Tile
    {
        public int Id { get; set; }
        public Point Position { get; set; } = new Point();

        [JsonConverter(typeof(StringEnumConverter))]
        public TileType Type { get; set; }
    }
}