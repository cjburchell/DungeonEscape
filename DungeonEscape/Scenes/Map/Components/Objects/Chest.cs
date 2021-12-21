namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
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
        private readonly UiSystem _ui;
        private readonly int _level;
        private SpriteAnimator _openImage;
        private readonly string _openImageName;
        private readonly Item _item;

        private bool IsOpen
        {
            get => this.State.IsOpen != null && this.State.IsOpen.Value;
            set => this.State.IsOpen = value;
        }

        public Chest(TmxObject tmxObject, ObjectState state, TmxMap map, UiSystem ui, IGame gameState) : base(tmxObject, state, map, gameState)
        {
            this.State.IsOpen ??= this.TmxObject.Properties.ContainsKey("IsOpen") &&
                                  bool.Parse(this.TmxObject.Properties["IsOpen"]);
            
            this._ui = ui;
            this._level = tmxObject.Properties.ContainsKey("ChestLevel") ? int.Parse(tmxObject.Properties["ChestLevel"]) : 0;
            this._openImageName = tmxObject.Properties.ContainsKey("OpenImage") ? tmxObject.Properties["OpenImage"] : "ochest.png";
            if (this.State.Gold.HasValue)
            {
                this._item = Item.CreateGold(this.State.Gold.Value);
                return;
            }
            
            if(this.State.ItemId.HasValue)
            {
                this._item = gameState.Items.First(item => item.Id == this.State.ItemId);
                return;
            }
            
            if (this.TmxObject.Properties.ContainsKey("ItemId"))
            {
                this.State.ItemId = int.Parse(tmxObject.Properties["ItemId"]);
                this._item = gameState.Items.First(item => item.Id == this.State.ItemId);
                return;
            }

            if (this.TmxObject.Properties.ContainsKey("Gold"))
            {
                this.State.Gold = int.Parse(tmxObject.Properties["Gold"]);
                this._item = Item.CreateGold(this.State.Gold.Value);
                return;
            }

            if (tmxObject.Name == "Key Chest")
            {
                this.State.ItemId = 26; // key item
                this._item = gameState.Items.First(item => item.Id == this.State.ItemId);
                return;
            }

            if (Random.Chance(0.25f))
            {
                var levelItems = gameState.Items.Where(item => item.MinLevel <= this._level).ToArray();
                var itemNumber = Random.NextInt(levelItems.Length);
                this._item = levelItems[itemNumber];
                this.State.ItemId = this._item.Id;
                return;
            }

            this.State.Gold = Random.NextInt(100) + 20;
            this._item = Item.CreateGold(this.State.Gold.Value);
        }
        
        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.IsOpen);
            var texture = Texture2D.FromFile(Core.GraphicsDevice, $"Content/images/sprites/{this._openImageName}");
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, MapScene.DefaultTileSize, MapScene.DefaultTileSize);
            this._openImage = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            this._openImage.RenderLayer = 15;
            this._openImage.LayerDepth = 15;
            this._openImage.SetEnabled(this.IsOpen);
        }

        public override bool OnAction(Party party)
        {
            this.GameState.IsPaused = true;
            void Done()
            {
                this.GameState.IsPaused = false;
            }

            if (this.IsOpen)
            {
                new TalkWindow(this._ui).Show("You found nothing", Done);
                return true;
            }

            if (!this.GameState.Party.CanOpenChest(this._level))
            {
                new TalkWindow(this._ui).Show("Unable to open chest", Done);
                return true;
            }
            
            if (this._item.Type == ItemType.Gold)
            {
                new TalkWindow(this._ui).Show($"You found {this._item.Cost} Gold", Done);
                party.Gold += this._item.Cost;
            }
            else
            {
                if (party.Items.Count >= Party.MaxItems)
                {
                    new TalkWindow(this._ui).Show($"You do not have enough space in your inventory for {this._item.Name}", Done);
                    return true;
                }

                new TalkWindow(this._ui).Show($"You found a {this._item.Name}", Done);
                party.Items.Add(new ItemInstance(this._item));
            }
            this.GameState.Sounds.PlaySoundEffect("treasure");
            this.IsOpen = true;
            this.DisplayVisual(!this.IsOpen);
            this._openImage.SetEnabled(this.IsOpen);

            return true;
        }
    }
}