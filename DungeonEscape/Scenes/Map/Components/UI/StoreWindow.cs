namespace DungeonEscape.Scenes.Map.Components.UI
{
    using System;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;

    public enum StoreAction
    {
        Buy,
        Sell
    }

    public class StoreWindow : TextWindow    
    {
        public StoreWindow(UISystem ui) : base(ui, "Store", new Point(10, (MapScene.ScreenHeight) / 3 * 2))
        {
        }

        public void Show(Action<StoreAction?> doneAction)
        {
            base.Show("Welcome to my store. I buy and sell items what can I do for you?", choice =>
            {
                doneAction(choice switch
                {
                    "Sell" => StoreAction.Sell,
                    "Buy" => StoreAction.Buy,
                    _ => null
                });
                
            }, new []{"Buy", "Sell"} );
        }
    }
}