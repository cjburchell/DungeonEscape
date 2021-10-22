using System;
using Nez.Tiled;

namespace DungeonEscape.State
{
    public class Item
    {
        private readonly TmxTilesetTile tile;

        public Item(TmxTilesetTile tile)
        {
            this.tile = tile;
            if (Enum.TryParse(tile.Type, out ItemType type))
            {
                this.Type = type;
            }

            this.Name = tile.Properties["Name"];
            this.Defence = int.Parse(tile.Properties["Defence"]);
            this.Health = int.Parse(tile.Properties["Health"]);
            this.Attack = int.Parse(tile.Properties["Attack"]);
            this.Agility = int.Parse(tile.Properties["Agility"]);
            this.Gold = int.Parse(tile.Properties["Cost"]);
            this.MinLevel = int.Parse(tile.Properties["MinLevel"]);
        }

        public Item(string image, string name, ItemType type, int gold, int minLevel)
        {
            this.ImageSource = image;
            this.Name = name;
            this.Gold = gold;
            this.MinLevel = minLevel;
            this.Type = type;
        }

        public string ImageSource { get; }
        public ItemType Type { get; } = ItemType.Unknown;
        public string Name { get; }
        public int Defence { get; }
        public int Health { get; }
        public int Attack { get; }
        public int Agility { get; }
        public int Gold { get; }
        public int MinLevel { get; }
    }

}