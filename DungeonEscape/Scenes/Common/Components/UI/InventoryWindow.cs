namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class InventoryWindow : SelectWindow<ItemInstance>
    {
        public InventoryWindow(UISystem ui) : this(ui, new Point(20, 20))
        {
        }
        
        public InventoryWindow(UISystem ui, Point position) : base(ui, "Inventory", position, 250)
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