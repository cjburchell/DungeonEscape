namespace DungeonEscape.State
{
    using Microsoft.Xna.Framework.Graphics;

    public class ItemInstance
    {
        private readonly Item item;

        public ItemInstance(Item item)
        {
            this.item = item;
        }

        public Texture2D Image => this.item.Image;

        public int MinLevel => this.item.MinLevel;
        
        public ItemType Type => this.item.Type;
        public string Name => this.item.Name;
        public Hero EquippedTo { get; set; }
        public int Gold => this.item.Gold;

        public bool IsEquipped;

        public bool IsEquippable =>
            this.Type == ItemType.Armor || this.Type == ItemType.Shield || this.Type == ItemType.Weapon;

        public void Unequip()
        {
            switch (this.Type)
            {
                case ItemType.Weapon:
                    this.EquippedTo.Weapon = null;
                    break;
                case ItemType.Armor:
                    this.EquippedTo.Armor = null;
                    break;
                case ItemType.Shield:
                    this.EquippedTo.Shield = null;
                    break;
            }

            this.EquippedTo.Agility -= this.item.Agility;
            this.EquippedTo.Attack -= this.item.Attack;
            this.EquippedTo.Defence -= this.item.Defence;
            this.EquippedTo.MaxHealth -= this.item.Health;

            if (this.EquippedTo.Health > this.EquippedTo.MaxHealth)
            {
                this.EquippedTo.Health = this.EquippedTo.MaxHealth;
            }

            this.EquippedTo = null;
            this.IsEquipped = false;
        }

        public void Equip(Hero hero)
        {
            switch (this.Type)
            {
                case ItemType.Weapon:
                    hero.Weapon?.Unequip();
                    hero.Weapon = this;
                    break;
                case ItemType.Armor:
                    hero.Armor?.Unequip();;
                    hero.Armor = this;
                    break;
                case ItemType.Shield:
                    hero.Shield?.Unequip();;
                    hero.Shield = this;
                    break;
            }
            
            this.IsEquipped = true;
            this.EquippedTo = hero;
            hero.Agility += this.item.Agility;
            hero.Attack += this.item.Attack;
            hero.Defence += this.item.Defence;
            hero.MaxHealth += this.item.Health;
        }

        public void Use(Hero hero)
        {
            hero.Agility += this.item.Agility;
            hero.Attack += this.item.Attack;
            hero.Defence += this.item.Defence;
            hero.Health += this.item.Health;
            if (hero.Health > hero.MaxHealth)
            {
                hero.Health = hero.MaxHealth;
            }
        }
    }
}