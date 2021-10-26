using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DungeonEscape.State
{
    public class ItemInstance
    {
        private readonly Item item;

        public ItemInstance(Item item)
        {
            this.item = item;
        }

        public int MinLevel => this.item.MinLevel;
        
        public ItemType Type => this.item.Type;
        public string Name => this.item.Name;
        public Hero EquippedTo { get; set; }

        public bool IsEquipped = true;

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
    
    public class Party
    {
        public const int MaxItems = 30;
        
        public Party()
        {
            this.Members.Add(new Hero {Name = "Player 1"});
            this.Members.Add(new Hero {Name = "Player 2"});
            this.Members.Add(new Hero {Name = "Player 3"});
        }
        public Point OverWorldPos { get; set; } = Point.Zero;
        public bool HasShip { get; set; }
        public List<Hero> Members { get; } = new List<Hero>();
        public int Gold { get; set; }
        public List<ItemInstance> Items { get; } = new List<ItemInstance>();
        
        public bool CanOpenChest(int level)
        {
            return true;
        }
        
        public bool CanOpenDoor(int doorLevel)
        {
            var key = this.Items.FirstOrDefault(item => item.Type == ItemType.Key && item.MinLevel == doorLevel);
            if (key == null)
            {
                return false;
            }

            this.Items.Remove(key);
            return  true;
        }
    }
}