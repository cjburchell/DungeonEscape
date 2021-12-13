namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System;
    using System.Linq;
    using Map;
    using Microsoft.Xna.Framework;
    using State;

    public class TalkWindow : TextWindow
    {
        public TalkWindow(UiSystem ui, string title = null) : this(ui, title, new Point(10, MapScene.ScreenHeight / 3 * 2))
        {
        }

        private TalkWindow(UiSystem ui, string title, Point position, int width = MapScene.ScreenWidth - 20, int height = MapScene.ScreenHeight / 3 - 10) : base(ui, title, position, width, height)
        {
        }

        public void Show(string text, Action doneAction)
        {
            base.Show(text, _ => doneAction.Invoke(), new []{"Close"} );
        }
        
        public void Show(Dialog dialog, Action<Choice> doneAction)
        {
            base.Show(dialog.Text, result =>  doneAction.Invoke(dialog.Choices.FirstOrDefault(i=> i.Text == result)), dialog.Choices.Select( i => i.Text) );
        }
    }
}