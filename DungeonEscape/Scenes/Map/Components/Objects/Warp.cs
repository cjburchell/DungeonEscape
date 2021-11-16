namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using Microsoft.Xna.Framework;
    using Nez.Tiled;
    using State;

    public class Warp : MapObject
    {
        private readonly int mapId;
        private readonly Point? warpMap;
        public Warp(TmxObject tmxObject, ObjectState state, TmxMap map, IGame gameState) : base(tmxObject, state,  map, gameState)
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
            this.gameState.SetMap(this.mapId, point);
        }
    }
}