// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;

    public class MapState
    {
        public string Id { get; set; }
        public List<ObjectState> Objects { get; set; } = new();
        
        public List<SpriteState> Sprites { get; set; } = new();
    }
}