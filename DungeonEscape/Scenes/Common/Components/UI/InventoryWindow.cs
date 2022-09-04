namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class InventoryWindow : SelectWindow<ItemInstance>
    {
        private readonly List<Hero> _heroes;

        public InventoryWindow(UiSystem ui, List<Hero> heroes) : this(ui, new Point(20, 20))
        {
            _heroes = heroes;
        }
        
        public InventoryWindow(UiSystem ui, Point position) : base(ui, "Inventory", position, 700, 48)
        {
        }
        
        protected override Button CreateButton(ItemInstance item)
        {
            var table = new Table();
            var image = new Image(item.Image).SetAlignment(Align.Left);
            var equipSymbol = string.Empty;
            if (item.IsEquipped)
            {
                if (this._heroes.Any(hero => hero.Id == item.EquippedTo))
                {
                    equipSymbol = "(E)";
                }
            }

            var style = item.Rarity switch
            {
                Rarity.Uncommon => "uncommon_label",
                Rarity.Rare => "rare_label",
                Rarity.Epic => "epic_label",
                Rarity.Common => "common_label",
                _ => null
            };

            var equip = new Label(equipSymbol, Skin).SetAlignment(Align.Left);
            var itemName = new Label(item.NameWithStats, Skin, style).SetAlignment(Align.Left);
            table.Add(image).Width(48);
            table.Add(itemName).Width(500);
            table.Add(equip).Width(100);

            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}