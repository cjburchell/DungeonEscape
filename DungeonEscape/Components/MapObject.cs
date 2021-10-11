using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class MapObject: Component
    {
        private readonly TmxObject tmxObject;
        private readonly int gridTileHeight;

        public MapObject(TmxObject tmxObject,int gridTileHeight)
        {
            this.tmxObject = tmxObject;
            this.gridTileHeight = gridTileHeight;
        }

        public override void Initialize()
        {
            base.Initialize();

            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this.tmxObject,
                new Rectangle
                {
                    X = (int) this.tmxObject.X, Y = (int) this.tmxObject.Y - gridTileHeight,
                    Width = (int) this.tmxObject.Width, Height = (int) this.tmxObject.Height
                }));
            collider.IsTrigger = true;
            if (!bool.Parse(this.tmxObject.Properties["Collideable"]))
            {
                return;
            }

            var offsetWidth = (int) (this.tmxObject.Width * (1.0f / 4.0f));
            var offsetHeight = (int) (this.tmxObject.Height * (1.0f / 4.0f));
            this.Entity.AddComponent(new BoxCollider(new Rectangle
            {
                X = (int) this.tmxObject.X + offsetWidth / 2,
                Y = (int) this.tmxObject.Y - gridTileHeight + offsetHeight / 2,
                Width = (int) this.tmxObject.Width - offsetWidth, Height = (int) this.tmxObject.Height - offsetHeight
            }));
        }
    }
}