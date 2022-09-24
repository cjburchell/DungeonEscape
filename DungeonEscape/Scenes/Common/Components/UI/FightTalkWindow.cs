namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System;

    public class FightTalkWindow : TalkWindow
    {
        public new void Show(string text, Action doneAction)
        {
            base.Show(text, _ => doneAction.Invoke(), new []{"OK"} );
        }

        public FightTalkWindow(UiSystem ui) : base(ui)
        {
        }
    }
}