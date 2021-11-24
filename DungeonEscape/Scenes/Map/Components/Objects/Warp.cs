namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Nez.Tiled;
    using State;

    public class Warp : MapObject
    {
        private readonly int _mapId;
        private readonly int? _spawnId;

        public Warp(TmxObject tmxObject, ObjectState state, TmxMap map, IGame gameState) : base(tmxObject, state,  map, gameState)
        {
            if (tmxObject.Properties.ContainsKey("WarpMap"))
            {
                this._mapId = int.Parse(tmxObject.Properties["WarpMap"]);
            }
            
            if (tmxObject.Properties.ContainsKey("SpawnId"))
            {
                this._spawnId = int.Parse(tmxObject.Properties["SpawnId"]);
            }
        }

        public override void OnHit(Party party)
        {
            this.GameState.IsPaused = true;
            this.GameState.SetMap(this._mapId, this._spawnId);
        }
    }
}