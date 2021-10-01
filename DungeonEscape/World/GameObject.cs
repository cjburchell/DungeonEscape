namespace DungeonEscape.World
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public abstract class GameObject
    {
        public IVisual Visual;
        public Vector2 Location { get; set; }
        
        public bool Collideable = false;

        public virtual void Update(GameTime gameTime)
        {
        }
        
        public virtual void Draw(SpriteBatch spriteBatch, int depth = 0)
        {
            this.Visual.Draw(spriteBatch, Location, depth);
        }
        
        public virtual Rectangle BoundingBox =>
            new Rectangle(
                (int)Location.X,
                (int)Location.Y,
                Visual.Width,
                Visual.Height);
    }
}