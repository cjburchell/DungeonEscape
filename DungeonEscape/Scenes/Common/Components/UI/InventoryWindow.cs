using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class InventoryWindow : SelectWindow<ItemInstance>
    {
        public InventoryWindow(UISystem ui) : base(ui, "Inventory", new Point(30, 30), 250)
        {
        }
        
        protected override Button CreateButton(ItemInstance item)
        {
            var table = new Table();
            var image = new Image(item.Image).SetAlignment(Align.Left);
            var equipSymbol = item.IsEquipped?"(E)":string.Empty;
            var equip = new Label(equipSymbol, Skin).SetAlignment(Align.Left);
            var itemName = new Label(item.Name, Skin).SetAlignment(Align.Left);
            table.Add(image).Width(32);
            table.Add(equip).Width(ButtonHeight);
            table.Add(itemName).Width(100);

            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}