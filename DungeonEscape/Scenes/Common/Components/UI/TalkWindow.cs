using System;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class TalkWindow : TextWindow
    {
        public TalkWindow(UICanvas canvas, WindowInput input) : base(canvas, input, new Point(20, 20))
        {
        }

        public void Show(string text, Action doneAction)
        {
            base.Show(text, _ => doneAction.Invoke(), new []{"Close"} );
        }
    }
}