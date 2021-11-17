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
        private readonly UISystem ui;
        
        public Saver(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UISystem ui) : base(tmxObject, state, map, gameState, graph)
        {
            this.ui = ui;
        }
        
        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            var questionWindow = new QuestionWindow(this.ui);
            questionWindow.Show("Would you like me to record your deeds?", accepted =>
            {
                if (accepted)
                {
                    this.gameState.ReloadSaveGames();
                    var saveWindow = new SaveWindow(this.ui);
                    saveWindow.Show(this.gameState.GameSaves, save =>
                        {
                            if (save == null)
                            {
                                this.gameState.IsPaused = false;
                                return;
                            }
                            
                            this.gameState.Party.SavedMapId = this.gameState.Party.CurrentMapId;
                            this.gameState.Party.SavedPoint = this.gameState.Party.CurrentPosition;
                            save.Party = this.gameState.Party;
                            save.MapStates = this.gameState.MapStates;
                            save.Time = DateTime.Now;
                            this.gameState.Save();
                            new TalkWindow(this.ui).Show($"It has been recorded\nYou have {party.Members.First().NextLevel} xp\nto the next level",
                                () =>
                                {
                                    this.gameState.IsPaused = false;
                                });
                        }
                        );
                    
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