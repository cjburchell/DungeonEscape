using System.Collections.Generic;
using System.Linq;

namespace DungeonEscape.State
{
    using System;
    using Newtonsoft.Json;
    using Random = Nez.Random;

    public class Hero : Fighter
    {
        public override string ToString()
        {
            return this.Name;
        }

        public string Name { get; set; }
        public int XP { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Magic { get; set; }
        public int MaxMagic { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int Agility { get; set; }
        public int NextLevel { get; set; }
        public int Level { get; set; }
        public List<int> Spells { get; private set; } = new List<int>();
        
        [JsonIgnore]
        public bool IsDead => this.Health <= 0;
        
        public string WeaponId { get; set; }
        public string ArmorId { get; set; }
        public string ShieldId { get; set; }
        public string Id { get; set; }

        public Hero()
        {
            this.Id = Guid.NewGuid().ToString();
            this.RollStats();
            this.Health = this.MaxHealth;
            this.Magic = this.MaxMagic;
        }

        private void RollStats()
        {
            Level = 1;
            NextLevel = 100;
            MaxHealth = Random.NextInt(5) + 40;
            Attack = Random.NextInt(5) + 5;
            Defence = Random.NextInt(5) + 1;
            MaxMagic = 5;
            Agility = Random.NextInt(5) + 1;
        }

        public bool CheckLevelUp(IEnumerable<Spell> availableSpells, out string levelUpMessage)
        {
            if (this.XP < this.NextLevel)
            {
                levelUpMessage = null;
                return false;
            }

            var oldLevel = this.Level;
            ++this.Level;
            this.NextLevel = (this.NextLevel * 3) + Random.NextInt(this.NextLevel / 20);
            
            levelUpMessage = $"{this.Name} has advanced to level {this.Level}\n";
                    
            this.Attack += Random.NextInt(7) + 1;
            this.MaxMagic += Random.NextInt(6) + 5;
            this.MaxHealth += Random.NextInt(7) + 1;
            this.Defence += Random.NextInt(4) + 1;
            this.Agility += Random.NextInt(3) + 1;

            foreach (var spell in availableSpells.Where(spell => spell.MinLevel <= this.Level && spell.MinLevel > oldLevel))
            {
                this.Spells.Add(spell.Id);
                levelUpMessage += $"   Has learned the {spell.Name} Spell\n";
            }

            levelUpMessage += $"   Next Level is {this.NextLevel} XP\n";
            
            
            this.Magic = this.MaxMagic;
            this.Health = this.MaxHealth;
            return true;
        }

        public bool CanUseItem(ItemInstance item)
        {
            return !this.IsDead && !item.IsEquipped && (item.Type == ItemType.OneUse ||
                                                        item.Type == ItemType.Weapon ||
                                                        item.Type == ItemType.Armor ||
                                                        item.Type == ItemType.Shield);
        }
    }
}