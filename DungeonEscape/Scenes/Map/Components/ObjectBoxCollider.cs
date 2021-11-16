namespace DungeonEscape.Scenes.Map.Components
{
    using Microsoft.Xna.Framework;
    using Nez;

    public class ObjectBoxCollider : BoxCollider
    {
        public ICollidable Object { get; }

        public ObjectBoxCollider(ICollidable collidable, Rectangle rectangle) : base(rectangle)
        {
            this.Object = collidable;
        }
    }
}