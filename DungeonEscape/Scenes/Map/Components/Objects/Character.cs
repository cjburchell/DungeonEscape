using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Character : Sprite
    {
        private readonly TalkWindow talkWindow;
        private readonly string text;

        public Character(TmxObject tmxObject, TmxMap map, TalkWindow talkWindow, IGame gameState, AstarGridGraph graph) : base(tmxObject, map, gameState, graph)
        {
            this.talkWindow = talkWindow;
            this.text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : tmxObject.Name;
        }

        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            this.talkWindow.Show(this.text, () => this.gameState.IsPaused = false);
            return true;
        }
    }
}