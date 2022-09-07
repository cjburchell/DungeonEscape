// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Redpoint.DungeonEscape.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public class ItemInstance
    {
        public ItemInstance(Item item)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Item = item;
            this.Charges = item.Charges;
        }

        // ReSharper disable once UnusedMember.Global
        public ItemInstance()
        {
        }

        public Item Item { get; set; }
        public string Id { get; set; }
        public string EquippedTo { get; set; }
        public bool IsEquipped { get; set; }
        
        public int Charges { get; set; }

        [JsonIgnore]
        public Nez.Textures.Sprite Image => this.Item.Image;
        
        [JsonIgnore]
        public Rarity Rarity => this.Item.Rarity;

        [JsonIgnore]
        public int MinLevel => this.Item.MinLevel;
        
        [JsonIgnore]
        public ItemType Type => this.Item.Type;
        
        [JsonIgnore]
        public List<Slot> Slots => this.Item.Slots;
        
        [JsonIgnore]
        public string Name => this.Item.Name;

        [JsonIgnore] public string NameWithStats
        {
            get
            {
                var stats = this.Item.StatString;
                if (this.MaxCharges != 0)
                {
                    if (string.IsNullOrEmpty(stats))
                    {
                        stats = $"C{this.Charges}/{this.MaxCharges}";
                    }
                    else
                    {
                        stats += $", C{this.Charges}/{this.MaxCharges}";
                    }
                }
                
                return $"{this.Name} {stats}";
            }
        }

        [JsonIgnore]
        public int Gold => this.Item.Cost;
        
        [JsonIgnore]
        public int Agility => this.Item.GetAttribute(StatType.Agility);

        [JsonIgnore]
        public int Attack => this.Item.GetAttribute(StatType.Attack);
        
        [JsonIgnore]
        public int Defence => this.Item.GetAttribute(StatType.Defence);
        
        [JsonIgnore]
        public int MagicDefence => this.Item.GetAttribute(StatType.MagicDefence);

        [JsonIgnore]
        public int Health => this.Item.GetAttribute(StatType.Health);
        
        [JsonIgnore]
        public int Magic => this.Item.GetAttribute(StatType.Magic);

        [JsonIgnore] public IReadOnlyCollection<Class> Classes => this.Item.Classes;
        
        [JsonIgnore]
        public bool IsEquippable => Item.EquippableItems.Contains(this.Type);

        [JsonIgnore]
        public int MaxCharges => Item.Charges;

        [JsonIgnore]
        public bool HasCharges => this.MaxCharges == 0 || this.Charges != 0; 

        public void UnEquip(IEnumerable<Hero> heroes)
        {
            if (!this.IsEquipped || string.IsNullOrEmpty(this.EquippedTo))
            {
                return;
            }

            var equippedHero = heroes.FirstOrDefault(hero => hero.Id == this.EquippedTo);
            equippedHero?.UnEquip(this);
        }
        
        public (string, bool) Use(IFighter source, IFighter target, IGame game, int round)
        {
            var message = source != target
                ? $"{source.Name} used {this.Name} on {target.Name}"
                : $"{source.Name} used {this.Name}";
            
            if (this.MaxCharges != 0)
            {
                if (this.Charges <= 0)
                {
                    return (message+"\nbut the item has no charges", false);
                }
            }

            if (this.Item.Skill != null)
            {
                var (result, worked)  = this.Item.Skill.Do(source, target, game, round);
                if (worked && this.MaxCharges != 0)
                {
                    this.Charges--;
                }
                
                return ($"{message}\n{result}", worked);
            }
            
            foreach (var stat in this.Item.Stats.Where(i => i.Value != 0).Select(o => o.Type).Distinct()
                         .OrderBy(i => i))
            {
                var value = this.Item.GetAttribute(stat);
                if (value == 0) continue;
                
                switch (stat)
                {
                    case StatType.Health:
                        if (target.Health+value > target.MaxHealth)
                        {
                            target.Health = target.MaxHealth;
                            value = target.MaxHealth - this.Health;
                        }
                        else
                        {
                            target.Health += value;
                        }
                        break;
                    case StatType.Magic:
                        if (target.Magic+value > target.MaxMagic)
                        {
                            target.Magic = target.MaxMagic;
                            value = target.MaxMagic - target.Magic;
                        }
                        else
                        {
                            target.Magic += value;
                        }

                        break;
                    case StatType.Agility:
                        target.Agility += value;
                        break;
                    case StatType.Attack:
                        target.Attack += value;
                        break;
                    case StatType.Defence:
                        target.Defence += value;
                        break;
                    case StatType.MagicDefence:
                        target.MagicDefence += value;
                        break;
                }

                if (value == 0) continue;
                var direction = value > 0 ? "Increased" : "Decreased";
                message += $"\n{target.Name} {stat} {direction} by {value}";
            }

            if (this.MaxCharges != 0)
            {
                this.Charges--;
            }
            return (message, true);
        }
    }
}