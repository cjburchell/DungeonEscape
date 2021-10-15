using DungeonEscape.Scenes.Map.Components.UI;
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

        public Door(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, TalkWindow talkWindow) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile)
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
                
                this.talkWindow.ShowText($"Unable to open chest");
                return false;
            }

            this.SetEnableCollider(false);
            this.DisplayVisual(false);
            this.isOpen = true;
            return true;
        }
    }
}