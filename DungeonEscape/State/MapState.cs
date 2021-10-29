namespace DungeonEscape.State
{
    using System.Collections.Generic;

    public class MapState
    {
        public int Id { get; set; }
        public List<ObjectState> Objects { get; set; } = new List<ObjectState>();
        
        public List<SpriteState> Sprites { get; set; } = new List<SpriteState>();
    }
}