using System.Linq;
using DungeonEscape.Scenes.Map.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Saver : Sprite
    {
        private readonly QuestionWindow questionWindow;
        private readonly TalkWindow talkWindow;

        public Saver(TmxObject tmxObject, TmxMap map, IGame gameState, AstarGridGraph graph, QuestionWindow questionWindow, TalkWindow talkWindow) : base(tmxObject, map, gameState, graph)
        {
            this.questionWindow = questionWindow;
            this.talkWindow = talkWindow;
        }
        
        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            this.questionWindow.Show($"Would you like me to record your deeds?", accepted =>
            {
                if (accepted)
                {
                    // TODO: Save the game
                    this.talkWindow.Show($"It has been recorded\nYou have {party.Members.First().NextLevel} xp\nto the next level", () => { this.gameState.IsPaused = false;});
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