using System;
using System.Collections.Generic;
using Nez.Tiled;

namespace DungeonEscape.State
{

    public enum SpellType
    {
        Heal = 0,
        Outside = 1,
        Fireball = 2,
        Lighting =  3,
        Return = 4,
    }

    public class Spell
    {
        private static readonly List<SpellType> encounterSpells = new List<SpellType> {SpellType.Heal, SpellType.Fireball, SpellType.Lighting};
        private static readonly List<SpellType> nonEncounterSpells = new List<SpellType> {SpellType.Heal, SpellType.Outside, SpellType.Return};
        
        public override string ToString()
        {
            return this.Name;
        }
        

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
        
        public bool IsNonEncounterSpell => nonEncounterSpells.Contains(this.Type);
        
        public bool IsEncounterSpell => encounterSpells.Contains(this.Type);

        public int Power { get; }

        public int Cost { get; }

        public int MinLevel { get; }

        public SpellType Type { get; }

        public string Name { get; }
    }
}