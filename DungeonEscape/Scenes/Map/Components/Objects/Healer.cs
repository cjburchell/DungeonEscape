namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

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
            var questionWindow = new QuestionWindow(this.ui);
            var goldWindow = new GoldWindow(party, this.ui.Canvas);
            goldWindow.ShowWindow();
            questionWindow.Show($"Would you like to be healed\nFor {this.cost} gold?", accepted =>
            {
                if (accepted)
                {
                    void Done()
                    {
                        goldWindow.CloseWindow();
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
                        
                        new TalkWindow(this.ui).Show("Thank you come again!", Done);
                    }
                    else
                    {
                        new TalkWindow(this.ui).Show($"You do not have {this.cost} gold", Done);
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