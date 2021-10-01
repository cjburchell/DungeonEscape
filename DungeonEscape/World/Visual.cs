namespace DungeonEscape.World
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public interface IVisual
    {
        void Draw(SpriteBatch spriteBatch, Vector2 location, int depth);
        string Id { get; }
        
        int Width { get; }
        int Height { get; }
    }

    public class Image : IVisual
    {
        public Texture2D Texture;

        public void Draw(SpriteBatch spriteBatch, Vector2 location, int depth)
        {
            spriteBatch.Draw(this.Texture, location, null,Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
        }

        public string Id { get; set; }
        public int Width => this.Texture.Width;
        public int Height => this.Texture.Height;
    }
}