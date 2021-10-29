using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;
using Nez;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Healer : Sprite
    {
        private readonly UISystem ui;
        private readonly int cost;

        public Healer(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UISystem ui) : base(tmxObject, state, map, gameState, graph)
        {
            this.ui = ui;
            this.cost = tmxObject.Properties.ContainsKey("Cost") ? int.Parse(tmxObject.Properties["Cost"]) : 25;
        }
        
        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            var questionWindow = this.ui.Canvas.AddComponent(new QuestionWindow(this.ui));
            questionWindow.Show($"Would you like to be healed\nFor {this.cost} gold?", accepted =>
            {
                if (accepted)
                {
                    void Done()
                    {
                        this.gameState.IsPaused = false;
                    }

                    if (party.Gold >= this.cost)
                    {
                        party.Gold -= this.cost;
                        foreach (var partyMember in party.Members)
                        {
                            partyMember.Health = partyMember.MaxHealth;
                            partyMember.Magic = partyMember.MaxMagic;
                        }
                        
                        var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                        talkWindow.Show("Thank you come again!", Done);
                    }
                    else
                    {
                        var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                        talkWindow.Show($"You do not have {this.cost} gold", Done);
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