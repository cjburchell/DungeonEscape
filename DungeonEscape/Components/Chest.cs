using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class Chest : MapObject
    {
        private readonly int level;
        private SpriteAnimator openImage;

        private bool isOpen
        {
            get =>
                this.tmxObject.Properties.ContainsKey("IsOpen") &&
                bool.Parse(this.tmxObject.Properties["IsOpen"]);

            set => this.tmxObject.Properties["IsOpen"] = value.ToString();
        }

        public Chest(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile)
        {
            this.level = tmxObject.Properties.ContainsKey("ChestLevel") ? int.Parse(tmxObject.Properties["ChestLevel"]) : 0;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.isOpen);
            var texture = Texture2D.FromFile(Core.GraphicsDevice, "Content/images/sprites/ochest.png");
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, 32, 32);
            this.openImage = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            this.openImage.LayerDepth = 11;
            this.openImage.SetEnabled(this.isOpen);
        }

        public override bool OnAction(Player player)
        {
            if (this.isOpen || !player.CanOpenChest(this.level))
            {
                return false;
            }
            
            this.isOpen = true;
            this.DisplayVisual(!this.isOpen);
            this.openImage.SetEnabled(this.isOpen);
            return true;
        }
    }
}