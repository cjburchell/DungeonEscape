// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework.Graphics;
    using Newtonsoft.Json;
    using Nez;
    using Nez.Tiled;

    public class Monster
    {
        public Monster()
        {
            
        }

        public void Setup(TmxTilesetTile tile)
        {
            this.Image = tile.Image.Texture;
            this.Flash = CreateFlashImage(this.Image);
        }

        public static Texture2D CreateFlashImage(Texture2D image)
        {
            var flash = new Texture2D(Core.GraphicsDevice, image.Width, image.Height);
            var data = new byte[image.Width*image.Height*4];
            image.GetData(data);
            // create a silhouette of the monster
            for (var index = 0; index < data.Length; index += 4)
            {
                if (data[index + 3] == 0)
                {
                    continue;
                }

                data[index + 3] = 128;
                data[index] = 255;
                data[index + 1] = 255;
                data[index + 2] = 255;
            }
            
            flash.SetData(data);

            return flash;
        }
        
        public Monster(TmxTilesetTile tile) : this()
        {
            this.Setup(tile);
        }

        public int Id { get; set; }
        
        public int Magic { get; set; }

        public int MinLevel { get; set;}
        
        public int MagicConst { get; set;}

        [JsonIgnore] public Texture2D Image { get; private set; }
        [JsonIgnore] public Texture2D Flash { get; private set; }
        
        
        [JsonProperty("Spells")]
        public List<int> SpellList { get; set; } = new List<int>();

        public int Agility { get; set;}

        public int Defence { get; set;}

        public ulong Xp { get; set;}

        public int HealthConst { get; set;}
        
        public int Gold { get;  set;}

        public int Health { get;  set;}

        public int Attack { get;  set;}

        public string Name { get;  set;}
    }
}