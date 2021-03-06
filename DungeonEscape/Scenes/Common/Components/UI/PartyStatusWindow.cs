namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;

    public class PartyStatusWindow: BasicWindow, IUpdatable
    {
        private Table _statusTable;
        private readonly Party _party;

        public PartyStatusWindow(Party party, UICanvas canvas, ISounds sounds) : this(party, canvas, sounds, new Point(10, 10))
        {
        }

        private PartyStatusWindow(Party party, UICanvas canvas, ISounds sounds, Point position) : base(new UiSystem(canvas, sounds,true), null,
            position, 90, 110, false)
        {
            this._party = party;
            var texture = canvas.Entity.Scene.Content.LoadTexture("Content/images/sprites/hero.png");
            foreach (var member in this._party.Members)
            {
                member.SetupImage(texture);
            }
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this._statusTable = new Table();
            this.Window.AddElement(this._statusTable);
            this.UpdateStatus();
        }

        private void UpdateStatus()
        {
            const int heroWidth = MapScene.DefaultTileSize;
            const int statusWidth = 80;
            const int statusItemWidth = statusWidth/2;
            const int padding = 5;
            
            var windowWidth = this._party.Members.Count * (heroWidth + statusWidth + 15);
            this.Window.SetWidth(windowWidth);
            this._statusTable.ClearChildren();
            this._statusTable.SetFillParent(true);
            foreach (var member in this._party.Members)
            {
                member.Image.SetAlignment(Align.Center);
                this._statusTable.Add(member.Image).Width(heroWidth).SetPadLeft(5).SetPadRight(5);
                var memberStatus = new Table();
                memberStatus.Row().SetPadTop(padding);
                memberStatus.Add(new Label(member.Name.Length < 10 ? member.Name : member.Name.Substring(0, 10), Skin).SetAlignment(Align.Left)).Width(statusWidth).SetColspan(2).SetPadRight(5);
                memberStatus.Row().SetPadTop(padding);
                memberStatus.Add(new Label("HP", Skin).SetAlignment(Align.Left)).Width(statusItemWidth);
                memberStatus.Add(new Label($"{member.Health}", Skin).SetAlignment(Align.Right)).Width(statusItemWidth).SetPadRight(5);
                memberStatus.Row().SetPadTop(padding);
                memberStatus.Add( new Label("MP", Skin).SetAlignment(Align.Left)).Width(statusItemWidth);
                memberStatus.Add( new Label($"{member.Magic}", Skin).SetAlignment(Align.Right)).Width(statusItemWidth).SetPadRight(5);
                memberStatus.Row().SetPadTop(padding);
                memberStatus.Add(new Label($"{member.Class.ToString().Substring(0, 3)}:", Skin).SetAlignment(Align.Left)).Width(statusItemWidth).SetPadRight(5);
                memberStatus.Add(new Label($"{member.Level}", Skin).SetAlignment(Align.Right)).Width(statusItemWidth).SetPadRight(5);
                this._statusTable.Add(memberStatus).Width(statusWidth);
            }
            this._statusTable.Invalidate();
        }

        public void Update()
        {
            this.UpdateStatus();
        }
    }
}