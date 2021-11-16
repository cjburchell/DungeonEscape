namespace DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;

    public class GoldWindow : BasicWindow, IUpdatable
    {
        private readonly Party party;
        private Label goldLabel;

        public GoldWindow(Party party, UICanvas canvas) : this(party, canvas,
            new Point(MapScene.ScreenWidth - 155, MapScene.ScreenHeight - 55))
        {
        }

        public GoldWindow(Party party, UICanvas canvas, Point position) : base(new UISystem(canvas, true), "Gold",
            position, 150, 50, false)
        {
            this.party = party;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.goldLabel = new Label($"Gold: {this.party.Gold}", Skin).SetAlignment(Align.Center);
            this.goldLabel.FillParent = true;
            this.Window.AddElement(this.goldLabel);
        }

        public void Update()
        {
            this.goldLabel.SetText($"Gold: {this.party.Gold}");
        }
    }
}