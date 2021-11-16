namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

    public class KeyStore : Sprite
    {
        private readonly UISystem ui;
        private readonly Item key;

        public KeyStore(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UISystem ui) : base(
            tmxObject, state, map, gameState, graph)
        {
            this.ui = ui;
            var cost = tmxObject.Properties.ContainsKey("Cost") ? int.Parse(tmxObject.Properties["Cost"]) : 250;
            var level = tmxObject.Properties.ContainsKey("Level") ? int.Parse(tmxObject.Properties["Level"]) : 0;
            this.key = new Item("Content/images/items/key.png", "Key", ItemType.Key, cost, level);
        }

        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            var questionWindow = new QuestionWindow(this.ui);
            var goldWindow = new GoldWindow(party, this.ui.Canvas, new Point(MapScene.ScreenWidth - 155, MapScene.ScreenHeight / 3 * 2 - 55));
            goldWindow.ShowWindow();
            questionWindow.Show($"Would you like to buy a key\nFor {this.key.Gold} gold?", accepted =>
            {
                if (accepted)
                {
                    void Done()
                    {
                        goldWindow.CloseWindow();
                        this.gameState.IsPaused = false;
                    }

                    if (party.Gold < this.key.Gold)
                    {
                        new TalkWindow(this.ui).Show($"You do not have {this.key.Gold} gold",
                            Done);
                    }
                    else if (party.Items.Count >= Party.MaxItems)
                    {
                        new TalkWindow(this.ui).Show("You do not have space in your inventory for the key",
                            Done);
                    }
                    else
                    {
                        party.Gold -= this.key.Gold;
                        party.Items.Add(new ItemInstance(this.key));
                        new TalkWindow(this.ui).Show("Thank you come again!", Done);
                    }
                }
                else
                {
                    this.gameState.IsPaused = false;
                }
            });

            return true;
        }
    }
}    