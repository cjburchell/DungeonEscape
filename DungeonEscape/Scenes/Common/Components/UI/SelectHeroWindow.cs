namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using State;

    public class SelectHeroWindow : SelectWindow<Hero>
    {
        public SelectHeroWindow(UiSystem ui, Point point) : base(ui, null, point, 250)
        {
        }
        
        public SelectHeroWindow(UiSystem ui) : this(ui, new Point(20, 20))
        {
        }
    }
}