using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GameFile
{
    public class Sprite
    {
        public Point StartPosition { get; set; } = new Point();
        public int Id { get; set; }
        
        public int State { get; set; }
        
        public bool CanMove { get; set; }
        public string Text { get; set; }
        
        public bool Collideable = true;
        
        public Warp Warp { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public SpriteType Type { get; set; }

        public string Name { get; set; }
    }
}