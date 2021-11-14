using System;
using Microsoft.Xna.Framework;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class QuestionWindow : TextWindow
    {
        public QuestionWindow(UISystem ui) : base(ui,"Question",new Point(10, (MapScene.ScreenHeight) / 3 * 2))
        {
        }
        
        public void Show(string text, Action<bool> doneAction)
        {
            base.Show(text, result => doneAction.Invoke(result  == "Yes"), new []{"Yes", "No"} );
        }
    }
}