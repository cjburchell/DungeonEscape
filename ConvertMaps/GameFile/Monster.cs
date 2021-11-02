using System.Collections.Generic;

namespace GameFile
{
    public class Monster
    {
        public int Health { get; set; }
        public int HealthConst { get; set; }
        public int Attack { get; set; }
        public int XP { get; set; }
        public int Gold { get; set; }
        public List<SpriteSpell> Spells { get; set; }
        public string Name { get; set; }
        public int Defence { get; set; }
        public int Agility { get; set; }
        
        public TileInfo Info { get; set; }
        public int MinLevel { get; set; }
        public int Magic { get; set; }
    }
}