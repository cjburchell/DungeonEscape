namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

    public class KeyStore : Sprite
    {
        private readonly UiSystem _ui;
        private readonly Item _key;

        public KeyStore(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UiSystem ui) : base(
            tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
            var cost = tmxObject.Properties.ContainsKey("Cost") ? int.Parse(tmxObject.Properties["Cost"]) : 250;
            var level = tmxObject.Properties.ContainsKey("Level") ? int.Parse(tmxObject.Properties["Level"]) : 0;
            this._key = new Item("Content/images/items/key.png", "Key", ItemType.Key, cost, level);
        }

        public override bool OnAction(Party party)
        {
            this.GameState.IsPaused = true;
            var questionWindow = new QuestionWindow(this._ui);
            var goldWindow = new GoldWindow(party, this._ui.Canvas, new Point(MapScene.ScreenWidth - 155, MapScene.ScreenHeight / 3 * 2 - 55));
            goldWindow.ShowWindow();
            questionWindow.Show($"Would you like to buy a key\nFor {this._key.Gold} gold?", accepted =>
            {
                if (accepted)
                {
                    void Done()
                    {
                        goldWindow.CloseWindow();
                        this.GameState.IsPaused = false;
                    }

                    if (party.Gold < this._key.Gold)
                    {
                        new TalkWindow(this._ui).Show($"You do not have {this._key.Gold} gold",
                            Done);
                    }
                    else if (party.Items.Count >= Party.MaxItems)
                    {
                        new TalkWindow(this._ui).Show("You do not have space in your inventory for the key",
                            Done);
                    }
                    else
                    {
                        party.Gold -= this._key.Gold;
                        party.Items.Add(new ItemInstance(this._key));
                        new TalkWindow(this._ui).Show("Thank you come again!", Done);
                    }
                }
                else
                {
                    this.GameState.IsPaused = false;
                }
            });

            return true;
        }
    }
}    