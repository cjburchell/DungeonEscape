using System;
using Nez.Tiled;

namespace DungeonEscape.State
{

    public enum SpellType
    {
        Heal = 0,
        Outside = 1,
        Fireball = 2,
        Lighting =  3
    }
    
    public class Spell
    {
        private TmxTilesetTile tile;
        public int Id => this.tile.Id;
        
        public Spell(TmxTilesetTile tile)
        {
            this.tile = tile;
            this.Name = StringUtils.AddSpacesToSentence(tile.Properties["Name"]);
            this.Type = Enum.Parse<SpellType>(this.tile.Type);
            this.MinLevel = int.Parse(tile.Properties["MinLevel"]);
            this.Cost = int.Parse(tile.Properties["Cost"]);
            this.Power = int.Parse(tile.Properties["Power"]);
        }

        public int Power { get; }

        public int Cost { get; }

        public int MinLevel { get; }

        public SpellType Type { get; }

        public string Name { get; }
    }
}