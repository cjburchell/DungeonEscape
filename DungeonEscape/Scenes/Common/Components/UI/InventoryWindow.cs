namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class InventoryWindow : SelectWindow<ItemInstance>
    {
        private readonly Hero _hero;
        
        public InventoryWindow(UiSystem ui, Hero hero) : this(ui, hero, new Point(20, 20))
        {
        }
        
        public InventoryWindow(UiSystem ui, Hero hero, Point point) : base(ui, $"{hero.Name}'s Inventory" ,point, 700, 48)
        {
            _hero = hero;
        }
        
        protected override Button CreateButton(ItemInstance item)
        {
            var table = new Table();
            var image = new Image(item.Image);
            var equipSymbol = string.Empty;
            if (item.IsEquipped)
            {
                equipSymbol = "(E)";
            }
            else
            {
                if (item.IsEquippable && item.Item.Classes != null && !item.Item.Classes.Contains(this._hero.Class))
                {
                    equipSymbol = "!";
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
            
            if (item.Type == ItemType.Quest)
            {
                style = "quest_label";
            }

            var equip = new Label(equipSymbol, Skin);
            var itemName = new Label(item.NameWithStats, Skin, style);
            table.Add(image).Width(48).Left();
            table.Add(itemName).Width(500).Left();
            table.Add(equip).Width(100).Left();

            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}