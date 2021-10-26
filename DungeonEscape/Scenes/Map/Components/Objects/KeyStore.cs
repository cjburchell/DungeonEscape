using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.Scenes.Map.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class KeyStore : Sprite
    {
        private readonly QuestionWindow questionWindow;
        private readonly TalkWindow talkWindow;
        private Item key;

        public KeyStore(TmxObject tmxObject, TmxMap map, IGame gameState, AstarGridGraph graph, QuestionWindow questionWindow, TalkWindow talkWindow) : base(tmxObject, map, gameState, graph)
        {
            this.questionWindow = questionWindow;
            this.talkWindow = talkWindow;
            var cost = tmxObject.Properties.ContainsKey("Cost") ? int.Parse(tmxObject.Properties["Cost"]) : 250;
            var level= tmxObject.Properties.ContainsKey("Level") ? int.Parse(tmxObject.Properties["Level"]) : 0;
            this.key = new Item("Content/images/items/key.png", "Key", ItemType.Key, cost, level);
        }
        
        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            this.questionWindow.Show($"Would you like to buy a key\nFor {this.key.Gold} gold?", accepted =>
            {
                if (accepted)
                {
                    if (party.Gold < this.key.Gold)
                    {
                        this.talkWindow.Show($"You do not have {this.key.Gold} gold",
                            () => { this.gameState.IsPaused = false; });
                        return;
                    }

                    if (party.Items.Count >= Party.MaxItems)
                    {
                        this.talkWindow.Show($"You do not have space in your inventory for the key",
                            () => { this.gameState.IsPaused = false; });
                        return;
                    }

                    party.Gold -= this.key.Gold;
                    party.Items.Add(new ItemInstance(this.key));
                    this.talkWindow.Show("Thank you come again!", () => { this.gameState.IsPaused = false; });
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