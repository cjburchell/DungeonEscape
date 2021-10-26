using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class SpellWindow : SelectWindow<Spell>
    {
        public SpellWindow(UICanvas canvas, WindowInput input) : base(canvas, input, "Spells", new Point(150, 30))
        {
        }
    }
}