namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;

    public class CommandMenu: SelectWindow<string>
    {
        public CommandMenu(UiSystem ui) : base(ui,"Command", new Point(20,20),100)
        {
        }
    }
}