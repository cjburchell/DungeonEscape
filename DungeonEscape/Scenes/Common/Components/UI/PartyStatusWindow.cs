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

        private PartyStatusWindow(Party party, UICanvas canvas, ISounds sounds, Point position) : base(new UiSystem(canvas, sounds,true),
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
                string textStyle = null;
                if (member.IsDead)
                {
                    textStyle = "red_label";
                }
                else if (member.Health < member.MaxHealth / 10)
                {
                    textStyle = "orange_label";
                }
                
                this._statusTable.Add(member.Image).Width(heroWidth).SetPadLeft(5).SetPadRight(5).Center();
                var memberStatus = new Table();
                memberStatus.Row().SetPadTop(padding);
                memberStatus.Add(new Label(member.Name.Length < 10 ? member.Name : member.Name[..10], Skin, textStyle)).Left().Width(statusWidth).SetColspan(2).SetPadRight(5);
                memberStatus.Row().SetPadTop(padding);
                memberStatus.Add(new Label("HP:", Skin, textStyle)).Left().Width(statusItemWidth);
                memberStatus.Add(new Label($"{member.Health}", Skin, textStyle)).Right().Width(statusItemWidth).SetPadRight(5);
                memberStatus.Row().SetPadTop(padding);
                if (member.MaxMagic != 0)
                {
                    memberStatus.Add( new Label("MP:", Skin, textStyle)).Left().Width(statusItemWidth);
                    memberStatus.Add( new Label($"{member.Magic}", Skin, textStyle)).Right().Width(statusItemWidth).SetPadRight(5);
                    memberStatus.Row().SetPadTop(padding);
                }
                else
                {
                    memberStatus.Add( new Label(" ", Skin, textStyle)).Left().Width(statusItemWidth);
                    memberStatus.Row().SetPadTop(padding);
                }
                memberStatus.Add(new Label($"{member.Class.ToString()[..3]}:", Skin, textStyle)).Left().Width(statusItemWidth).SetPadRight(5);
                memberStatus.Add(new Label($"{member.Level}", Skin, textStyle)).Right().Width(statusItemWidth).SetPadRight(5);
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