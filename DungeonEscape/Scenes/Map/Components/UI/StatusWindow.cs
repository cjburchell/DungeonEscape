using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class StatusWindow: GameWindow, IUpdatable
    {
        private readonly IGame gameState;
        private VirtualButton hideWindowInput;
        private TextButton closeButton;
        private Action done;
        private Label[][] memberStats;
        private Label goldLabel;

        public StatusWindow(UICanvas canvas, IGame gameState) : base(canvas,"Command", new Point(150,30),300,150)
        {
            this.gameState = gameState;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            var table = this.Window.AddElement(new Table());
            var statusTable = new Table();
            this.closeButton = new TextButton("Close", Skin);
            this.closeButton.GetLabel().SetFontScale(FontScale);
            this.closeButton.OnClicked += _ => { this.CloseWindow(); };

            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(statusTable).Expand().Top().Left().SetPadLeft(10);
            statusTable.Row();
            statusTable.Add(new Label("Name").SetFontScale(FontScale)).Width(115);
            statusTable.Add(new Label("Level").SetFontScale(FontScale)).Width(75);
            statusTable.Add(new Label("HP").SetFontScale(FontScale)).Width(50);
            statusTable.Add(new Label("MP").SetFontScale(FontScale)).Width(50);
            
            var memberStats = new List<Label[]>();
            foreach (var partyMember in this.gameState.Party.Members)
            {
                var stats = new List<Label>();
                statusTable.Row().SetPadTop(5);
                
                var nameLabel = new Label(partyMember.Name).SetFontScale(FontScale);
                stats.Add(nameLabel);
                var levelLabel = new Label($"{partyMember.Level}").SetFontScale(FontScale);
                stats.Add(levelLabel);
                var healthLabel = new Label($"{partyMember.Health}").SetFontScale(FontScale);
                stats.Add(healthLabel);
                var magicLabel = new Label($"{partyMember.Magic}").SetFontScale(FontScale);
                stats.Add(magicLabel);
                
                
                statusTable.Add(nameLabel);
                statusTable.Add(levelLabel);
                statusTable.Add(healthLabel);
                statusTable.Add(magicLabel);
                memberStats.Add(stats.ToArray());
            }

            this.memberStats = memberStats.ToArray();


            this.goldLabel = new Label($"Gold: {this.gameState.Party.Gold}").SetFontScale(FontScale);
            table.Row();
            table.Add(this.goldLabel).Left();
            table.Row();
            table.Add(this.closeButton).Height(30).Width(80).SetColspan(4).Center().Bottom().SetPadBottom(2);

            this.hideWindowInput = new VirtualButton();
            this.hideWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
            this.hideWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.RightControl));
            this.hideWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.B));
        }

        void UpdateStatus()
        {
            this.goldLabel.SetText($"Gold: {this.gameState.Party.Gold}");
            var member = 0;
            foreach (var partyMember in this.gameState.Party.Members)
            {
                this.memberStats[member][0].SetText(partyMember.Name);
                this.memberStats[member][1].SetText($"{partyMember.Level}");
                this.memberStats[member][2].SetText($"{partyMember.Health}");
                this.memberStats[member][3].SetText($"{partyMember.Magic}");
                member++;
            }
        }

        protected override void HideWindow()
        {
            base.HideWindow();
            this.Window.GetStage().SetGamepadFocusElement(null);
        }

        protected override void ShowWindow()
        {
            base.ShowWindow();
            this.Window.GetStage().SetGamepadFocusElement(this.closeButton);
            this.gameState.IsPaused = true;
        }

        public override void OnRemovedFromEntity()
        {
            this.hideWindowInput.Deregister();
        }

        private void CloseWindow()
        {
            if (!this.IsVisible)
            {
                return;
            }

            this.HideWindow();
            this.done?.Invoke();
        }
        

        public void Update()
        {
            if (!this.IsVisible)
            {
                return;
            }
            
            this.UpdateStatus();
            if (this.hideWindowInput.IsPressed)
            {
                this.CloseWindow();   
            }
        }

        public void Show(Action doneAction)
        {
            this.done = doneAction;
            this.ShowWindow();
        }
    }
}