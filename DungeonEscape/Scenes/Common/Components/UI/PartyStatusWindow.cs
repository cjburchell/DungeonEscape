namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;

    public class PartyStatusWindow: BasicWindow, IUpdatable
    {
        private Table _statusTable;
        private readonly Party _party;

        public PartyStatusWindow(Party party, UICanvas canvas) : this(party, canvas, new Point(10, 10))
        {
        }

        private PartyStatusWindow(Party party, UICanvas canvas, Point position) : base(new UiSystem(canvas, true), "Status",
            position, 90, 100, false)
        {
            this._party = party;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this._statusTable = new Table();
            var table = this.Window.AddElement(new Table());
            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(this._statusTable).Expand().Top().Left().SetPadLeft(10);
            
            this.UpdateStatus();
        }

        private void UpdateStatus()
        {
            this.Window.SetWidth(this._party.Members.Count * 50 + 40);
            this._statusTable.ClearChildren();
            this._statusTable.Row();
            foreach (var nameLabel in this._party.Members.Select(partyMember =>
                new Label(partyMember.Name.Length < 4 ? partyMember.Name : partyMember.Name.Substring(0, 4), Skin)
                    .SetAlignment(Align.Center)))
            {
                this._statusTable.Add(nameLabel).Width(50);
            }

            this._statusTable.Row().SetPadTop(5);
            foreach (var healthLabel in this._party.Members.Select(partyMember => new Label($"H{partyMember.Health}", Skin).SetAlignment(Align.Center)))
            {
                this._statusTable.Add(healthLabel).Width(50);
            }
            
            this._statusTable.Row().SetPadTop(5);
            foreach (var magicLabel in this._party.Members.Select(partyMember => new Label($"M{partyMember.Magic}", Skin).SetAlignment(Align.Center)))
            {
                this._statusTable.Add(magicLabel).Width(50);
            }
            
            this._statusTable.Row().SetPadTop(5);
            foreach (var partyMember in this._party.Members)
            {
                this._statusTable.Add(new Label($"L{partyMember.Level}", Skin).SetAlignment(Align.Center)).Width(50);
            }

            this._statusTable.Invalidate();
        }

        public void Update()
        {
            this.UpdateStatus();
        }
    }
}