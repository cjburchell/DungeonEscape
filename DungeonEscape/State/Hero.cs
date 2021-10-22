using Nez;

namespace DungeonEscape.State
{
    public class Hero
    {
        public string Name { get; set; } = "Test";
        public int XP { get; set; } = 0;
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Magic { get; set; }
        public int MaxMagic { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int Agility { get; set; }
        public int NextLevel { get; set; }
        public int Level { get; set; }

        public Hero()
        {
            this.Level = 1;
            this.Defence = Random.NextInt(5) + 1;
            this.Attack = Random.NextInt(5) + 5;
            this.Agility = Random.NextInt(5) + 1;
            this.MaxHealth = Random.NextInt(5) + 40;
            this.Health = this.MaxHealth;
            this.MaxMagic = 5;
            this.Magic = this.MaxMagic;
            this.NextLevel = 50;
        }

        public bool CheckLevelUp()
        {
            if (this.XP < this.NextLevel)
            {
                return false;
            }

            ++this.Level;
            this.Attack += (Random.NextInt(7) + 1);
            this.MaxMagic += Random.NextInt(6) + 5;
            this.Magic = this.MaxMagic;
            this.MaxHealth += Random.NextInt(7) + 1;
            this.Health = this.MaxHealth;
            this.Defence += Random.NextInt(4) + 1;
            this.Agility += Random.NextInt(3) + 1;
            this.NextLevel = (this.NextLevel * 2) + Random.NextInt(this.NextLevel / 10);
            return true;
        }
    }
}