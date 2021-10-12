using System.Linq;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class SpriteComponent : Component
    {
        private readonly TmxObject tmxObject;
        private readonly int gridTileHeight;
        private readonly int gridTileWidth;
        private readonly TmxTilesetTile mapTile;
        private SpriteAnimator animator;
        private Mover mover;

        public SpriteComponent(TmxObject tmxObject, int gridTileHeight, int gridTileWidth,
            TmxTilesetTile mapTile)
        {
            this.tmxObject = tmxObject;
            this.gridTileHeight = gridTileHeight;
            this.gridTileWidth = gridTileWidth;
            this.mapTile = mapTile;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.Entity.SetPosition(this.tmxObject.X + (int)(gridTileWidth/2.0), this.tmxObject.Y - (int)(gridTileHeight/2.0));
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(mapTile.Image.Texture, 32, 32);
            this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            this.mover = this.Entity.AddComponent(new Mover());

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

            var offsetWidth = (int) (this.tmxObject.Width * 0.25F);
            var offsetHeight = (int) (this.tmxObject.Height * 0.25F);
            this.Entity.AddComponent(new BoxCollider(new Rectangle
            {
                X = (int)(-this.tmxObject.Width/2.0f), 
                Y = (int)(-this.tmxObject.Height/2.0f), 
                Width = (int) this.tmxObject.Width - offsetWidth,
                Height = (int) this.tmxObject.Height - offsetHeight / 2
            }));
        }
    }
}