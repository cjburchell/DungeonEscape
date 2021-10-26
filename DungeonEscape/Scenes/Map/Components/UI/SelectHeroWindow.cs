using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    
    public class SelectHeroWindow : SelectWindow<Hero>
    {
        public SelectHeroWindow(UICanvas canvas, WindowInput input) : base(canvas, input, "Select", new Point(150, 30))
        {
        }
    }
}