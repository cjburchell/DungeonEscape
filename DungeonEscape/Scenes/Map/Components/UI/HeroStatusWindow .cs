using System;
using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez.UI;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class HeroStatusWindow: BasicWindow
    {
        private TextButton closeButton;
        private Action done;
        private Table statusTable;
        private Hero hero;

        public HeroStatusWindow(UISystem ui) : base(ui, "Hero Status",
            new Point(30, 30), 220, 250)
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
            this.closeButton.OnClicked += _ => { this.CloseWindow(); };

            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(this.statusTable).Expand().Top().Left().SetPadLeft(10);
            table.Row();
            table.Add(this.closeButton).Height(30).Width(80).SetColspan(4).Center().Bottom().SetPadBottom(2);
            
            this.ShowWindow();
            this.UpdateStatus();
            this.Window.GetStage().SetGamepadFocusElement(this.closeButton);
        }

        void UpdateStatus()
        {
            const int labelColumnWidth = 125;
            const int dataColumnWidth = 75;
            this.statusTable.Row().SetPadTop(5);
            this.statusTable.Add(new Label(this.hero.Name, Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this.statusTable.Row();
            this.statusTable.Add(new Label("Level:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this.statusTable.Add(new Label($"{this.hero.Level}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this.statusTable.Row();
            this.statusTable.Add(new Label("Health:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this.statusTable.Add(new Label($"{this.hero.Health}/{hero.MaxHealth}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this.statusTable.Row();
            this.statusTable.Add(new Label("Magic:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this.statusTable.Add(new Label($"{this.hero.Magic}/{hero.MaxMagic}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this.statusTable.Row();
            this.statusTable.Add(new Label("Attack:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this.statusTable.Add(new Label($"{this.hero.Attack}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this.statusTable.Row();
            this.statusTable.Add(new Label("Defence:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this.statusTable.Add(new Label($"{this.hero.Defence}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this.statusTable.Row();
            this.statusTable.Add(new Label("Agility:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this.statusTable.Add(new Label($"{this.hero.Agility}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this.statusTable.Row();
            this.statusTable.Add(new Label("XP:", Skin).SetAlignment(Align.TopLeft)).Width(labelColumnWidth);
            this.statusTable.Add(new Label($"{this.hero.XP}", Skin).SetAlignment(Align.TopLeft)).Width(dataColumnWidth);
            this.statusTable.Row();
            this.statusTable.Add(new Label("Next Level:", Skin).SetAlignment(Align.TopLeft)).Width(labelColumnWidth);
            this.statusTable.Add(new Label($"{this.hero.NextLevel - this.hero.XP}XP", Skin).SetAlignment(Align.TopLeft)).Width(dataColumnWidth);
            
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
        
        public void Show(Hero heroToShow, Action doneAction)
        {
            this.done = doneAction;
            this.hero = heroToShow;
        }
    }
}