using DungeonEscape.Scenes.Map.Components.UI;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Character : Sprite
    {
        private readonly TalkWindow talkWindow;
        private string text;

        public Character(TmxObject tmxObject, TmxMap map, TalkWindow talkWindow, IGame gameState) : base(tmxObject, map, gameState)
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