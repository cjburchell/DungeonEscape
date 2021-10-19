using System.Collections.Generic;
using System.Linq;
using DungeonEscape.Scenes.Map.Components.UI;
using DungeonEscape.State;
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
        private Item item;

        private bool isOpen
        {
            get =>
                this.tmxObject.Properties.ContainsKey("IsOpen") &&
                bool.Parse(this.tmxObject.Properties["IsOpen"]);

            set => this.tmxObject.Properties["IsOpen"] = value.ToString();
        }

        public Chest(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, TalkWindow talkWindow, IEnumerable<Item> items) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile)
        {
            this.talkWindow = talkWindow;
            this.level = tmxObject.Properties.ContainsKey("ChestLevel") ? int.Parse(tmxObject.Properties["ChestLevel"]) : 0;
            this.openImageName = tmxObject.Properties.ContainsKey("OpenImage") ? tmxObject.Properties["OpenImage"] : "ochest.png";

            if (tmxObject.Name == "Key Chest")
            {
                this.item = new Item("", "Key", ItemType.Key, 1);
            }
            else
            {
                if (Random.Chance(0.25f))
                {
                    var levelItems = items.Where(item => item.MinLevel <= this.level).ToArray();
                    var itemNumber = Random.NextInt(levelItems.Length);
                    this.item = levelItems[itemNumber];
                }
                else
                {
                    this.item = new Item("", "Gold", ItemType.Gold, Random.NextInt(100) + 20);
                }
            }
           
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


                if (this.item.Type == ItemType.Gold)
                {
                    this.talkWindow.ShowText($"You found {this.item.Gold} Gold");
                    player.GameState.Player.Gold += this.item.Gold;
                }
                else
                {
                    this.talkWindow.ShowText($"You found a {this.item.Name}");
                    player.GameState.Player.Items.Add(this.item);
                }

                return true;
        }
    }
}