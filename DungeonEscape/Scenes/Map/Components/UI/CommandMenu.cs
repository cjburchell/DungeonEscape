using DungeonEscape.Scenes.Common.Components.UI;
using Microsoft.Xna.Framework;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class CommandMenu: SelectWindow<string>
    {
        public CommandMenu(UISystem ui) : base(ui,"Command", new Point(20,20),100)
        {
        }
    }
}