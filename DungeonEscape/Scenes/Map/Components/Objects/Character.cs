namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

    public class Character : Sprite
    {
        private readonly UiSystem _ui;
        private readonly string _text;

        public Character(TmxObject tmxObject, SpriteState state, TmxMap map, UiSystem ui, IGame gameState, AstarGridGraph graph) : base(tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
            this._text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : null;
        }

        public override bool OnAction(Party party)
        {
            if (this._text == null)
            {
                return false;
            }
            
            this.GameState.IsPaused = true;
            new TalkWindow(this._ui).Show(this._text, () =>
            {
                this.GameState.IsPaused = false;
            });
            return true;

        }
    }
}