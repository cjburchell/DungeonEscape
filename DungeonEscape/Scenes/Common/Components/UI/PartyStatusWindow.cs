using System;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class PartyStatusWindow: BasicWindow
    {
        private TextButton closeButton;
        private Action done;
        private Label goldLabel;
        private Table statusTable;

        public PartyStatusWindow(UICanvas canvas, WindowInput input) : base(canvas, input, "Status",
            new Point(150, 30), 300, 150)
        {
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            var table = this.Window.AddElement(new Table());
            this.statusTable = new Table();
            this.closeButton = new TextButton("Close", Skin);
            this.closeButton.GetLabel();
            this.closeButton.ShouldUseExplicitFocusableControl = true;
            this.closeButton.OnClicked += _ => { this.HideWindow(); };

            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(this.statusTable).Expand().Top().Left().SetPadLeft(10);
            this.goldLabel = new Label($"Gold: 0", Skin);
            table.Row();
            table.Add(this.goldLabel).Left();
            table.Row();
            table.Add(this.closeButton).Height(30).Width(80).SetColspan(4).Center().Bottom().SetPadBottom(2);
        }

        void UpdateStatus(Party party)
        {
            this.goldLabel.SetText($"Gold: {party.Gold}");
            this.statusTable.ClearChildren();
            this.statusTable.DebugAll();
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

        public override void HideWindow()
        {
            base.HideWindow();
            this.Window.GetStage().SetGamepadFocusElement(null);
            this.done?.Invoke();
        }

        public override void DoAction()
        {
            this.HideWindow();
        }

        public void Show(Party party, Action doneAction, bool showClose = true)
        {
            FocusedWindow = this;
            this.done = doneAction;
            this.UpdateStatus(party);
            this.closeButton.SetVisible(showClose);
            this.ShowWindow();
            this.Window.GetStage().SetGamepadFocusElement(this.closeButton);
        }
    }
}