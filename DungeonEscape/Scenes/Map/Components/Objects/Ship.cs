using DungeonEscape.State;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Ship : Warp
    {
        public Ship(TmxObject tmxObject, ObjectState state, int gridTileHeight, int gridTileWidth, TmxMap map, IGame gameState) : base(tmxObject, state, gridTileHeight, gridTileWidth, map, gameState)
        {
        }

        public override void OnHit(Party party)
        {
            party.HasShip = true;
            base.OnHit(party);
        }
    }
}