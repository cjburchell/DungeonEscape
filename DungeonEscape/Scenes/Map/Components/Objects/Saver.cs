namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Linq;
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;
    using UI;

    public class Saver : Sprite
    {
        private readonly UiSystem _ui;
        
        public Saver(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UiSystem ui) : base(tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
        }
        
        public override bool OnAction(Party party)
        {
            this.GameState.IsPaused = true;
            var questionWindow = new QuestionWindow(this._ui);
            questionWindow.Show("Would you like me to record your deeds?", accepted =>
            {
                if (accepted)
                {
                    this.GameState.ReloadSaveGames();
                    var saveWindow = new SaveWindow(this._ui);
                    saveWindow.Show(this.GameState.GameSaves, save =>
                        {
                            if (save == null)
                            {
                                this.GameState.IsPaused = false;
                                return;
                            }
                            
                            this.GameState.Party.SavedMapId = this.GameState.Party.CurrentMapId;
                            this.GameState.Party.SavedPoint = this.GameState.Party.CurrentPosition;
                            save.Party = this.GameState.Party;
                            save.MapStates = this.GameState.MapStates;
                            save.Time = DateTime.Now;
                            this.GameState.Save();
                            new TalkWindow(this._ui).Show($"It has been recorded\nYou have {party.Members.First().NextLevel} xp\nto the next level",
                                () =>
                                {
                                    this.GameState.IsPaused = false;
                                });
                        }
                        );
                    
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