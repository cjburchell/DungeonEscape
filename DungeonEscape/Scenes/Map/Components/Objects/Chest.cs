using DungeonEscape.Scenes.Map.Components.UI;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Chest : MapObject
    {
        private readonly TalkWindow talkWindow;
        private readonly int level;
        private SpriteAnimator openImage;
        private string openImageName;

        private bool isOpen
        {
            get =>
                this.tmxObject.Properties.ContainsKey("IsOpen") &&
                bool.Parse(this.tmxObject.Properties["IsOpen"]);

            set => this.tmxObject.Properties["IsOpen"] = value.ToString();
        }

        public Chest(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, TalkWindow talkWindow) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile)
        {
            this.talkWindow = talkWindow;
            this.level = tmxObject.Properties.ContainsKey("ChestLevel") ? int.Parse(tmxObject.Properties["ChestLevel"]) : 0;
            this.openImageName = tmxObject.Properties.ContainsKey("OpenImage") ? tmxObject.Properties["OpenImage"] : "ochest.png";
        }
        
        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.isOpen);
            var texture = Texture2D.FromFile(Core.GraphicsDevice, $"Content/images/sprites/{this.openImageName}");
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, 32, 32);
            this.openImage = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            this.openImage.RenderLayer = 15;
            this.openImage.LayerDepth = 15;
            this.openImage.SetEnabled(this.isOpen);
        }

        public override bool OnAction(Player player)
        {
            if (isOpen)
            {
                this.talkWindow.ShowText($"You found nothing");
                return false;
            }
            
            if (!player.CanOpenChest(this.level))
            {
                this.talkWindow.ShowText($"Unable to open chest");
                return false;
            }
            
            this.isOpen = true;
            this.DisplayVisual(!this.isOpen);
            this.openImage.SetEnabled(this.isOpen);

            this.talkWindow.ShowText($"You found {100} Gold");
            return true;
        }
    }
}