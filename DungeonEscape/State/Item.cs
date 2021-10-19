using System;
using Nez.Tiled;

namespace DungeonEscape.State
{
    public class Item
    {
        public Item(TmxTilesetTile tile)
        {
            if (Enum.TryParse(tile.Type, out ItemType type))
            {
                this.Type = type;
            }

            Name = tile.Properties["Name"];
            Defence = int.Parse(tile.Properties["Defence"]);
            Health = int.Parse(tile.Properties["Health"]);
            Attack = int.Parse(tile.Properties["Attack"]);
            Agility = int.Parse(tile.Properties["Agility"]);
            this.Gold = int.Parse(tile.Properties["Cost"]);
            MinLevel = int.Parse(tile.Properties["MinLevel"]);
        }

        public Item(string image, string name, ItemType type, int gold)
        {
            this.Name = name;
            this.Gold = gold;
            this.Type = type;
        }

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