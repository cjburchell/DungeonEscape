using System.Collections.Generic;

namespace GameFile
{
    using Newtonsoft.Json;

    public class Monster
    {
        public int Health { get; set; }
        public int HealthConst { get; set; }
        public int Attack { get; set; }
        public int XP { get; set; }
        public int Gold { get; set; }
        public List<int> Spells { get; set; }
        public string Name { get; set; }
        public int Defence { get; set; }
        public int Agility { get; set; }

        [JsonIgnore]
        public TileInfo Info { get; set; }

        public int Id => Info.Id;

        public int MinLevel { get; set; }
        public int Magic { get; set; }
    }
}