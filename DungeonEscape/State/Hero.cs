// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Redpoint.DungeonEscape.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Random = Nez.Random;

    public class Hero : IFighter
    {
        public override string ToString()
        {
            return this.Name;
        }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Class Class { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Gender Gender { get; set; }

        public string Name { get; set; }
        public int Xp { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public bool RanAway => false;
        public int Magic { get; set; }
        public int MaxMagic { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int Agility { get; set; }
        public int NextLevel { get; set; }
        public int Level { get; set; }

        public IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells)
        {
            return availableSpells.Where(spell => spell.MinLevel <= this.Level && spell.Classes.Contains(this.Class));
        }

        [JsonIgnore]
        public bool IsDead => this.Health <= 0;
        
        public string WeaponId { get; set; }
        public string ArmorId { get; set; }
        public string ShieldId { get; set; }
        public string Id { get; set; }

        public Hero()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public void RollStats(IEnumerable<ClassStats> classLevels)
        {
            this.Level = 1;
            var classStats = classLevels.First(stats => stats.Class == this.Class);
            this.NextLevel = classStats.FirstLevel;
            
            // Roll starting stats
            
            this.MaxHealth = classStats.Stats.First( item=> item.Type == StatType.Health).RollStartValue();
            this.Attack = classStats.Stats.First( item=> item.Type == StatType.Attack).RollStartValue();
            this.Defence = classStats.Stats.First(item => item.Type == StatType.Defence).RollStartValue();
            this.MaxMagic = classStats.Stats.First( item=> item.Type == StatType.Magic).RollStartValue();
            this.Agility = classStats.Stats.First( item=> item.Type == StatType.Agility).RollStartValue();

            this.Health = this.MaxHealth;
            this.Magic = this.MaxMagic;
        }

        public bool CheckLevelUp(IEnumerable<ClassStats> classLevels, IEnumerable<Spell> availableSpells, out string levelUpMessage)
        {
            if (this.Xp < this.NextLevel)
            {
                levelUpMessage = null;
                return false;
            }

            var oldLevel = this.Level;
            ++this.Level;
            var classStats = classLevels.First(stats => stats.Class == this.Class);
            this.NextLevel = this.NextLevel * classStats.NextLevelFactor + Random.NextInt(this.NextLevel / classStats.NextLevelRandomPercent);
            
            levelUpMessage = $"{this.Name} has advanced to level {this.Level}\n";
            
           
            this.MaxHealth += classStats.Stats.First( item=> item.Type == StatType.Health).RollNextValue();
            this.Attack += classStats.Stats.First( item=> item.Type == StatType.Attack).RollNextValue();
            this.Defence += classStats.Stats.First(item => item.Type == StatType.Defence).RollNextValue();
            this.MaxMagic += classStats.Stats.First( item=> item.Type == StatType.Magic).RollNextValue();
            this.Agility = classStats.Stats.First( item=> item.Type == StatType.Agility).RollNextValue();

            levelUpMessage = availableSpells.Where(spell => spell.MinLevel <= this.Level && spell.MinLevel > oldLevel && spell.Classes.Contains(this.Class)).Aggregate(levelUpMessage, (current, spell) => current + $"   Has learned the {spell.Name} Spell\n");
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