using System.Collections.Generic;
using System.Linq;
using Nez;

namespace DungeonEscape.State
{
    public class Hero
    {
        public override string ToString()
        {
            return this.Name;
        }

        public string Name { get; set; } = "Test";
        public int XP { get; set; } = 0;
        public int Health { get; set; }
        public int MaxHealth { get; set; } = Random.NextInt(5) + 40;
        public int Magic { get; set; }
        public int MaxMagic { get; set; } = 5;
        public int Attack { get; set; } = Random.NextInt(5) + 5;
        public int Defence { get; set; } = Random.NextInt(5) + 1;
        public int Agility { get; set; } = Random.NextInt(5) + 1;
        public int NextLevel { get; set; } = 100;
        public int Level { get; set; } = 1;
        public List<Spell> Spells { get; private set; } = new List<Spell>();

        public Hero()
        {
            this.Health = this.MaxHealth;
            this.Magic = this.MaxMagic;
        }

        public bool CheckLevelUp(List<Spell> availableSpells, out string levelUpMessage)
        {
            if (this.XP < this.NextLevel)
            {
                levelUpMessage = null;
                return false;
            }

            var oldLevel = this.Level;
            ++this.Level;
            this.NextLevel = (this.NextLevel * 3) + Random.NextInt(this.NextLevel / 20);
            
            levelUpMessage = $"{Name} has advanced to level {Level}\n";
                    
            this.Attack += (Random.NextInt(7) + 1);
            this.MaxMagic += Random.NextInt(6) + 5;
            this.MaxHealth += Random.NextInt(7) + 1;
            this.Defence += Random.NextInt(4) + 1;
            this.Agility += Random.NextInt(3) + 1;

            foreach (var spell in availableSpells.Where(spell => spell.MinLevel <= this.Level && spell.MinLevel > oldLevel))
            {
                this.Spells.Add(spell);
                levelUpMessage += $"   Has learned the {spell.Name} Spell\n";
            }

            levelUpMessage += $"   Next Level is {NextLevel} XP\n";
            
            
            this.Magic = this.MaxMagic;
            this.Health = this.MaxHealth;
            return true;
        }
    }
}