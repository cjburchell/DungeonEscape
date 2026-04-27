using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Redpoint.DungeonEscape.State
{
    public class Item
    {
        public static readonly List<ItemType> EquippableItems = new List<ItemType>
        {
            ItemType.Weapon,
            ItemType.Armor
        };

        public override string ToString()
        {
            return Name;
        }

        public string Id { get; set; }
        public int ImageId { get; set; }
        public string QuestId { get; set; }
        public bool StartQuest { get; set; }
        public List<int> ForStage { get; set; }
        public int? NextStage { get; set; }

        [JsonIgnore]
        public bool IsKey
        {
            get { return Skill != null && Skill.Type == SkillType.Open; }
        }

        [JsonProperty("Skill")]
        public string SkillId { get; set; }

        [JsonIgnore]
        public Skill Skill { get; set; }

        [JsonProperty("Slots", ItemConverterType = typeof(StringEnumConverter))]
        public List<Slot> Slots { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType Type { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Rarity Rarity { get; set; }

        public List<StatValue> Stats { get; set; }
        public int Cost { get; set; }
        public int MinLevel { get; set; }

        [JsonProperty("Classes", ItemConverterType = typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }

        [JsonIgnore]
        public bool CanBeSoldInStore
        {
            get { return Type != ItemType.Gold && Type != ItemType.Unknown; }
        }

        [JsonIgnore]
        public string NameWithStats
        {
            get
            {
                var stats = StatString;
                if (Charges != 0)
                {
                    stats = string.IsNullOrEmpty(stats) ? "C" + Charges : stats + ", C" + Charges;
                }

                return Name + " " + stats;
            }
        }

        public int Charges { get; set; }
        public Target Target { get; set; }

        public Item()
        {
            Type = ItemType.Unknown;
            Stats = new List<StatValue>();
            Target = Target.Single;
        }

        public void Setup(IEnumerable<Skill> skills)
        {
            Skill = skills == null ? null : skills.FirstOrDefault(i => i.Name == SkillId);
        }

        public int GetAttribute(StatType statType)
        {
            return Stats.Where(i => i.Type == statType).Sum(stat => stat.Value);
        }

        [JsonIgnore]
        public string StatString
        {
            get
            {
                var stats = "";
                foreach (var stat in Stats.Where(i => i.Value != 0).Select(o => o.Type).Distinct().OrderBy(i => i))
                {
                    var value = GetAttribute(stat);
                    var valueString = value > 0 ? "+" + value : value.ToString();
                    var shortStat = "";

                    switch (stat)
                    {
                        case StatType.Health:
                            shortStat = "H";
                            break;
                        case StatType.Magic:
                            shortStat = "M";
                            break;
                        case StatType.Agility:
                            shortStat = "Ag";
                            break;
                        case StatType.Attack:
                            shortStat = "At";
                            break;
                        case StatType.Defence:
                            shortStat = "D";
                            break;
                        case StatType.MagicDefence:
                            shortStat = "Md";
                            break;
                    }

                    stats = string.IsNullOrEmpty(stats)
                        ? valueString + shortStat
                        : stats + ", " + valueString + shortStat;
                }

                return stats;
            }
        }
    }
}
