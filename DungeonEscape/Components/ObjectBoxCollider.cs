using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public interface ICollidable
    {
        void OnHit(Player player);
        bool OnAction(Player player);
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