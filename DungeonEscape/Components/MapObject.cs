using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class MapObject: Component
    {
        private readonly TmxObject tmxObject;
        private readonly int gridTileHeight;
        private readonly int gridTileWidth;
        private readonly TmxTilesetTile mapTile;
        private SpriteAnimator animator;

        public MapObject(TmxObject tmxObject,int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile)
        {
            this.tmxObject = tmxObject;
            this.gridTileHeight = gridTileHeight;
            this.gridTileWidth = gridTileWidth;
            this.mapTile = mapTile;
        }

        public override void Initialize()
        {
            base.Initialize();

            this.Entity.SetPosition(this.tmxObject.X + (int) (gridTileWidth / 2.0),
                this.tmxObject.Y - (int) (gridTileHeight / 2.0));
            
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(mapTile.Image.Texture, 32, 32);
            this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));

            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this.tmxObject,
                new Rectangle
                {
                    X = (int)(-this.tmxObject.Width/2.0f), 
                    Y = (int)(-this.tmxObject.Height/2.0f), 
                    Width = (int) this.tmxObject.Width,
                    Height = (int) this.tmxObject.Height
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
                X = (int)(-this.tmxObject.Width/2.0f) + offsetWidth / 2, 
                Y = (int)(-this.tmxObject.Height/2.0f) + offsetHeight / 2,
                Width = (int) this.tmxObject.Width - offsetWidth, 
                Height = (int) this.tmxObject.Height - offsetHeight
            }));
        }
    }
}