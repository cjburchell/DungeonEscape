using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using Nez;

    public class Character : Sprite
    {
        private readonly UISystem ui;
        private readonly string text;

        public Character(TmxObject tmxObject, SpriteState state, TmxMap map, UISystem ui, IGame gameState, AstarGridGraph graph) : base(tmxObject, state, map, gameState, graph)
        {
            this.ui = ui;
            this.text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : tmxObject.Name;
        }

        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            new TalkWindow(this.ui, "Talk Npc").Show(this.text, () =>
            {
                this.gameState.IsPaused = false;
            });
            return true;
        }
    }
}