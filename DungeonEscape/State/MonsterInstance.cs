// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace DungeonEscape.State
{
    using Nez;
    using Nez.UI;

    public class MonsterInstance : Fighter
    {
        public override string ToString()
        {
            return this.Name;
        }
        
        public Monster Info { get; }

        public MonsterInstance(Monster info)
        {
            this.Info = info;
            this.Health = 0;
            for (var i = 0; i < info.Health; i++)
            {
                this.Health+=Random.NextInt(8)+1;
            }

            this.Health += info.HealthConst;
            this.MaxHealth = this.Health;
            
            for (var i = 0; i < info.Magic; i++)
            {
                this.Magic+=Random.NextInt(8)+1;
            }

            this.MaxMagic = this.Magic;
        }
        
        public int MaxMagic { get; set; }

        public Image Image { get; set; }
        
        public string Name => this.Info.Name;
        public int Health { get; set; }
        public int Magic { get; set; }
        public int Attack => this.Info.Attack;
        public int Defence => this.Info.Defence;
        public int Agility => this.Info.Agility;      
        public bool IsDead => this.Health <= 0;
        public int Level => this.Info.MinLevel;
        public int MaxHealth { get; }
        public bool RanAway { get; set; }
    }
}