namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class BuyItemsWindow : SelectWindow<Item>
    {
        public BuyItemsWindow(UiSystem ui) : base(ui, null, new Point(20, 20), 250)
        {
        }

        protected override Button CreateButton(Item item)
        {
            var table = new Table();
            var image = new Image(item.Image).SetAlignment(Align.Left);
            var itemName = new Label(item.Name, Skin).SetAlignment(Align.Left);
            var cost = new Label($"{item.Cost}", Skin).SetAlignment(Align.Right);
            table.Add(image).Width(32);
            table.Add(itemName).Width(150);
            table.Add(cost).Width(30);

            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
        
    }
}