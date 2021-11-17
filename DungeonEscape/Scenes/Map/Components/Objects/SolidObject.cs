namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.Tiled;
    using State;

    public class SolidObject : MapObject
    {
        private readonly Rectangle _collideRect;
        private BoxCollider _boxCollider;
        
        protected bool Collideable
        {
            get => this.State.Collideable != null && this.State.Collideable.Value;
            set => this.State.Collideable = value;
        }
        
        protected SolidObject(TmxObject tmxObject, ObjectState state, TmxMap map, IGame gameState) : base(tmxObject, state, map, gameState)
        {
            state.Collideable ??= bool.Parse(this.TmxObject.Properties["Collideable"]);
            
            var offsetWidth = (int) (tmxObject.Width * (1.0f / 4.0f));
            var offsetHeight = (int) (tmxObject.Height * (1.0f / 4.0f));
            this._collideRect = new Rectangle
            {
                X = (int) (-tmxObject.Width / 2.0f) + offsetWidth / 2,
                Y = (int) (-tmxObject.Height / 2.0f) + offsetHeight / 2,
                Width = (int) tmxObject.Width - offsetWidth,
                Height = (int) tmxObject.Height - offsetHeight
            };
        }

        protected void SetEnableCollider(bool enable)
        {
            this._boxCollider?.SetEnabled(enable);
        }

        public override void Initialize()
        {
            base.Initialize();
            
            if (!this.Collideable)
            {
                return;
            }

            this._boxCollider = this.Entity.AddComponent(new BoxCollider(this._collideRect));
        }
    }
}