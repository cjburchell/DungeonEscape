using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Map.Components
{
    public class CameraBounds : Component, IUpdatable
    {
        public Vector2 Min, Max;


        public CameraBounds()
        {
            // make sure we run last so the camera is already moved before we evaluate its position
            this.SetUpdateOrder(int.MaxValue);
        }


        public CameraBounds(Vector2 min, Vector2 max) : this()
        {
            this.Min = min;
            this.Max = max;
        }


        public override void OnAddedToEntity()
        {
            this.Entity.UpdateOrder = int.MaxValue;
        }


        void IUpdatable.Update()
        {
            var cameraBounds = this.Entity.Scene.Camera.Bounds;

            if (cameraBounds.Top < this.Min.Y)
                this.Entity.Scene.Camera.Position += new Vector2(0, this.Min.Y - cameraBounds.Top);

            if (cameraBounds.Left < this.Min.X)
                this.Entity.Scene.Camera.Position += new Vector2(this.Min.X - cameraBounds.Left, 0);

            if (cameraBounds.Bottom > this.Max.Y)
                this.Entity.Scene.Camera.Position += new Vector2(0, this.Max.Y - cameraBounds.Bottom);

            if (cameraBounds.Right > this.Max.X)
                this.Entity.Scene.Camera.Position += new Vector2(this.Max.X - cameraBounds.Right, 0);
        }
    }
}