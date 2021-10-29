using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using Nez;

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
            var questionWindow = this.ui.Canvas.AddComponent(new QuestionWindow(this.ui));
            questionWindow.Show($"Would you like to buy a key\nFor {this.key.Gold} gold?", accepted =>
            {
                if (accepted)
                {
                    void Done()
                    {
                        this.gameState.IsPaused = false;
                    }

                    if (party.Gold < this.key.Gold)
                    {
                        var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                        talkWindow.Show($"You do not have {this.key.Gold} gold",
                            Done);
                    }
                    else if (party.Items.Count >= Party.MaxItems)
                    {
                        var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                        talkWindow.Show("You do not have space in your inventory for the key",
                            Done);
                    }
                    else
                    {
                        party.Gold -= this.key.Gold;
                        party.Items.Add(new ItemInstance(this.key));
                        var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                        talkWindow.Show("Thank you come again!", Done);
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