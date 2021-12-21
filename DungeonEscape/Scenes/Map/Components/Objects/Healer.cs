namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

    public class Healer : Sprite
    {
        private readonly UiSystem _ui;
        private readonly int _cost;

        public Healer(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UiSystem ui) : base(tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
            this._cost = tmxObject.Properties.ContainsKey("Cost") ? int.Parse(tmxObject.Properties["Cost"]) : 25;
        }
        
        public override bool OnAction(Party party)
        {
            this.GameState.IsPaused = true;
            var questionWindow = new QuestionWindow(this._ui);
            var goldWindow = new GoldWindow(party, this._ui.Canvas, this._ui.Sounds);
            goldWindow.ShowWindow();
            questionWindow.Show($"Would you like to be healed\nFor {this._cost} gold?", accepted =>
            {
                if (accepted)
                {
                    void Done()
                    {
                        goldWindow.CloseWindow();
                        this.GameState.IsPaused = false;
                    }

                    if (party.Gold >= this._cost)
                    {
                        party.Gold -= this._cost;
                        foreach (var partyMember in party.Members)
                        {
                            partyMember.Health = partyMember.MaxHealth;
                            partyMember.Magic = partyMember.MaxMagic;
                        }
                        
                        new TalkWindow(this._ui).Show("Thank you come again!", Done);
                    }
                    else
                    {
                        new TalkWindow(this._ui).Show($"You do not have {this._cost} gold", Done);
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