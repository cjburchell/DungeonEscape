namespace Redpoint.DungeonEscape.State
{
    public class ObjectState
    {
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        public int Id { get; set; }
        public bool? Collideable { get; set; }
        public bool? IsOpen { get; set; }

        public Item Item { get; set; }

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public bool IsActive { get; set; } = true;
    }
}