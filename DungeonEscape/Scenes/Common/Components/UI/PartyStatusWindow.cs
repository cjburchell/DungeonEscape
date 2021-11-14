using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    using Nez;

    public class PartyStatusWindow: BasicWindow, IUpdatable
    {
        private Table statusTable;
        private readonly Party party;

        public PartyStatusWindow(Party party, UICanvas canvas) : this(party, canvas, new Point(10, 10))
        {
        }
        
        public PartyStatusWindow(Party party, UICanvas canvas, Point position) : base(new UISystem(canvas, true), "Status",
            position, 90, 100, false)
        {
            this.party = party;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.statusTable = new Table();
            var table = this.Window.AddElement(new Table());
            // layout
            table.SetFillParent(true);
            table.Row();
            table.Add(this.statusTable).Expand().Top().Left().SetPadLeft(10);
            
            this.UpdateStatus();
        }

        private void UpdateStatus()
        {
            this.Window.SetWidth(this.party.Members.Count * 50 + 40);
            this.statusTable.ClearChildren();
            this.statusTable.Row();
            foreach (var partyMember in this.party.Members)
            {
                var nameLabel = new Label(partyMember.Name.Substring(0,4), Skin).SetAlignment(Align.Center);
                this.statusTable.Add(nameLabel).Width(50);
            }
            
            this.statusTable.Row().SetPadTop(5);
            foreach (var partyMember in this.party.Members)
            {
                var healthLabel = new Label($"H{partyMember.Health}", Skin).SetAlignment(Align.Center);
                this.statusTable.Add(healthLabel).Width(50);
            }
            
            this.statusTable.Row().SetPadTop(5);
            foreach (var partyMember in this.party.Members)
            {
                var magicLabel = new Label($"M{partyMember.Magic}", Skin).SetAlignment(Align.Center);
                this.statusTable.Add(magicLabel).Width(50);
            }
            
            this.statusTable.Row().SetPadTop(5);
            foreach (var partyMember in this.party.Members)
            {
                this.statusTable.Add(new Label($"L{partyMember.Level}", Skin).SetAlignment(Align.Center)).Width(50);
            }

            this.statusTable.Invalidate();
        }

        public void Update()
        {
            this.UpdateStatus();
        }
    }
}