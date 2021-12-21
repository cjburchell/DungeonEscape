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
        private Item _item;

        public ItemInstance(Item item)
        {
            this.Id = Guid.NewGuid().ToString();
            this._item = item;
            this.ItemId = item.Id;
        }

        // ReSharper disable once UnusedMember.Global
        public ItemInstance()
        {
        }

        public void UpdateItem(IEnumerable<Item> items)
        {
            this._item = items.FirstOrDefault(i => i.Id == this.ItemId);
        }
        
        public string Id { get; set; }
        public int ItemId { get; set; }
        public string EquippedTo { get; set; }
        public bool IsEquipped { get; set; }

        [JsonIgnore]
        public Nez.Textures.Sprite Image => this._item.Image;

        [JsonIgnore]
        public int MinLevel => this._item.MinLevel;
        
        [JsonIgnore]
        public ItemType Type => this._item.Type;
        
        [JsonIgnore]
        public string Name => this._item.Name;

        [JsonIgnore]
        public int Gold => this._item.Cost;
        
        [JsonIgnore]
        public bool IsEquippable =>
            this.Type == ItemType.Armor || this.Type == ItemType.Shield || this.Type == ItemType.Weapon;

        public void UnEquip(IEnumerable<Hero> heroes)
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
                        case ItemType.OneUse:
                            break;
                        case ItemType.Key:
                            break;
                        case ItemType.Gold:
                            break;
                        case ItemType.Unknown:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    equippedHero.Agility -= this._item.Agility;
                    equippedHero.Attack -= this._item.Attack;
                    equippedHero.Defence -= this._item.Defence;
                    equippedHero.MaxHealth -= this._item.Health;

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
                    oldItem?.UnEquip(heroes);
                    hero.WeaponId = this.Id;
                    break;
                case ItemType.Armor:
                    oldItem = items.FirstOrDefault(i => i.Id == hero.ArmorId);
                    oldItem?.UnEquip(heroes);
                    hero.ArmorId = this.Id;
                    break;
                case ItemType.Shield:
                    oldItem = items.FirstOrDefault(i => i.Id == hero.ShieldId);
                    oldItem?.UnEquip(heroes);
                    hero.ShieldId = this.Id;
                    break;
                case ItemType.OneUse:
                    break;
                case ItemType.Key:
                    break;
                case ItemType.Gold:
                    break;
                case ItemType.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            this.IsEquipped = true;
            this.EquippedTo = hero.Id;
            hero.Agility += this._item.Agility;
            hero.Attack += this._item.Attack;
            hero.Defence += this._item.Defence;
            hero.MaxHealth += this._item.Health;
        }

        public void Use(Hero hero)
        {
            hero.Agility += this._item.Agility;
            hero.Attack += this._item.Attack;
            hero.Defence += this._item.Defence;
            hero.Health += this._item.Health;
            if (hero.Health > hero.MaxHealth)
            {
                hero.Health = hero.MaxHealth;
            }
        }
    }
}