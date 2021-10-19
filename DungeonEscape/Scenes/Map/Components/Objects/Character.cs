using DungeonEscape.Scenes.Map.Components.UI;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Character : Sprite
    {
        private readonly TalkWindow talkWindow;
        private string text;

        public Character(TmxObject tmxObject, TmxMap map, TalkWindow talkWindow, IGame gameState, AstarGridGraph graph) : base(tmxObject, map, gameState, graph)
        {
            this.talkWindow = talkWindow;
            this.text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : tmxObject.Name;
        }

        public override bool OnAction(Player player)
        {
            this.talkWindow.ShowText(this.text);
            return true;
        }
    }
}