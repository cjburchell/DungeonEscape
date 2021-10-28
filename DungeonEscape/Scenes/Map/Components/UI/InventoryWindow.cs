using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class InventoryWindow : SelectWindow<ItemInstance>
    {
        public InventoryWindow(UISystem ui) : base(ui, "Inventory", new Point(150, 30))
        {
        }
        
        protected override Button CreateButton(ItemInstance item)
        {
            var equipSymbol = item.IsEquipped?"(E)":string.Empty;
            var button = new TextButton($"{item.Name}{equipSymbol}",  Skin, "no_border");
            button.GetLabel().SetAlignment(Align.Left);
            return button;
        }
    }
    
    
    public class BuyItemsWindow : SelectWindow<Item>
    {
        public BuyItemsWindow(UISystem ui) : base(ui, "Buy", new Point(150, 30))
        {
        }

        protected override Button CreateButton(Item item)
        {
            var button = new TextButton( $"{item.Name} {item.Gold}",  Skin, "no_border");
            button.GetLabel().SetAlignment(Align.Left);
            return button;
        }
        
    }
    
    public class SellPartyItemsWindow : SelectWindow<ItemInstance>
    {
        public SellPartyItemsWindow(UISystem ui) : base(ui, "Sell", new Point(150, 30))
        {
        }

        protected override Button CreateButton(ItemInstance item)
        {
            var equipSymbol = item.IsEquipped?"(E)":string.Empty;
            var button = new TextButton( $"{item.Name}{equipSymbol} {item.Gold}", Skin, "no_border");
            button.GetLabel().SetAlignment(Align.Left);
            return button;
        }
    }
}