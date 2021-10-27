using System;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class QuestionWindow : TextWindow
    {
        public QuestionWindow(UISystem ui) : base(ui,new Point(20, 20))
        {
        }
        
        public void Show(string text, Action<bool> doneAction)
        {
            base.Show(text, result => doneAction.Invoke(result  == "Yes"), new []{"Yes", "No"} );
        }
    }
}