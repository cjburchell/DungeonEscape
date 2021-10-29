using System;
using Nez.Tiled;
using Microsoft.Xna.Framework.Graphics;
using Nez;

namespace DungeonEscape.State
{
    public class Item
    {
        public override string ToString()
        {
            return this.Name;
        }

        public Item(TmxTilesetTile tile)
        {
            if (Enum.TryParse(tile.Type, out ItemType type))
            {
                this.Type = type;
            }

            this.Id = tile.Id;
            this.Name = tile.Properties["Name"];
            this.Defence = int.Parse(tile.Properties["Defence"]);
            this.Health = int.Parse(tile.Properties["Health"]);
            this.Attack = int.Parse(tile.Properties["Attack"]);
            this.Agility = int.Parse(tile.Properties["Agility"]);
            this.Gold = int.Parse(tile.Properties["Cost"]);
            this.MinLevel = int.Parse(tile.Properties["MinLevel"]);
            this.ImageSource = tile.Image.Source;
            this.Image = tile.Image.Texture;
        }

        public int Id { get; }

        public Item(string image, string name, ItemType type, int gold, int minLevel)
        {
            if (!string.IsNullOrEmpty(image))
            {
                this.ImageSource = image;
                this.Image = Texture2D.FromFile(Core.GraphicsDevice,this.ImageSource);
            }
            this.Name = name;
            this.Gold = gold;
            this.MinLevel = minLevel;
            this.Type = type;
        }


        public Texture2D Image { get; }
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