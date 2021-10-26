using DungeonEscape.Scenes.Common.Components.UI;
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
        
        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            this.questionWindow.Show($"Would you like to be healed\nFor {this.cost} gold?", accepted =>
            {
                if (accepted)
                {
                    if (party.Gold >= this.cost)
                    {
                        party.Gold -= this.cost;
                        foreach (var partyMember in party.Members)
                        {
                            partyMember.Health = partyMember.MaxHealth;
                            partyMember.Magic = partyMember.MaxMagic;
                        }
                        
                        this.talkWindow.Show("Thank you come again!", () => { this.gameState.IsPaused = false;});
                    }
                    else
                    {
                        this.talkWindow.Show($"You do not have {this.cost} gold", () => { this.gameState.IsPaused = false;});
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