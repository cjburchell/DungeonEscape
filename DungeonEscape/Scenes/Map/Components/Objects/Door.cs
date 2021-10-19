using DungeonEscape.Scenes.Map.Components.UI;
using DungeonEscape.State;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Door: SolidObject
    {
        private readonly TalkWindow talkWindow;
        private readonly int level;
        private bool isOpen
        {
            get =>
                this.tmxObject.Properties.ContainsKey("IsOpen") &&
                bool.Parse(this.tmxObject.Properties["IsOpen"]);

            set => this.tmxObject.Properties["IsOpen"] = value.ToString();
        }

        public Door(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, TalkWindow talkWindow, IGame gameState) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile, gameState)
        {
            this.talkWindow = talkWindow;
            this.level = tmxObject.Properties.ContainsKey("DoorLevel") ? int.Parse(tmxObject.Properties["DoorLevel"]) : 0;
        }

        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.isOpen);
            this.SetEnableCollider(!this.isOpen);
        }

        public override bool OnAction(Player player)
        {
            if (this.isOpen)
            {
                return false;
            }

            if (!player.CanOpenDoor(this.level))
            {

                this.gameState.IsPaused = true;
                this.talkWindow.ShowText("Unable to open door", () => this.gameState.IsPaused = false);
                return false;
            }

            this.SetEnableCollider(false);
            this.DisplayVisual(false);
            this.isOpen = true;
            this.Collideable = false;
            return true;
        }
    }
}