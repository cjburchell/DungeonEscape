namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;

    public class GoldWindow : BasicWindow, IUpdatable
    {
        private readonly Party _party;
        private Label _goldLabel;

        public GoldWindow(Party party, UICanvas canvas, ISounds sounds) : this(party, canvas, sounds,
            new Point(MapScene.ScreenWidth - 155, MapScene.ScreenHeight - 55))
        {
        }

        public GoldWindow(Party party, UICanvas canvas, ISounds sounds, Point position) : base(new UiSystem(canvas, sounds, true),
            position, 150, 50, false)
        {
            this._party = party;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this._goldLabel = new Label($"Gold: {this._party.Gold}", Skin).SetAlignment(Align.Center);
            this.Window.AddElement(this._goldLabel);
        }

        public void Update()
        {
            this._goldLabel.SetText($"Gold: {this._party.Gold}");
        }
    }
}