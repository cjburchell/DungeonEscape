namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System;
    using Map;
    using Microsoft.Xna.Framework;

    public class QuestionWindow : TextWindow
    {
        public QuestionWindow(UiSystem ui, string title = null) : base(ui, title,
            new Point(10, MapScene.ScreenHeight / 3 * 2))
        {
        }

        public void Show(string text, Action<bool> doneAction)
        {
            base.Show(text, result => doneAction.Invoke(result  == "Yes"), new []{"Yes", "No"} );
        }
    }
}