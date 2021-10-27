using System.Linq;
using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using Nez;

    public class Saver : Sprite
    {
        private readonly UISystem ui;
        
        public Saver(TmxObject tmxObject, TmxMap map, IGame gameState, AstarGridGraph graph, UISystem ui) : base(tmxObject, map, gameState, graph)
        {
            this.ui = ui;
        }
        
        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            var questionWindow = this.ui.Canvas.AddComponent(new QuestionWindow(this.ui));
            questionWindow.Show("Would you like me to record your deeds?", accepted =>
            {
                if (accepted)
                {
                    // TODO: Save the game
                    var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                    talkWindow.Show($"It has been recorded\nYou have {party.Members.First().NextLevel} xp\nto the next level",
                        () =>
                        {
                            this.gameState.IsPaused = false;
                        });
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