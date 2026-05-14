using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Redpoint.DungeonEscape.State
{
    public class ItemInstance
    {
        public ItemInstance(Item item)
        {
            Id = Guid.NewGuid().ToString();
            Item = item;
            Charges = item.Charges;
        }

        public ItemInstance()
        {
        }

        public Item Item { get; set; }
        public string Id { get; set; }
        public string EquippedTo { get; set; }
        public bool IsEquipped { get; set; }
        public int Charges { get; set; }

        [JsonIgnore]
        public Rarity Rarity { get { return Item.Rarity; } }

        [JsonIgnore]
        public int MinLevel { get { return Item.MinLevel; } }

        [JsonIgnore]
        public ItemType Type { get { return Item.Type; } }

        [JsonIgnore]
        public List<Slot> Slots { get { return Item.Slots; } }

        [JsonIgnore]
        public string Name { get { return Item.Name; } }

        [JsonIgnore]
        public string NameWithStats
        {
            get
            {
                var stats = Item.StatString;
                if (MaxCharges != 0)
                {
                    stats = string.IsNullOrEmpty(stats)
                        ? "C" + Charges + "/" + MaxCharges
                        : stats + ", C" + Charges + "/" + MaxCharges;
                }

                return Name + " " + stats;
            }
        }

        [JsonIgnore]
        public int Gold { get { return Item.Cost; } }

        [JsonIgnore]
        public int Agility { get { return Item.GetAttribute(StatType.Agility); } }

        [JsonIgnore]
        public int Attack { get { return Item.GetAttribute(StatType.Attack); } }

        [JsonIgnore]
        public int Defence { get { return Item.GetAttribute(StatType.Defence); } }

        [JsonIgnore]
        public int MagicDefence { get { return Item.GetAttribute(StatType.MagicDefence); } }

        [JsonIgnore]
        public int Health { get { return Item.GetAttribute(StatType.Health); } }

        [JsonIgnore]
        public int Magic { get { return Item.GetAttribute(StatType.Magic); } }

        [JsonIgnore]
        public IReadOnlyCollection<Class> Classes { get { return Item.Classes; } }

        [JsonIgnore]
        public bool IsEquippable { get { return Item.EquippableItems.Contains(Type); } }

        [JsonIgnore]
        public int MaxCharges { get { return Item.Charges; } }

        [JsonIgnore]
        public bool HasCharges { get { return MaxCharges == 0 || Charges != 0; } }

        [JsonIgnore]
        public Target Target { get { return Item.Target; } }

        public void UnEquip(IEnumerable<Hero> heroes)
        {
            if (!IsEquipped || string.IsNullOrEmpty(EquippedTo))
            {
                return;
            }

            var equippedHero = heroes.FirstOrDefault(hero => hero.Name == EquippedTo);
            if (equippedHero != null)
            {
                equippedHero.UnEquip(this);
            }
        }

        public (string, bool) Use(IFighter source, IFighter target, BaseState targetObject, IGame game, int round, bool ignoreCharges = false)
        {
            var message = source != target
                ? source.Name + " used " + Name + " on " + target.Name
                : source.Name + " used " + Name;

            if (MaxCharges != 0 && !ignoreCharges && Charges <= 0)
            {
                return (message + "\nbut the item has no charges", false);
            }

            if (Item.Skill != null)
            {
                var result = Item.Skill.Do(target, source, targetObject, game, round);
                if (result.Item2 && MaxCharges != 0)
                {
                    Charges--;
                }

                return (message + "\n" + result.Item1, result.Item2);
            }

            foreach (var stat in Item.Stats.Where(i => i.Value != 0).Select(o => o.Type).Distinct().OrderBy(i => i))
            {
                var value = Item.GetAttribute(stat);
                if (value == 0)
                {
                    continue;
                }

                switch (stat)
                {
                    case StatType.Health:
                        if (target.Health + value > target.MaxHealth)
                        {
                            target.Health = target.MaxHealth;
                            value = target.MaxHealth - Health;
                        }
                        else
                        {
                            target.Health += value;
                        }
                        break;
                    case StatType.Magic:
                        if (target.Magic + value > target.MaxMagic)
                        {
                            target.Magic = target.MaxMagic;
                            value = target.MaxMagic - target.Magic;
                        }
                        else
                        {
                            target.Magic += value;
                        }
                        break;
                    case StatType.Agility:
                        target.Agility += value;
                        break;
                    case StatType.Attack:
                        target.Attack += value;
                        break;
                    case StatType.Defence:
                        target.Defence += value;
                        break;
                    case StatType.MagicDefence:
                        target.MagicDefence += value;
                        break;
                }

                if (value == 0)
                {
                    continue;
                }

                var direction = value > 0 ? "Increased" : "Decreased";
                message += "\n" + target.Name + " " + stat + " " + direction + " by " + value;
            }

            if (MaxCharges != 0 && Charges != 0)
            {
                Charges--;
            }

            return (message, true);
        }
    }
}
