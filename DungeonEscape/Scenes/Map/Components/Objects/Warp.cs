using Microsoft.Xna.Framework;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Warp : MapObject
    {
        private readonly int mapId;
        private Point? warpMap;
        public Warp(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile)
        {
            if (tmxObject.Properties.ContainsKey("WarpMap"))
            {
                this.mapId = int.Parse(tmxObject.Properties["WarpMap"]);
            }

            if (tmxObject.Properties.ContainsKey("WarpMapX") &&
                tmxObject.Properties.ContainsKey("WarpMapY"))
            {
                this.warpMap = new Point
                {
                    X = int.Parse(tmxObject.Properties["WarpMapX"]), 
                    Y = int.Parse(tmxObject.Properties["WarpMapY"])
                };
            }
        }

        public override void OnHit(Player player)
        {
            var point = this.warpMap;
            if (!point.HasValue)
            {
                if (this.mapId == 0 && player.GameState.Player.OverWorldPos != Point.Zero)
                {
                    point = player.GameState.Player.OverWorldPos;
                }
            }
            
            player.GameState.IsPaused = true;
            MapScene.SetMap(this.mapId, point);
        }
    }
}