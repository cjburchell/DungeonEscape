namespace DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class SellPartyItemsWindow : SelectWindow<ItemInstance>
    {
        public SellPartyItemsWindow(UISystem ui) : base(ui, "Sell", new Point(20, 20), 250)
        {
        }

        protected override Button CreateButton(ItemInstance item)
        {
            var table = new Table();
            var image = new Image(item.Image).SetAlignment(Align.Left);
            var equipSymbol = item.IsEquipped?"(E)":string.Empty;
            var equip = new Label(equipSymbol, Skin).SetAlignment(Align.Left);
            var itemName = new Label(item.Name, Skin).SetAlignment(Align.Left);
            var cost = new Label($"{item.Gold * 3 / 4}", Skin).SetAlignment(Align.Right);
            table.Add(image).Width(32);
            table.Add(equip).Width(ButtonHeight);
            table.Add(itemName).Width(100);
            table.Add(cost).Width(32);

            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}