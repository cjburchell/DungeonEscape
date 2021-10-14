using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class Door: SolidObject
    {
        private readonly int level;
        private bool isOpen
        {
            get =>
                this.tmxObject.Properties.ContainsKey("IsOpen") &&
                bool.Parse(this.tmxObject.Properties["IsOpen"]);

            set => this.tmxObject.Properties["IsOpen"] = value.ToString();
        }

        public Door(TmxObject tmxObject, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile) : base(tmxObject, gridTileHeight, gridTileWidth, mapTile)
        {
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
            if (this.isOpen || !player.CanOpenDoor(this.level))
            {
                return false;
            }

            this.SetEnableCollider(false);
            this.DisplayVisual(false);
            this.isOpen = true;
            return true;
        }
    }
}