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
            return new TextButton( $"{item.Name}", Skin);
        }
    }
    
    
    public class SellItemsWindow : SelectWindow<Item>
    {
        public SellItemsWindow(UISystem ui) : base(ui, "Sell", new Point(150, 30))
        {
        }

        protected override Button CreateButton(Item item)
        {
            return new TextButton( $"{item.Name} {item.Gold}", Skin);
        }
    }
}