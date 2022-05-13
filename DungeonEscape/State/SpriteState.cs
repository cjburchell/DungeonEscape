namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;

    public class SpriteState
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public string Name { get; set; }
        public List<Item> Items { get; set; }
    }
}