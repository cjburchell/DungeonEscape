using System;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class PartyStatusWindow: BasicWindow
    {
        private TextButton closeButton;
        private Action done;
        private Label goldLabel;
        private Table statusTable;
        private Party party;

        public PartyStatusWindow(UISystem ui) : base(ui, "Status",
            new Point(30, 30), 300, 150)
        {
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.statusTable = new Table();
            this.closeButton = new TextButton("Close", Skin) {ShouldUseExplicitFocusableControl = true};
            this.closeButton.OnClicked += _ => { this.CloseWindow(); };
            this.goldLabel = new Label($"Gold: 0", Skin).SetAlignment(Align.Left);
            
            var table = this.Window.AddElement(new Table());
            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(this.statusTable).Expand().Top().Left().SetPadLeft(10);
            
            table.Row();
            table.Add(this.goldLabel).SetPadLeft(5).SetPadTop(5).Width(200).Left();
            table.Row();
            table.Add(this.closeButton).Height(30).Width(80).SetColspan(4).Center().Bottom().SetPadBottom(2);
            
            this.UpdateStatus();
            this.ShowWindow();
            this.Window.GetStage().SetGamepadFocusElement(this.closeButton);
        }

        void UpdateStatus()
        {
            this.goldLabel.SetText($"Gold: {this.party.Gold}");
            this.statusTable.Row();
            this.statusTable.Add(new Label("Name", Skin).SetAlignment(Align.Left)).Width(115);
            this.statusTable.Add(new Label("Level", Skin).SetAlignment(Align.Center)).Width(75);
            this.statusTable.Add(new Label("HP", Skin).SetAlignment(Align.Center)).Width(50);
            this.statusTable.Add(new Label("MP", Skin).SetAlignment(Align.Center)).Width(50);
            
            foreach (var partyMember in this.party.Members)
            {
                this.statusTable.Row().SetPadTop(5);
                
                var nameLabel = new Label(partyMember.Name, Skin).SetAlignment(Align.Left);
                var levelLabel = new Label($"{partyMember.Level}", Skin).SetAlignment(Align.Center);
                var healthLabel = new Label($"{partyMember.Health}", Skin).SetAlignment(Align.Center);
                var magicLabel = new Label($"{partyMember.Magic}", Skin).SetAlignment(Align.Center);
                
                this.statusTable.Add(nameLabel).Width(115);
                this.statusTable.Add(levelLabel).Width(75);
                this.statusTable.Add(healthLabel).Width(50);
                this.statusTable.Add(magicLabel).Width(50);
            }
            
            this.statusTable.Validate();
        }

        public override void CloseWindow()
        {
            base.CloseWindow();
            this.done?.Invoke();
        }

        public override void DoAction()
        {
            this.CloseWindow();
        }

        public void Show(Party partyToShow, Action doneAction)
        {
            this.done = doneAction; ;
            this.party = partyToShow;
        }
    }
}