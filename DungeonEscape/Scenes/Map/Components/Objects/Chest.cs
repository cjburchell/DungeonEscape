namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using System.Linq;
    using Common.Components.UI;
    using Microsoft.Xna.Framework.Graphics;
    using Nez;
    using Nez.Sprites;
    using Nez.Tiled;
    using State;

    public class Chest : MapObject
    {
        private readonly UISystem ui;
        private readonly int level;
        private SpriteAnimator openImage;
        private readonly string openImageName;
        private readonly Item item;

        private bool isOpen
        {
            get => this.state.IsOpen != null && this.state.IsOpen.Value;
            set => this.state.IsOpen = value;
        }

        public Chest(TmxObject tmxObject, ObjectState state, TmxMap map, UISystem ui, IGame gameState) : base(tmxObject, state, map, gameState)
        {
            this.state.IsOpen ??= this.tmxObject.Properties.ContainsKey("IsOpen") &&
                                  bool.Parse(this.tmxObject.Properties["IsOpen"]);
            
            this.ui = ui;
            this.level = tmxObject.Properties.ContainsKey("ChestLevel") ? int.Parse(tmxObject.Properties["ChestLevel"]) : 0;
            this.openImageName = tmxObject.Properties.ContainsKey("OpenImage") ? tmxObject.Properties["OpenImage"] : "ochest.png";

            if (tmxObject.Name == "Key Chest")
            {
                this.item = new Item("Content/images/items/key.png", "Key", ItemType.Key, 250, 0);
            }
            else
            {
                if (Random.Chance(0.25f))
                {
                    var levelItems = gameState.Items.Where(item => item.MinLevel <= this.level).ToArray();
                    var itemNumber = Random.NextInt(levelItems.Length);
                    this.item = levelItems[itemNumber];
                }
                else
                {
                    this.item = new Item("", "Gold", ItemType.Gold, Random.NextInt(100) + 20, 0);
                }
            }
           
        }
        
        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.isOpen);
            var texture = Texture2D.FromFile(Core.GraphicsDevice, $"Content/images/sprites/{this.openImageName}");
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, MapScene.DefaultTileSize, MapScene.DefaultTileSize);
            this.openImage = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            this.openImage.RenderLayer = 15;
            this.openImage.LayerDepth = 15;
            this.openImage.SetEnabled(this.isOpen);
        }

        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            void Done()
            {
                this.gameState.IsPaused = false;
            }

            if (this.isOpen)
            {
                new TalkWindow(this.ui).Show("You found nothing", Done);
                return true;
            }

            if (!this.gameState.Party.CanOpenChest(this.level))
            {
                new TalkWindow(this.ui).Show("Unable to open chest", Done);
                return true;
            }
            
            if (this.item.Type == ItemType.Gold)
            {
                new TalkWindow(this.ui).Show($"You found {this.item.Gold} Gold", Done);
                party.Gold += this.item.Gold;
            }
            else
            {
                if (party.Items.Count >= Party.MaxItems)
                {
                    new TalkWindow(this.ui).Show($"You do not have enough space in your inventory for {this.item.Name}", Done);
                    return true;
                }

                new TalkWindow(this.ui).Show($"You found a {this.item.Name}", Done);
                party.Items.Add(new ItemInstance(this.item));
            }
            
            this.isOpen = true;
            this.DisplayVisual(!this.isOpen);
            this.openImage.SetEnabled(this.isOpen);

            return true;
        }
    }
}