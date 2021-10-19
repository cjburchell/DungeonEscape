using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Ship : Warp
    {
        public Ship(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile)
        {
        }

        public override void OnHit(Player player)
        {
            player.GameState.Player.HasShip = true;
            base.OnHit(player);
        }
    }
}