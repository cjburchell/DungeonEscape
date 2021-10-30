using System;
using Microsoft.Xna.Framework;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class TalkWindow : TextWindow
    {
        public TalkWindow(UISystem ui, string title = "Talk") : this(ui, title, new Point(20, 20))
        {
        }
        
        public TalkWindow(UISystem ui, string title, Point position, int width = 472, int height = 150) : base(ui, title, position, width, height)
        {
        }

        public void Show(string text, Action doneAction)
        {
            base.Show(text, _ => doneAction.Invoke(), new []{"Close"} );
        }
    }
}