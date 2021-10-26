using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class InventoryWindow : SelectWindow<ItemInstance>
    {
        public InventoryWindow(UICanvas canvas, WindowInput input) : base(canvas, input, "Inventory", new Point(150, 30))
        {
        }
    }
}