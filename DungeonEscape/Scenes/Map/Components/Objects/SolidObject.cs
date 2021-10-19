using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class SolidObject : MapObject
    {
        private readonly Rectangle collideRect;

        protected bool Collideable
        {
            get => bool.Parse(this.tmxObject.Properties["Collideable"]);
            set => this.tmxObject.Properties["Collideable"] = value.ToString();
        }

        private BoxCollider boxCollider;

        protected SolidObject(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, IGame gameState) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile, gameState)
        {
            var offsetWidth = (int) (tmxObject.Width * (1.0f / 4.0f));
            var offsetHeight = (int) (tmxObject.Height * (1.0f / 4.0f));
            this.collideRect = new Rectangle
            {
                X = (int) (-tmxObject.Width / 2.0f) + offsetWidth / 2,
                Y = (int) (-tmxObject.Height / 2.0f) + offsetHeight / 2,
                Width = (int) tmxObject.Width - offsetWidth,
                Height = (int) tmxObject.Height - offsetHeight
            };
        }

        public void SetEnableCollider(bool enable)
        {
            this.boxCollider?.SetEnabled(enable);
        }

        public override void Initialize()
        {
            base.Initialize();
            
            if (!this.Collideable)
            {
                return;
            }

            this.boxCollider = this.Entity.AddComponent(new BoxCollider(this.collideRect));
        }
    }
}