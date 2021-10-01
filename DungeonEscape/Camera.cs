namespace DungeonEscape
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class Camera
    {
        private float zoom = 1.0f;
        public Vector2 Pos = Vector2.Zero;
        public float Rotation = 0;

        public float Zoom
        {
            get => this.zoom;
            set
            {
                this.zoom = value;
                if (this.zoom < 0.1f) this.zoom = 0.1f;
            } // Negative zoom will flip image
        }

        public Matrix GetTransormation(Viewport viewport)
        {
            var transform = // Thanks to o KB o for this solution
                Matrix.CreateTranslation(new Vector3(-this.Pos.X, -this.Pos.Y, 0)) *
                Matrix.CreateRotationZ(this.Rotation) *
                Matrix.CreateScale(new Vector3(this.Zoom, this.Zoom, 1)) *
                Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));
            return transform;
        }
    }
}