namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using System;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class HeroStatusWindow: BasicWindow
    {
        private TextButton _closeButton;
        private Action _done;
        private Table _statusTable;
        private Hero _hero;

        public HeroStatusWindow(UiSystem ui) : base(ui, null,
            new Point(20, 20), 220, 250)
        {
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            var table = this.Window.AddElement(new Table());
            this._statusTable = new Table();
            this._closeButton = new TextButton("Close", Skin);
            this._closeButton.GetLabel();    
            this._closeButton.ShouldUseExplicitFocusableControl = true;
            this._closeButton.OnClicked += _ => { this.CloseWindow(); };

            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(this._statusTable).Expand().Top().Left().SetPadLeft(10);
            table.Row();
            table.Add(this._closeButton).Height(ButtonHeight).Width(ButtonWidth).SetColspan(4).Center().Bottom().SetPadBottom(2);
            
            this.ShowWindow();
            this.UpdateStatus();
            this.Window.GetStage().SetGamepadFocusElement(this._closeButton);
        }

        private void UpdateStatus()
        {
            const int labelColumnWidth = 125;
            const int dataColumnWidth = 75;
            this._statusTable.Row().SetPadTop(5);
            this._statusTable.Add(new Label(this._hero.Name, Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Level:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Level}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Health:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Health}/{this._hero.MaxHealth}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Magic:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Magic}/{this._hero.MaxMagic}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Attack:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Attack}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Defence:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Defence}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Agility:", Skin).SetAlignment(Align.Left)).Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Agility}", Skin).SetAlignment(Align.Left)).Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("XP:", Skin).SetAlignment(Align.TopLeft)).Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Xp}", Skin).SetAlignment(Align.TopLeft)).Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Next Level:", Skin).SetAlignment(Align.TopLeft)).Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.NextLevel - this._hero.Xp}XP", Skin).SetAlignment(Align.TopLeft)).Width(dataColumnWidth);
            
            this._statusTable.Validate();
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
        
        public void Show(Hero heroToShow, Action doneAction)
        {
            this._done = doneAction;
            this._hero = heroToShow;
            this.ShowWindow();
        }
    }
}