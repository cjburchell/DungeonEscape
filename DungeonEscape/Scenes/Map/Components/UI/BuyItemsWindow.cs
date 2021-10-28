namespace DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class BuyItemsWindow : SelectWindow<Item>
    {
        public BuyItemsWindow(UISystem ui) : base(ui, "Buy", new Point(150, 30), 210)
        {
        }

        protected override Button CreateButton(Item item)
        {
            var table = new Table();
            var image = new Image(item.Image).SetAlignment(Align.Left);
            var itemName = new Label(item.Name, Skin).SetAlignment(Align.Left);
            var cost = new Label($"{item.Gold * 3 / 4}", Skin).SetAlignment(Align.Right);
            table.Add(image).Width(32);
            table.Add(itemName).Width(100);
            table.Add(cost).Width(30);

            var button = new Button(Skin);
            button.Add(table);
            return button;
        }
        
    }
}