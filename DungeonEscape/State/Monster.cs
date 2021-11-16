// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework.Graphics;
    using Newtonsoft.Json;
    using Nez.Tiled;

    public class Monster
    {
        public Monster()
        {
            
        }

        public void Setup(TmxTilesetTile tile, IEnumerable<Spell> spells)
        {
            this.Image = tile.Image.Texture;
            this.Spells = this.SpellList.Select(spellId => spells.FirstOrDefault(item => item.Id == spellId))
                .Where(spell => spell != null).ToList();
        }
        
        public Monster(TmxTilesetTile tile, IEnumerable<Spell> spells) : this()
        {
            this.Setup(tile, spells);
        }

        public int Id { get; set; }
        
        public int Magic { get; set; }

        public int MinLevel { get; set;}

        [JsonIgnore] public Texture2D Image { get; private set; }

        [JsonIgnore]
        public List<Spell> Spells { get; private set; } = new List<Spell>();
        
        [JsonProperty("Spells")]
        public List<int> SpellList { get; set; } = new List<int>();

        public int Agility { get; set;}

        public int Defence { get; set;}

        public int XP { get; set;}

        public int HealthConst { get; set;}
        
        public int Gold { get;  set;}

        public int Health { get;  set;}

        public int Attack { get;  set;}

        public string Name { get;  set;}
    }
}