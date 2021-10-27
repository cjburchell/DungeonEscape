using System;
using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez.UI;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    using Nez;

    public class HeroStatusWindow: BasicWindow
    {
        private TextButton closeButton;
        private Action done;
        private Table statusTable;
        private Hero hero;

        public HeroStatusWindow(UISystem ui) : base(ui, "Status",
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
            this.statusTable.ClearChildren();
            this.statusTable.DebugAll();
            this.statusTable.Row().SetPadTop(5);
            this.statusTable.Add(new Label("Name:", Skin).SetAlignment(Align.TopLeft)).Width(100);
            this.statusTable.Add(new Label(hero.Name, Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Row();
            this.statusTable.Add(new Label("Level:", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Add(new Label($"{hero.Level}", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Row();
            this.statusTable.Add(new Label("Health:", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Add(new Label($"{hero.Health}/{hero.MaxHealth}", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Row();
            this.statusTable.Add(new Label("Magic:", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Add(new Label($"{hero.Magic}/{hero.MaxMagic}", Skin).SetAlignment(Align.TopLeft));
            
            this.statusTable.Row();
            this.statusTable.Add(new Label("Attack:", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Add(new Label($"{hero.Attack}", Skin).SetAlignment(Align.TopLeft));
            
            this.statusTable.Row();
            this.statusTable.Add(new Label("Defence:", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Add(new Label($"{hero.Defence}", Skin).SetAlignment(Align.TopLeft));
            
            this.statusTable.Row();
            this.statusTable.Add(new Label("Agility:", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Add(new Label($"{hero.Agility}", Skin).SetAlignment(Align.TopLeft));
            
            this.statusTable.Row();
            this.statusTable.Add(new Label("XP:", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Add(new Label($"{hero.XP}", Skin).SetAlignment(Align.TopLeft));
            
            this.statusTable.Row();
            this.statusTable.Add(new Label("Next Level:", Skin).SetAlignment(Align.TopLeft));
            this.statusTable.Add(new Label($"{hero.NextLevel - hero.XP}XP", Skin).SetAlignment(Align.TopLeft));
            
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
        
        public void Show(Hero hero, Action doneAction)
        {
            this.done = doneAction;
            this.hero = hero;
        }
    }
}