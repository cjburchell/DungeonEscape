using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class SpellWindow : SelectWindow<Spell>
    {
        public SpellWindow(UISystem ui) : base(ui, "Spells", new Point(150, 30))
        {
        }
    }
}