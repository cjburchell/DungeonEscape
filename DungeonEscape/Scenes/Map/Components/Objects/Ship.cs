namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Nez.Tiled;
    using State;

    public class Ship : Warp
    {
        public Ship(TmxObject tmxObject, ObjectState state, TmxMap map, IGame gameState) : base(tmxObject, state, map, gameState)
        {
        }

        public override void OnHit(Party party)
        {
            party.HasShip = true;
            base.OnHit(party);
        }
    }
}