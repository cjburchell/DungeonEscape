﻿namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Nez.Tiled;
    using State;

    public class Warp : MapObject
    {
        private readonly string _mapId;
        private readonly string _spawnId;

        public Warp(TmxObject tmxObject, ObjectState state, TmxMap map, IGame gameState) : base(tmxObject, state,  map, gameState)
        {
            if (tmxObject.Properties.ContainsKey("WarpMap"))
            {
                this._mapId = tmxObject.Properties["WarpMap"];
            }
            
            if (tmxObject.Properties.ContainsKey("SpawnId"))
            {
                this._spawnId = tmxObject.Properties["SpawnId"];
            }
        }

        public override void OnHit()
        {
            this.GameState.Sounds.PlaySoundEffect("stairs-up");
            this.GameState.IsPaused = true;
            this.GameState.SetMap(this._mapId, this._spawnId);
        }
    }
}