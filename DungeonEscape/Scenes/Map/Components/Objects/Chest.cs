namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Nez.Sprites;
    using Nez.Tiled;
    using State;

    public class Chest : MapObject
    {
        private readonly UiSystem _ui;
        private readonly int _level;
        private SpriteAnimator _openImage;
        private readonly int _openImageId;
        
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
            this._openImageId = tmxObject.Properties.ContainsKey("OpenImage") ? int.Parse(tmxObject.Properties["OpenImage"]) : 135;
            if(this.State.Item != null)
            {
                var tileSet = Game.LoadTileSet("Content/items2.tsx");
                this.State.Item.Setup(tileSet, gameState.Skills);
                return;
            }
            
            if (this.TmxObject.Properties.ContainsKey("ItemId"))
            {
                this.State.Item = this.GameState.GetCustomItem(tmxObject.Properties["ItemId"]);
                return;
            }

            if (this.TmxObject.Properties.ContainsKey("Gold"))
            {
                this.State.Item = GameState.CreateGold(int.Parse(tmxObject.Properties["Gold"]));
                return;
            }
            
            
            this.State.Item = GameState.CreateRandomItem(this._level == 0 ? this.GameState.Party.MaxLevel() : this._level);
        }
        
        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.IsOpen);
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this._tileSet.Image.Texture,
                (int) this.TmxObject.Width, (int) this.TmxObject.Height);
            this._openImage =
                this.Entity.AddComponent(
                    new SpriteAnimator(sprites[this._openImageId]));
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
                    new TalkWindow(this._ui).Show($"You found {this.State.Item.Name} but your party did not have enough space in your inventory it", Done);
                    return true;
                }

                var questMessage = GameState.CheckQuest(this.State.Item);

                new TalkWindow(this._ui).Show($"{selectedMember.Name} found a {this.State.Item.Name}\n{questMessage}", Done);
            }
            this.GameState.Sounds.PlaySoundEffect("treasure");
            this.IsOpen = true;
            this.DisplayVisual(!this.IsOpen);
            this._openImage.SetEnabled(this.IsOpen);

            return true;
        }
    }
}