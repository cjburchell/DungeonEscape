using System.Linq;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class SpriteComponent: Component
    {
        private readonly TmxObject tmxObject;
        private readonly int gridTileHeight;
        private readonly int gridTileWidth;
        private readonly TmxTilesetTile mapTile;
        private SpriteAnimator animator;

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
            var sprites =  Nez.Textures.Sprite.SpritesFromAtlas(mapTile.Image.Texture, 32, 32);
            this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            
            //var texture = this.tmxObject. this.tmxObject.Tile.Gid;
            
            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this.tmxObject, new Rectangle{X = (int) this.tmxObject.X, Y= (int)this.tmxObject.Y-gridTileHeight, Width = (int)this.tmxObject.Width, Height = (int)this.tmxObject.Height}));
            collider.IsTrigger = true;

            if (!bool.Parse(this.tmxObject.Properties["Collideable"]))
            {
                return;
            }

            var offsetWidth =(int)( this.tmxObject.Width * 0.25F);
            var offsetHeight =(int)( this.tmxObject.Height * 0.25F);
            this.Entity.AddComponent(new BoxCollider(new Rectangle{X = (int)this.tmxObject.X + offsetWidth/2, Y= (int)this.tmxObject.Y-gridTileWidth, Width = (int)this.tmxObject.Width-offsetWidth, Height = (int)this.tmxObject.Height - offsetHeight/2}));
        }
    }
}