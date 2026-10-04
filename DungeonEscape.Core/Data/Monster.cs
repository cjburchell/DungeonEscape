using Redpoint.DungeonEscape.State;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Redpoint.DungeonEscape.Data
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
        public ulong Xp { get; set; }
        public int Gold { get; set; }
        public int HitPoints { get; set; }
        public int HitPointRandom { get; set; }
        public int HitPointTimes { get; set; }
        public int MagicTimes { get; set; }
        public int MagicConst { get; set; }
        public int MagicRandom { get; set; }
        public string Name { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
        public int ArmorClass { get; set; }
        public int ProficiencyBonus { get; set; }
        public int AttackBonus { get; set; }
        public int DamageDice { get; set; }
        public int DamageDie { get; set; }
        public int DamageBonus { get; set; }
        public string ChallengeRating { get; set; }
        public string DndMonster { get; set; }
        public string Size { get; set; }
        public string MonsterType { get; set; }
        public string Alignment { get; set; }
        public string Speed { get; set; }
        public string Senses { get; set; }
        public string Languages { get; set; }
        public string HitDice { get; set; }
        public List<MonsterTrait> Traits { get; set; }
        public List<MonsterAction> Actions { get; set; }

        [JsonProperty("Biomes", ItemConverterType = typeof(StringEnumConverter))]
        public List<Biome> Biomes { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Rarity Rarity { get; set; }

        public Monster()
        {
            SpellList = new List<string>();
            SkillList = new List<string>();
            Items = new List<string>();
            Traits = new List<MonsterTrait>();
            Actions = new List<MonsterAction>();
            HitPoints = 1;
            HitPointTimes = 1;
            MagicTimes = 1;
            Rarity = Rarity.Common;
        }

        public bool InBiome(Biome biome)
        {
            return Biomes != null && Biomes.Any() && Biomes.Contains(biome);
        }
    }
}
