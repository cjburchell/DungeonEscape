using System;

namespace Redpoint.DungeonEscape.Scenes.Map.Components
{
    using State;

    public interface ICollidable
    {
        void OnHit();
        void OnAction(Action done);

        bool CanDoAction();
        
        BaseState State { get; }
    }
}