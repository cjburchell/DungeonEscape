namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;


    public class SpellWindow : SelectWindow<Spell>
    {
        public SpellWindow(UiSystem ui, Point point, IFighter hero) : base(ui, $"{hero.Name}'s Spells", point, 250)
        {
        }
        
        public SpellWindow(UiSystem ui, IFighter hero) : this(ui, new Point(20, 20), hero)
        {
        }
        
        protected override Button CreateButton(Spell spell)
        {
            var table = new Table();
            var image = new Image(spell.Image);
            var itemName = new Label(spell.Name, Skin);
            table.Add(image).Left().Width(32);
            table.Add(itemName).Left().Width(200);
            var button = new Button(Skin, "no_border");
            button.Add(table).Left();
            return button;
        }
    }
}