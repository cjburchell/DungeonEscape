namespace Redpoint.DungeonEscape.Scenes.Map.Components
{
    using Microsoft.Xna.Framework;
    using Nez;

    public class CameraBounds : Component, IUpdatable
    {
        private readonly Vector2 _min;
        private readonly Vector2 _max;


        private CameraBounds()
        {
            // make sure we run last so the camera is already moved before we evaluate its position
            this.SetUpdateOrder(int.MaxValue);
        }


        public CameraBounds(Vector2 min, Vector2 max) : this()
        {
            this._min = min;
            this._max = max;
        }


        public override void OnAddedToEntity()
        {
            this.Entity.UpdateOrder = int.MaxValue;
        }


        void IUpdatable.Update()
        {
            var cameraBounds = this.Entity.Scene.Camera.Bounds;

            if (cameraBounds.Top < this._min.Y)
                this.Entity.Scene.Camera.Position += new Vector2(0, this._min.Y - cameraBounds.Top);

            if (cameraBounds.Left < this._min.X)
                this.Entity.Scene.Camera.Position += new Vector2(this._min.X - cameraBounds.Left, 0);

            if (cameraBounds.Bottom > this._max.Y)
                this.Entity.Scene.Camera.Position += new Vector2(0, this._max.Y - cameraBounds.Bottom);

            if (cameraBounds.Right > this._max.X)
                this.Entity.Scene.Camera.Position += new Vector2(this._max.X - cameraBounds.Right, 0);
        }
    }
}