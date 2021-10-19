using DungeonEscape.Scenes.Map.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Healer : Sprite
    {
        private readonly QuestionWindow questionWindow;
        private readonly TalkWindow talkWindow;
        private readonly int cost;

        public Healer(TmxObject tmxObject, TmxMap map, IGame gameState, AstarGridGraph graph, QuestionWindow questionWindow, TalkWindow talkWindow) : base(tmxObject, map, gameState, graph)
        {
            this.questionWindow = questionWindow;
            this.talkWindow = talkWindow;
            this.cost = tmxObject.Properties.ContainsKey("Cost") ? int.Parse(tmxObject.Properties["Cost"]) : 25;
        }
        
        public override bool OnAction(Player player)
        {
            this.gameState.IsPaused = true;
            this.questionWindow.Show($"Would you like to be healed\nFor {this.cost} gold?", accepted =>
            {
                if (accepted)
                {
                    if (player.Gold >= this.cost)
                    {
                        player.Gold -= this.cost;
                        player.Health = player.MaxHealth;
                        player.Magic = player.MaxMagic;
                        this.talkWindow.ShowText("Thank you come again!", () => { this.gameState.IsPaused = false;});
                    }
                    else
                    {
                        this.talkWindow.ShowText($"You do not have {this.cost} gold", () => { this.gameState.IsPaused = false;});
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