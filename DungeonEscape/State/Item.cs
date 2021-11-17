// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System;
    using Microsoft.Xna.Framework.Graphics;
    using Nez;
    using Nez.Tiled;

    public class Item
    {
        public override string ToString()
        {
            return this.Name;
        }

        // ReSharper disable once UnusedMember.Global
        public Item()
        {
            
        }

        public void Setup(TmxTilesetTile tile)
        {
            if (Enum.TryParse(tile.Type, out ItemType type))
            {
                this.Type = type;
            }
            
            this.ImageSource = tile.Image.Source;
            this.Image = tile.Image.Texture;
        }

        public int Id { get; set; }

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


        public Texture2D Image { get; set; }
        public string ImageSource { get; set; }
        public ItemType Type { get; set; } = ItemType.Unknown;
        public string Name { get; set; }
        public int Defence { get; set; }
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Agility { get; set; }
        public int Gold { get; set; }
        public int MinLevel { get; set; }
    }

}