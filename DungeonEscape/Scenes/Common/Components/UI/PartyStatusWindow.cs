using System;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    using Nez;

    public class PartyStatusWindow: BasicWindow
    {
        private TextButton closeButton;
        private Action done;
        private Label goldLabel;
        private Table statusTable;
        private Party party;
        private bool showClose;

        public PartyStatusWindow(UISystem ui) : base(ui, "Status",
            new Point(150, 30), 300, 150)
        {
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.statusTable = new Table();
            this.closeButton = new TextButton("Close", Skin);
            this.closeButton.ShouldUseExplicitFocusableControl = true;
            this.closeButton.OnClicked += _ => { this.CloseWindow(); };
            this.goldLabel = new Label($"Gold: 0", Skin);
            
            var table = this.Window.AddElement(new Table());
            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(this.statusTable).Expand().Top().Left().SetPadLeft(10);
            
            table.Row();
            table.Add(this.goldLabel).Left();
            table.Row();
            table.Add(this.closeButton).Height(30).Width(80).SetColspan(4).Center().Bottom().SetPadBottom(2);
            
            this.UpdateStatus(this.party);
            this.closeButton.SetVisible(this.showClose);
            this.ShowWindow();
            this.Window.GetStage().SetGamepadFocusElement(this.closeButton);
        }

        void UpdateStatus(Party party)
        {
            this.goldLabel.SetText($"Gold: {party.Gold}");
            this.statusTable.Row();
            this.statusTable.Add(new Label("Name", Skin).SetAlignment(Align.Center)).Width(115);
            this.statusTable.Add(new Label("Level", Skin).SetAlignment(Align.Center)).Width(75);
            this.statusTable.Add(new Label("HP", Skin).SetAlignment(Align.Center)).Width(50);
            this.statusTable.Add(new Label("MP", Skin).SetAlignment(Align.Center)).Width(50);
            
            foreach (var partyMember in party.Members)
            {
                this.statusTable.Row().SetPadTop(5);
                
                var nameLabel = new Label(partyMember.Name, Skin);
                var levelLabel = new Label($"{partyMember.Level}", Skin);
                var healthLabel = new Label($"{partyMember.Health}", Skin);
                var magicLabel = new Label($"{partyMember.Magic}", Skin);
                
                this.statusTable.Add(nameLabel);
                this.statusTable.Add(levelLabel);
                this.statusTable.Add(healthLabel);
                this.statusTable.Add(magicLabel);
            }
            
            this.statusTable.Validate();
        }

        public override void CloseWindow()
        {
            base.CloseWindow();
            this.Window.GetStage().SetGamepadFocusElement(null);
            this.done?.Invoke();
            this.ui.Canvas.RemoveComponent(this);
        }

        public override void DoAction()
        {
            this.CloseWindow();
        }

        public void Show(Party party, Action doneAction, bool showClose = true)
        {
            this.done = doneAction; ;
            this.party = party;
            this.showClose = showClose;
        }
    }
}