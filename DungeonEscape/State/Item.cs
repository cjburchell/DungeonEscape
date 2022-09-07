// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Nez.Textures;
    using Nez.Tiled;

    public class Item
    {
        public override string ToString()
        {
            return this.Name;
        }

        public void Setup(TmxTileset tileset, IEnumerable<Skill> skills)
        {
            this.Image = tileset.Image != null
                ? new Sprite(tileset.Image.Texture, tileset.TileRegions[this.ImageId])
                : new Sprite(tileset.Tiles[this.ImageId].Image.Texture);

            this.Skill = skills.FirstOrDefault(i => i.Name == this.SkillId);
        }

        public string Id { get; set; }
        public int ImageId { get; set; }
        
        public string QuestId { get; set; }
        public int ForStage { get; set; }
        public int? NextStage { get; set; }
        
        [JsonProperty("Skill")] public string SkillId { get; set; }
        
        [JsonIgnore] public Skill Skill { get; set; }

        public static readonly List<ItemType> EquippableItems = new()
        {
            ItemType.Weapon,
            ItemType.Armor
        };

        [JsonProperty("Slots", ItemConverterType = typeof(StringEnumConverter))]
        public List<Slot> Slots { get; set; }

        [JsonIgnore] public Sprite Image { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType Type { get; set; } = ItemType.Unknown;

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Rarity Rarity { get; set; }

        public List<StatValue> Stats { get; set; } = new();

        public int Cost { get; set; }
        public int MinLevel { get; set; }

        [JsonProperty("Classes", ItemConverterType = typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }

        [JsonIgnore]
        public bool CanBeSoldInStore => this.Type != ItemType.Gold &&
                                        this.Type != ItemType.Key &&
                                        this.Type != ItemType.Unknown;

        [JsonIgnore]
        public string NameWithStats
        {
            get
            {
                var stats = this.StatString;
                if (Charges != 0)
                {
                    if (string.IsNullOrEmpty(stats))
                    {
                        stats = $"C{this.Charges}";
                    }
                    else
                    {
                        stats += $", C{this.Charges}";
                    }
                }
                
                return $"{this.Name} {stats}";
            }
        }
        
        public int GetAttribute(StatType statType)
        {
            return this.Stats.Where(i => i.Type == statType).Sum(stat => stat.Value);
        }
        
        [JsonIgnore]
        public string StatString
        {
            get
            {
                var stats = "";
                foreach (var stat in this.Stats.Where(i=> i.Value != 0).Select(o => o.Type).Distinct().OrderBy(i => i))
                {
                    var value = this.GetAttribute(stat);
                    var valueString = value > 0 ? $"+{value}" : $"{value}";
                    var shotStat = stat switch
                    {
                        StatType.Health => "H",
                        StatType.Magic => "M",
                        StatType.Agility => "Ag",
                        StatType.Attack => "At",
                        StatType.Defence => "D",
                        StatType.MagicDefence => "Md",
                        _ => ""
                    };
                    
                    if (string.IsNullOrEmpty(stats))
                    {
                        stats = $"{valueString}{shotStat}";
                    }
                    else
                    {
                        stats += $", {valueString}{shotStat}";
                    }
                }
                
                return stats;
            }
        }

        public int Charges { get; set; }
    }
}