using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DungeonEscape.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ItemInstance
    {
        private Item item;

        public ItemInstance(Item item)
        {
            this.Id = Guid.NewGuid().ToString();
            this.item = item;
            this.ItemId = item.Id;
        }

        // ReSharper disable once UnusedMember.Global
        public ItemInstance()
        {
        }

        public void UpdateItem(IEnumerable<Item> items)
        {
            this.item = items.FirstOrDefault(i => i.Id == this.ItemId);
        }
        
        public string Id { get; set; }
        public int ItemId { get; set; }
        public string EquippedTo { get; set; }
        public bool IsEquipped { get; set; }

        [JsonIgnore]
        public Texture2D Image => this.item.Image;

        [JsonIgnore]
        public int MinLevel => this.item.MinLevel;
        
        [JsonIgnore]
        public ItemType Type => this.item.Type;
        
        [JsonIgnore]
        public string Name => this.item.Name;

        [JsonIgnore]
        public int Gold => this.item.Gold;
        
        [JsonIgnore]
        public bool IsEquippable =>
            this.Type == ItemType.Armor || this.Type == ItemType.Shield || this.Type == ItemType.Weapon;

        public void Unequip(IEnumerable<Hero> heroes)
        {
            if (this.IsEquipped && !string.IsNullOrEmpty(this.EquippedTo))
            {
                var equippedHero = heroes.FirstOrDefault(hero => hero.Id == this.EquippedTo);
                if (equippedHero != null)
                {
                    switch (this.Type)
                    {
                        case ItemType.Weapon:
                            equippedHero.WeaponId = null;
                            break;
                        case ItemType.Armor:
                            equippedHero.ArmorId = null;
                            break;
                        case ItemType.Shield:
                            equippedHero.ShieldId = null;
                            break;
                    }

                    equippedHero.Agility -= this.item.Agility;
                    equippedHero.Attack -= this.item.Attack;
                    equippedHero.Defence -= this.item.Defence;
                    equippedHero.MaxHealth -= this.item.Health;

                    if (equippedHero.Health > equippedHero.MaxHealth)
                    {
                        equippedHero.Health = equippedHero.MaxHealth;
                    }
                }
            }
            
            this.EquippedTo = null;
            this.IsEquipped = false;
        }

        public void Equip(Hero hero, IEnumerable<ItemInstance> items, IEnumerable<Hero> heroes)
        {
            ItemInstance oldItem;
            switch (this.Type)
            {
                case ItemType.Weapon:
                    oldItem = items.FirstOrDefault(i => i.Id == hero.WeaponId);
                    oldItem?.Unequip(heroes);
                    hero.WeaponId = this.Id;
                    break;
                case ItemType.Armor:
                    oldItem = items.FirstOrDefault(i => i.Id == hero.ArmorId);
                    oldItem?.Unequip(heroes);
                    hero.ArmorId = this.Id;
                    break;
                case ItemType.Shield:
                    oldItem = items.FirstOrDefault(i => i.Id == hero.ShieldId);
                    oldItem?.Unequip(heroes);
                    hero.ShieldId = this.Id;
                    break;
            }
            
            this.IsEquipped = true;
            this.EquippedTo = hero.Id;
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