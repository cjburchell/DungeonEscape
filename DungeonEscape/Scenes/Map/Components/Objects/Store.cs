using DungeonEscape.Scenes.Map.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Store : Sprite
    {
        private readonly TalkWindow talkWindow;

        public Store(TmxObject tmxObject, TmxMap map, IGame gameState, AstarGridGraph graph, TalkWindow talkWindow) : base(tmxObject, map, gameState, graph)
        {
            this.talkWindow = talkWindow;
        }
        
        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            this.talkWindow.Show("Sorry, I have nothing for sale right now.", () => { this.gameState.IsPaused = false;});
            return true;
        }
    }
}