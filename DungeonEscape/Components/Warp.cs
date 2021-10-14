using DungeonEscape.Scenes;
using Microsoft.Xna.Framework;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class Warp : MapObject
    {
        private readonly int mapId;
        private int? warpMapX;
        private int? warpMapY;
        public Warp(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile)
        {
            this.mapId = int.Parse(tmxObject.Properties["WarpMap"]);
            if (tmxObject.Properties.ContainsKey("WarpMapX") &&
                tmxObject.Properties.ContainsKey("WarpMapY"))
            {
                this.warpMapX = int.Parse(tmxObject.Properties["WarpMapX"]);
                this.warpMapY = int.Parse(tmxObject.Properties["WarpMapY"]);
            }
        }

        public override void OnHit(Player player)
        {
            Vector2? point = null;
            if (this.warpMapX.HasValue && this.warpMapY.HasValue)
            {
                var map = player.GameState.GetMap(this.mapId);
                point = new Vector2()
                {
                    X = this.warpMapX.Value * map.TileHeight +
                        map.TileHeight / 2.0f,
                    Y = this.warpMapY.Value * map.TileWidth + map.TileWidth / 2.0f
                };
            }
            else
            {
                if (this.mapId == 0 && player.GameState.Player.OverWorldPos != Vector2.Zero)
                {
                    point = player.GameState.Player.OverWorldPos;
                }
            }

            player.IsControllable = false;
            MapScene.SetMap(this.mapId, point);
        }
    }
}