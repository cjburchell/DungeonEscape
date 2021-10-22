using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Map.Components
{
    public interface ICollidable
    {
        void OnHit(Party party);
        bool OnAction(Party party);
    }
    
    public class ObjectBoxCollider : BoxCollider
    {
        public ICollidable Object { get; private set; }

        public ObjectBoxCollider(ICollidable collidable, Rectangle rectangle) : base(rectangle)
        {
            this.Object = collidable;
        }
    }
}