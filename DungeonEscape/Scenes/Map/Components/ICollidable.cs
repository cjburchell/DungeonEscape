namespace Redpoint.DungeonEscape.Scenes.Map.Components
{
    using State;

    public interface ICollidable
    {
        void OnHit(Party party);
        bool OnAction(Party party);
    }
}