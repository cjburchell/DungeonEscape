namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using State;

    public class SelectHeroWindow : SelectWindow<Hero>
    {
        public SelectHeroWindow(UISystem ui, Point point) : base(ui, "Select Hero", point, 250)
        {
        }
        
        public SelectHeroWindow(UISystem ui) : this(ui, new Point(20, 20))
        {
        }
    }
}