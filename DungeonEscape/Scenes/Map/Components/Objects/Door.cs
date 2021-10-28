using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using Nez;

    public class Door: SolidObject
    {
        private readonly UISystem ui;
        private readonly int level;
        private bool isOpen
        {
            get =>
                this.tmxObject.Properties.ContainsKey("IsOpen") &&
                bool.Parse(this.tmxObject.Properties["IsOpen"]);

            set => this.tmxObject.Properties["IsOpen"] = value.ToString();
        }

        public Door(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, UISystem ui, IGame gameState) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile, gameState)
        {
            this.ui = ui;
            this.level = tmxObject.Properties.ContainsKey("DoorLevel") ? int.Parse(tmxObject.Properties["DoorLevel"]) : 0;
        }

        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.isOpen);
            this.SetEnableCollider(!this.isOpen);
        }

        public override bool OnAction(Party party)
        {
            if (this.isOpen)
            {
                return false;
            }

            if (!party.CanOpenDoor(this.level))
            {

                this.gameState.IsPaused = true;
                var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                talkWindow.Show("Unable to open door", () =>
                {
                    this.gameState.IsPaused = false;
                });
                return true;
            }

            this.SetEnableCollider(false);
            this.DisplayVisual(false);
            this.isOpen = true;
            this.Collideable = false;
            return true;
        }
    }
}