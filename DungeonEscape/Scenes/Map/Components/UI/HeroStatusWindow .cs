using Nez;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using System.Linq;
    using System;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class HeroStatusWindow : BasicWindow
    {
        private TextButton _closeButton;
        private Action _done;
        private Table _statusTable;
        private Hero _hero;
        private Table _itemTable;

        public HeroStatusWindow(UiSystem ui) : base(ui,
            new Point(20, 20), 1000, 300)
        {
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            var table = this.Window.AddElement(new Table());
            this._statusTable = new Table();
            this._itemTable = new Table();
            this._closeButton = new TextButton("Close", Skin)
            {
                ShouldUseExplicitFocusableControl = true
            };

            this._closeButton.OnClicked += _ =>
            {
                this.Ui.Sounds.PlaySoundEffect("confirm");
                this.CloseWindow();
            };

            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(this._statusTable).Expand().Top().Left().SetPadLeft(10);
            table.Add(this._itemTable).Expand().Top().Left().SetPadLeft(10);
            table.Row();
            table.Add(this._closeButton).Height(ButtonHeight).Width(ButtonWidth).SetColspan(4).Center().Bottom()
                .SetPadBottom(2);

            this.ShowWindow();
            this.UpdateStatus();
            this.Window.GetStage().SetGamepadFocusElement(this._closeButton);
        }

        private void UpdateStatus()
        {
            const int labelColumnWidth = 125;
            const int dataColumnWidth = 75;
            this._statusTable.Row().SetPadTop(5);
            this._statusTable.Add(new Label(this._hero.Name, Skin)).Width(labelColumnWidth).Left();
            this._statusTable.Row();
            this._statusTable.Add(new Label("Level:", Skin)).Left().Width(labelColumnWidth).Left();
            this._statusTable.Add(new Label($"{this._hero.Level}", Skin)).Left().Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Health:", Skin)).Left().Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Health}/{this._hero.MaxHealth}", Skin)).Left().Width(dataColumnWidth);
            this._statusTable.Row();
            if (this._hero.MaxMagic != 0)
            {
                this._statusTable.Add(new Label("Magic:", Skin)).Left().Width(labelColumnWidth);
                this._statusTable.Add(new Label($"{this._hero.Magic}/{this._hero.MaxMagic}", Skin)).Left().Width(dataColumnWidth);
                this._statusTable.Row();
            }

            this._statusTable.Add(new Label("Attack:", Skin)).Left().Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Attack}", Skin)).Left().Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Defence:", Skin)).Left().Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Defence}", Skin)).Left().Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Magic Defence:", Skin)).Left().Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.MagicDefence}", Skin)).Left().Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Agility:", Skin)).Left().Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Agility}", Skin)).Left().Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("XP:", Skin)).Left().Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Xp}", Skin)).Left().Width(dataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Next Level:", Skin)).Left().Width(labelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.NextLevel - this._hero.Xp}XP", Skin)).Left().Width(dataColumnWidth);

            this._statusTable.Validate();


            const int itemLabelColumnWidth = 125;
            const int itemColumnWidth = 500;
            this._itemTable.Row().SetPadTop(5);
            this._itemTable.Add(new Label("Equipment", Skin)).Left().Width(itemLabelColumnWidth);
            this._itemTable.Row();
            var slots = Enum.GetValues(typeof(Slot))
                .Cast<Slot>()
                .ToList();

            foreach (var slot in slots)
            {
                this._itemTable.Add(new Label($"{slot}:", Skin)).Left().Width(itemLabelColumnWidth);
                var itemId = this._hero.GetEquipmentId(new[] { slot }).FirstOrDefault();
                var item = this._hero.Items.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    var image = this._itemTable.Add(new Image(item.Image)).Width(48).Left().GetElement<Image>();
                    var style = item.Rarity switch
                    {
                        Rarity.Uncommon => "uncommon_label",
                        Rarity.Rare => "rare_label",
                        Rarity.Epic => "epic_label",
                        Rarity.Common => "common_label",
                        Rarity.Legendary => "legendary_label",
                        _ => null
                    };
                    
                    image.SetTouchable(Touchable.Enabled);
                    var tooltip = new Tooltip(new Label(item.NameWithStats, Skin, style),image);
                    this.Ui.Canvas.Stage.AddElement(tooltip);
                    
                    this._itemTable.Add(new Label($"{item.NameWithStats}", Skin, style)).Left().Width(itemColumnWidth);
                }

                this._itemTable.Row();
            }

            this._itemTable.Validate();

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