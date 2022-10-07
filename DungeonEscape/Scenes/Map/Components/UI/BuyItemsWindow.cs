namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class BuyItemsWindow : SelectWindow<Item>
    {
        public BuyItemsWindow(UiSystem ui) : base(ui, null, new Point(20, 20), 620, 48)
        {
        }

        protected override Button CreateButton(Item item)
        {
            var table = new Table();
            var image = new Image(item.Image);
            
            var style = item.Rarity switch
            {
                Rarity.Uncommon => "uncommon_label",
                Rarity.Rare => "rare_label",
                Rarity.Epic => "epic_label",
                Rarity.Common => "common_label",
                Rarity.Legendary => "legendary_label",
                _ => null
            };
            
            var itemName = new Label(item.NameWithStats, Skin, style);
            var cost = new Label($"{item.Cost}", Skin);
            table.Add(image).Left().Width(48);
            table.Add(itemName).Left().Width(500);
            table.Add(cost).Right().Width(30);

            var button = new Button(Skin, "no_border");
            button.Add(table).Left();
            return button;
        }
        
    }
}