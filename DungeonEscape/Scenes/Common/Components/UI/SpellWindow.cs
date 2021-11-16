namespace DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;


    public class SpellWindow : SelectWindow<Spell>
    {
        public SpellWindow(UISystem ui, Point point) : base(ui, "Spells", point, 250)
        {
        }
        
        public SpellWindow(UISystem ui) : this(ui, new Point(20, 20))
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