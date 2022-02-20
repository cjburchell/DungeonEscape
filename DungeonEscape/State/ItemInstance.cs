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
        public int Agility => this._item.Agility;

        [JsonIgnore]
        public int Attack => this._item.Attack;
        
        [JsonIgnore]
        public int Defence => this._item.Defence;

        [JsonIgnore]
        public int Health => this._item.Health;
        
        [JsonIgnore]
        public bool IsEquippable =>
            this.Type == ItemType.Armor || this.Type == ItemType.Shield || this.Type == ItemType.Weapon;
        
        public void UnEquip(IEnumerable<Hero> heroes)
        {
            if (!this.IsEquipped || string.IsNullOrEmpty(this.EquippedTo))
            {
                return;
            }

            var equippedHero = heroes.FirstOrDefault(hero => hero.Id == this.EquippedTo);
            equippedHero?.UnEquip(this);
        }
    }
}