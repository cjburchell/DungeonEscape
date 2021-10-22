using DungeonEscape.State;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Ship : Warp
    {
        public Ship(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, IGame gameState) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile, gameState)
        {
        }

        public override void OnHit(Party party)
        {
            party.HasShip = true;
            base.OnHit(party);
        }
    }
}