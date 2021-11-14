namespace DungeonEscape.Scenes.Common.Components.UI
{
    using System;
    using Microsoft.Xna.Framework;

    public class FightTalkWindow : TalkWindow
    {
        public new void Show(string text, Action doneAction)
        {
            base.Show(text, _ => doneAction.Invoke(), new []{"OK"} );
        }

        public FightTalkWindow(UISystem ui, string title = "Talk") : base(ui, title)
        {
        }

        public FightTalkWindow(UISystem ui, string title, Point position, int width = 492, int height = 150) : base(ui, title, position, width, height)
        {
        }
    }
}