using DungeonEscape.State;
using Microsoft.Xna.Framework;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    
    public class SelectHeroWindow : SelectWindow<Hero>
    {
        public SelectHeroWindow(UISystem ui) : base(ui, "Select Hero", new Point(20, 20), 250)
        {
        }
    }
}