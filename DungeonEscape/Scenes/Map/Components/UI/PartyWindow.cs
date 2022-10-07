using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Nez.UI;
using Redpoint.DungeonEscape.Scenes.Common.Components.UI;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    public class PartyWindow : BasicWindow
    {
        private Action _done;
        private IGame _gameState;
        private TextButton _closeButton;
        private TextButton _addButton;
        private TextButton _removeButton;
        private TextButton _upButton;
        private TextButton _downButton;
        private ListBox<Hero> _activeMembersList;
        private ListBox<Hero> _standbyMembersList;

        public PartyWindow(UiSystem ui) : base(ui, new Point(20, 20), 1000, 300)
        {
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            var table = this.Window.AddElement(new Table());
            var buttonTable = this.Window.AddElement(new Table());
            this._activeMembersList = new ListBox<Hero>(Skin);
            this._standbyMembersList = new ListBox<Hero>(Skin);
            this._closeButton = new TextButton("Close", Skin)
            {
                ShouldUseExplicitFocusableControl = true
            };

            this._closeButton.OnClicked += _ =>
            {
                this.Ui.Sounds.PlaySoundEffect("confirm");
                this.CloseWindow();
            };
            
            this._addButton = new TextButton("Add", Skin)
            {
                ShouldUseExplicitFocusableControl = true
            };
            this._addButton.OnClicked += _ =>
            {
                var selected = this._standbyMembersList.GetSelected();
                if (selected == null || this._gameState.Party.ActiveMembers.Count() >= this._gameState.Settings.MaxPartyMembers)
                {
                    return;
                }
                
                
                selected.Order = this._gameState.Party.ActiveMembers.Max(i => i.Order) + 1;
                selected.IsActive = true;
                _activeMembersList.SetItems(this._gameState.Party.ActiveMembers.ToArray());
                _activeMembersList.SetSelected(selected);
                _standbyMembersList.SetItems(this._gameState.Party.InactiveMembers.ToArray());
                _standbyMembersList.SetSelected(null);
                UpdateButtons();
            };
            
            this._removeButton = new TextButton("Remove", Skin)
            {
                ShouldUseExplicitFocusableControl = true
            };
            this._removeButton.OnClicked += _ =>
            {
                var selected = this._activeMembersList.GetSelected();
                if (selected == null || this._gameState.Party.ActiveMembers.Count() == 1)
                {
                    return;
                }
                
                selected.IsActive = false;
                selected.Order = 0;
                _activeMembersList.SetItems(this._gameState.Party.ActiveMembers.ToArray());
                _activeMembersList.SetSelected(this._gameState.Party.ActiveMembers.First());
                _standbyMembersList.SetItems(this._gameState.Party.InactiveMembers.ToArray());
                _standbyMembersList.SetSelected(selected);
                UpdateButtons();
            };
            
            this._upButton = new TextButton("Up", Skin)
            {
                ShouldUseExplicitFocusableControl = true
            };
            this._upButton.OnClicked += _ =>
            {
                var selected = this._activeMembersList.GetSelected();
                if (selected == null || selected.Order == 0)
                {
                    return;
                }
                
                var other = this._gameState.Party.ActiveMembers.FirstOrDefault(i => i.Order == selected.Order - 1);
                if (other != null)
                {
                    other.Order++;
                }
                    
                selected.Order--;
                _activeMembersList.SetItems(this._gameState.Party.ActiveMembers.ToArray());
                _activeMembersList.SetSelected(selected);
                UpdateButtons();
            };
            
            this._downButton = new TextButton("Down", Skin)
            {
                ShouldUseExplicitFocusableControl = true
            };
            this._downButton.OnClicked += _ =>
            {
                var selected = this._activeMembersList.GetSelected();
                if (selected == null || selected.Order == this._gameState.Party.ActiveMembers.Max(i => i.Order))
                {
                    return;
                }
                
                var other = this._gameState.Party.ActiveMembers.FirstOrDefault(i => i.Order == selected.Order + 1);
                if (other != null)
                {
                    other.Order--;
                }
                    
                selected.Order++;
                _activeMembersList.SetItems(this._gameState.Party.ActiveMembers.ToArray());
                _activeMembersList.SetSelected(selected);
                UpdateButtons();
            };

            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(new Label("Active", Skin)).Center();
            table.Add(new Label("", Skin));
            table.Add(new Label("Inactive", Skin)).Center();
            table.Row();
            table.Add(this._activeMembersList).Expand().Center().Height(250).Width(400).SetPadLeft(10);
            buttonTable.Add(this._addButton).Height(ButtonHeight).Width(ButtonWidth).Center().Bottom()
                .SetPadBottom(2);;
            buttonTable.Row();
            buttonTable.Add(this._upButton).Height(ButtonHeight).Width(ButtonWidth).Center().Bottom()
                .SetPadBottom(2);;
            buttonTable.Row();
            buttonTable.Add(this._downButton).Height(ButtonHeight).Width(ButtonWidth).Center().Bottom()
                .SetPadBottom(2);;
            buttonTable.Row();
            buttonTable.Add(this._removeButton).Height(ButtonHeight).Width(ButtonWidth).Center().Bottom()
                .SetPadBottom(2);;
            buttonTable.Row();
            table.Add(buttonTable).Expand().Center().SetPadLeft(10);
            table.Add(this._standbyMembersList).Expand().Center().Height(250).Width(400).SetPadLeft(10);
            table.Row();
            table.Add(this._closeButton).Height(ButtonHeight).Width(ButtonWidth).SetColspan(4).Center().Bottom()
                .SetPadBottom(2);

            string DisplayItem(Hero hero)
            {
                return $"{hero.Name}   {hero.Class.ToString()[..3]}:{hero.Level} {hero.Health}/{hero.MaxHealth}";
            }

            void UpdateButtons()
            {
                var selected = this._activeMembersList.GetSelected();
                _upButton.SetDisabled(selected == null || selected.Order == 0);
                _downButton.SetDisabled(selected == null || selected.Order == this._gameState.Party.ActiveMembers.Max(i => i.Order));
                _removeButton.SetDisabled(this._gameState.Party.ActiveMembers.Count() == 1);
                
                _addButton.SetDisabled(this._standbyMembersList.GetSelected() == null || this._gameState.Party.ActiveMembers.Count() >= this._gameState.Settings.MaxPartyMembers);
            }

            _activeMembersList.SetItems(this._gameState.Party.ActiveMembers.ToArray());
            _activeMembersList.SetSelected(this._gameState.Party.ActiveMembers.First());
            _activeMembersList.OnDisplay += DisplayItem;
            _activeMembersList.OnChanged += _ => UpdateButtons();
            _standbyMembersList.SetItems(this._gameState.Party.InactiveMembers.ToArray());
            _standbyMembersList.OnDisplay += DisplayItem;
            _standbyMembersList.OnChanged += _ => UpdateButtons();
            UpdateButtons();

            this.ShowWindow();
            this.Window.GetStage().SetGamepadFocusElement(this._closeButton);
        }

        public override void CloseWindow(bool remove = true)
        {
            base.CloseWindow(remove);
            this._done?.Invoke();
        }
        
        public override void DoAction()
        {
            this.CloseWindow();
        }

        public void Show(IGame gameState, Action done)
        {
            this._done = done;
            this._gameState = gameState;
            this.ShowWindow();
        }
    }
}