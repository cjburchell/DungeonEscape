// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
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