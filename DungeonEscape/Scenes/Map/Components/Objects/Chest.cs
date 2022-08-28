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
            if(this.State.Item != null)
            {
                var tileSet = Game.LoadTileSet("Content/items.tsx");
                this.State.Item.Setup(tileSet);
                return;
            }
            
            if (this.TmxObject.Properties.ContainsKey("ItemId"))
            {
                this.State.Item = gameState.CustomItems.First(item => item.Id == tmxObject.Properties["ItemId"]);
                return;
            }

            if (this.TmxObject.Properties.ContainsKey("Gold"))
            {
                this.State.Item = Item.CreateGold(int.Parse(tmxObject.Properties["ItemId"]));
                return;
            }

            if (tmxObject.Name == "Key Chest")
            {
                this.State.Item = gameState.CustomItems.First(item => item.Id == "26");
                return;
            }

            if (Random.Chance(0.25f))
            {
                this.State.Item =  Item.CreateRandomItem(this.GameState.ItemDefinitions, this.GameState.StatNames, this._level);
                return;
            }
            
            this.State.Item = Item.CreateGold(Dice.Roll(5, 20));
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
            
            if (this.State.Item.Type == ItemType.Gold)
            {
                new TalkWindow(this._ui).Show($"You found {this.State.Item.Cost} Gold", Done);
                party.Gold += this.State.Item.Cost;
            }
            else
            {
                var selectedMember = party.AddItem(new ItemInstance(this.State.Item));
                if (selectedMember == null)
                {
                    new TalkWindow(this._ui).Show($"You do not have enough space in your inventory for {this.State.Item.Name}", Done);
                    return true;
                }

                new TalkWindow(this._ui).Show($"{selectedMember.Name} found a {this.State.Item.Name}", Done);
            }
            this.GameState.Sounds.PlaySoundEffect("treasure");
            this.IsOpen = true;
            this.DisplayVisual(!this.IsOpen);
            this._openImage.SetEnabled(this.IsOpen);

            return true;
        }
    }
}