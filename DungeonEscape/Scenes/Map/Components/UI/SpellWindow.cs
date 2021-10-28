using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    using Nez.UI;

    public class SpellWindow : SelectWindow<Spell>
    {
        public SpellWindow(UISystem ui) : base(ui, "Spells", new Point(30, 30), 250)
        {
        }
        
        protected override Button CreateButton(Spell item)
        {
            var table = new Table();
            var image = new Image(item.Image).SetAlignment(Align.Left);
            var itemName = new Label(item.Name, Skin).SetAlignment(Align.Left);
            table.Add(image).Width(32);
            table.Add(itemName).Width(100);
            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}