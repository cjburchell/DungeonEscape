// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Linq;
    using System.Text.Json.Serialization;
    using Microsoft.Xna.Framework.Graphics;
    using Nez.Textures;
    using Nez.Tiled;

    public class Item
    {
        public override string ToString()
        {
            return this.Name;
        }

        public void Setup(TmxTileset tileset)
        {
            this.Image = tileset.Image != null ? new Sprite(tileset.Image.Texture, tileset.TileRegions[this.ImageId]) : new Sprite(tileset.Tiles[this.ImageId].Image.Texture);
        }

        public int Id { get; set; }
        public int ImageId { get; set; }

        public static Item CreateGold(int value)
        {
            var item = new Item
            {
                Name = "Gold",
                Cost = value,
                MinLevel = 0,
                Type = ItemType.Gold,
                ImageId = 202,
                Id = 0
            };
            
            // gold Image
            var tileSet = Game.LoadTileSet("Content/items.tsx");
            item.Setup(tileSet);

            return item;
        }

        public Sprite Image { get; set; }
        public ItemType Type { get; set; } = ItemType.Unknown;
        public string Name { get; set; }
        public int Defence { get; set; }
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Agility { get; set; }
        public int Cost { get; set; }
        public int MinLevel { get; set; }

        [JsonIgnore]
        public bool CanBeSoldInStore => this.Type == ItemType.Armor ||
                                        this.Type == ItemType.Shield ||
                                        this.Type == ItemType.Weapon ||
                                        this.Type == ItemType.OneUse;
    }

}