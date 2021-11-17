namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Microsoft.Xna.Framework;
    using Nez.Tiled;
    using State;

    public class Warp : MapObject
    {
        private readonly int _mapId;
        private readonly Point? _warpMap;
        public Warp(TmxObject tmxObject, ObjectState state, TmxMap map, IGame gameState) : base(tmxObject, state,  map, gameState)
        {
            if (tmxObject.Properties.ContainsKey("WarpMap"))
            {
                this._mapId = int.Parse(tmxObject.Properties["WarpMap"]);
            }

            if (tmxObject.Properties.ContainsKey("WarpMapX") &&
                tmxObject.Properties.ContainsKey("WarpMapY"))
            {
                this._warpMap = new Point
                {
                    X = int.Parse(tmxObject.Properties["WarpMapX"]), 
                    Y = int.Parse(tmxObject.Properties["WarpMapY"])
                };
            }
        }

        public override void OnHit(Party party)
        {
            var point = this._warpMap;
            if (!point.HasValue)
            {
                if (this._mapId == 0 && party.OverWorldPosition != Point.Zero)
                {
                    point = party.OverWorldPosition;
                }
            }
            
            this.GameState.IsPaused = true;
            this.GameState.SetMap(this._mapId, point);
        }
    }
}