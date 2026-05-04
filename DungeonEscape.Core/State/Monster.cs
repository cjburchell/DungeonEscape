using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Redpoint.DungeonEscape.State
{
    public class Monster
    {
        public int ImageId { get; set; }
        public int MinLevel { get; set; }
        public int GroupSize { get; set; }

        [JsonProperty("Spells")]
        public List<string> SpellList { get; set; }

        [JsonProperty("Skills")]
        public List<string> SkillList { get; set; }

        public List<string> Items { get; set; }
        public int Agility { get; set; }
        public int Defence { get; set; }
        public ulong Xp { get; set; }
        public int Gold { get; set; }
        public int HealthConst { get; set; }
        public int HealthRandom { get; set; }
        public int HealthTimes { get; set; }
        public int MagicTimes { get; set; }
        public int MagicConst { get; set; }
        public int MagicRandom { get; set; }
        public int Attack { get; set; }
        public string Name { get; set; }
        public int MagicDefence { get; set; }

        [JsonProperty("Biomes", ItemConverterType = typeof(StringEnumConverter))]
        public List<Biome> Biomes { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Rarity Rarity { get; set; }

        public Monster()
        {
            SpellList = new List<string>();
            SkillList = new List<string>();
            Items = new List<string>();
            HealthConst = 1;
            HealthTimes = 1;
            MagicTimes = 1;
            Rarity = Rarity.Common;
        }

        public bool InBiome(Biome biome)
        {
            return Biomes != null && Biomes.Any() && Biomes.Contains(biome);
        }
    }
}
