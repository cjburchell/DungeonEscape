using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Warp : MapObject
    {
        private readonly int mapId;
        private Point? warpMap;
        public Warp(TmxObject tmxObject, ObjectState state, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, IGame gameState) : base(tmxObject, state, gridTileHeight, gridTileWidth, mapTile, gameState)
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

        public override void OnHit(Party party)
        {
            var point = this.warpMap;
            if (!point.HasValue)
            {
                if (this.mapId == 0 && party.OverWorldPosition != Point.Zero)
                {
                    point = party.OverWorldPosition;
                }
            }
            
            this.gameState.IsPaused = true;
            MapScene.SetMap(this.gameState, this.mapId, point);
        }
    }
}