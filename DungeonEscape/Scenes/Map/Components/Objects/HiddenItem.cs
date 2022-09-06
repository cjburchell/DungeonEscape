namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System.Linq;
    using Common.Components.UI;
    using Nez.Tiled;
    using State;

    public class HiddenItem : MapObject
    {
        private readonly UiSystem _ui;
        private readonly int _level;
        
        private bool IsOpen
        {
            get => this.State.IsOpen != null && this.State.IsOpen.Value;
            set => this.State.IsOpen = value;
        }

        public HiddenItem(TmxObject tmxObject, ObjectState state, TmxMap map, UiSystem ui, IGame gameState) : base(tmxObject, state, map, gameState)
        {
            this._ui = ui;
            this._level = tmxObject.Properties.ContainsKey("Level") ? int.Parse(tmxObject.Properties["Level"]) : 0;
            if(this.State.Item != null)
            {
                this.State.Item.Setup(Game.LoadTileSet("Content/items2.tsx"));
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

            this.State.Item = GameState.CreateRandomItem( this._level == 0 ? this.GameState.Party.MaxLevel() : this._level);
        }

        public override bool OnAction(Party party)
        {
            this.GameState.IsPaused = true;
            void Done()
            {
                this.GameState.IsPaused = false;
            }

            if (this.IsOpen || !this.GameState.Party.CanOpenChest(this._level))
            {
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
            return true;
        }
    }
}