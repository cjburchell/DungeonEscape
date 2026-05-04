using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Redpoint.DungeonEscape.State
{
    public class Hero : Fighter
    {
        private static readonly Random Random = new Random();

        [JsonConverter(typeof(StringEnumConverter))]
        public Class Class { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Gender Gender { get; set; }

        public ulong NextLevel { get; set; }
        public bool IsActive { get; set; }
        public int Order { get; set; }
        public Dictionary<Slot, string> Slots { get; set; }
        public List<string> Skills { get; set; }

        public Hero()
        {
            IsActive = true;
            Slots = new Dictionary<Slot, string>();
            Skills = new List<string>();
        }

        public override IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells)
        {
            return availableSpells.Where(spell => spell.MinLevel <= Level && spell.Classes.Contains(Class));
        }

        public override IEnumerable<Skill> GetSkills(IEnumerable<Skill> availableSkills)
        {
            return Skills.Select(id => availableSkills.FirstOrDefault(item => item.Name == id))
                .Where(skill => skill != null).ToList();
        }

        public void Setup(IGame game, int level = 1, bool generateItems = true)
        {
            Level = 1;
            var classStatList = game.ClassLevelStats.ToList();
            var classStats = classStatList.First(stats => stats.Class == Class);
            Xp = 0;
            NextLevel = classStats.FirstLevel;

            MaxHealth = classStats.Stats.First(item => item.Type == StatType.Health).RollStartValue();
            Attack = classStats.Stats.First(item => item.Type == StatType.Attack).RollStartValue();
            Defence = classStats.Stats.First(item => item.Type == StatType.Defence).RollStartValue();
            MagicDefence = classStats.Stats.First(item => item.Type == StatType.MagicDefence).RollStartValue();
            MaxMagic = classStats.Stats.First(item => item.Type == StatType.Magic).RollStartValue();
            Agility = classStats.Stats.First(item => item.Type == StatType.Agility).RollStartValue();
            Skills = classStats.Skills.ToList();

            Health = MaxHealth;
            Magic = MaxMagic;
            while (Level < level)
            {
                Xp = NextLevel;
                CheckLevelUp(classStatList, null, out _);
            }

            if (!generateItems)
            {
                return;
            }

            Items = new List<ItemInstance>();

            var armor = game.CreateRandomEquipment(Level, Math.Max(Level - 5, 1), Rarity.Common, ItemType.Armor, Class, Slot.Chest);
            if (armor != null)
            {
                var item = new ItemInstance(armor);
                Items.Add(item);
                Equip(item);
            }

            var weapon = game.CreateRandomEquipment(Level, Math.Max(Level - 5, 1), Rarity.Common, ItemType.Weapon, Class);
            if (weapon != null)
            {
                var item = new ItemInstance(weapon);
                Items.Add(item);
                Equip(item);
            }
        }

        public bool CheckLevelUp(IEnumerable<ClassStats> classLevels, IEnumerable<Spell> availableSpells, out string levelUpMessage)
        {
            if (Xp < NextLevel)
            {
                levelUpMessage = null;
                return false;
            }

            var classStats = classLevels.First(stats => stats.Class == Class);
            var oldLevel = Level;
            Level++;
            NextLevel = CalculateNextLevel(oldLevel, NextLevel);

            levelUpMessage = Name + " has advanced to level " + Level + "\n";

            var health = classStats.Stats.First(item => item.Type == StatType.Health).RollNextValue();
            var attack = classStats.Stats.First(item => item.Type == StatType.Attack).RollNextValue();
            var defence = classStats.Stats.First(item => item.Type == StatType.Defence).RollNextValue();
            var magicDefence = classStats.Stats.First(item => item.Type == StatType.MagicDefence).RollNextValue();
            var magic = classStats.Stats.First(item => item.Type == StatType.Magic).RollNextValue();
            var agility = classStats.Stats.First(item => item.Type == StatType.Agility).RollNextValue();

            if (health != 0) levelUpMessage += "Health +" + health + "\n";
            if (attack != 0) levelUpMessage += "Attack +" + attack + "\n";
            if (defence != 0) levelUpMessage += "Defence +" + defence + "\n";
            if (magicDefence != 0) levelUpMessage += "Defence +" + magicDefence + "\n";
            if (magic != 0) levelUpMessage += "Magic +" + magic + "\n";
            if (agility != 0) levelUpMessage += "Agility +" + agility + "\n";

            MaxHealth += health;
            Attack += attack;
            Defence += defence;
            MagicDefence += magicDefence;
            MaxMagic += magic;
            Agility += agility;

            if (availableSpells != null)
            {
                foreach (var spell in availableSpells.Where(spell => spell.MinLevel <= Level && spell.MinLevel > oldLevel && spell.Classes.Contains(Class)))
                {
                    levelUpMessage += "Has learned the " + spell.Name + " Spell\n";
                }
            }

            levelUpMessage += "Next Level is " + NextLevel + " XP\n";
            Magic = MaxMagic;
            Health = MaxHealth;
            return true;
        }

        private static ulong CalculateNextLevel(int oldLevel, ulong currentLevel)
        {
            var factors = new Dictionary<int, double>
            {
                {1, 3.0},
                {2, 2.0},
                {3, 1.75},
                {4, 1.65},
                {5, 1.5},
                {10, 1.35},
                {15, 1.2},
                {20, 1.1},
                {45, 1}
            };

            var factor = 1.0;
            foreach (var pair in factors)
            {
                if (oldLevel < pair.Key)
                {
                    break;
                }

                factor = pair.Value;
            }

            const double randomFactor = 0.05;
            var randomMax = (int)(Math.Min(currentLevel, int.MaxValue) * randomFactor);
            var randomValue = randomMax > 0 ? Random.Next(randomMax) : 0;
            return (ulong)(currentLevel * factor) + (ulong)randomValue;
        }

        public bool CanUseItem(ItemInstance item)
        {
            return !IsDead &&
                   item.Item.Skill != null &&
                   item.Item.Skill.IsNonEncounterSkill &&
                   (item.Classes == null || item.Classes.Contains(Class));
        }

        public bool CanEquipItem(ItemInstance item)
        {
            return !IsDead && item.IsEquippable && !item.IsEquipped && (item.Classes == null || item.Classes.Contains(Class));
        }

        public void UnEquip(ItemInstance item)
        {
            if (!item.IsEquipped)
            {
                return;
            }

            if (!item.Slots.Any(slot => Slots.ContainsKey(slot) && Slots[slot] == item.Id))
            {
                return;
            }

            item.EquippedTo = null;
            item.IsEquipped = false;
            foreach (var slot in item.Slots)
            {
                Slots[slot] = null;
            }

            Agility -= item.Agility;
            Attack -= item.Attack;
            Defence -= item.Defence;
            MagicDefence -= item.MagicDefence;
            MaxHealth -= item.Health;

            if (Health > MaxHealth)
            {
                Health = MaxHealth;
            }
        }

        public override List<string> GetEquipmentId(IEnumerable<Slot> slots)
        {
            return (from slot in slots where Slots.ContainsKey(slot) select Slots[slot]).ToList();
        }

        public override void Equip(ItemInstance item)
        {
            foreach (var slot in item.Slots)
            {
                Slots[slot] = item.Id;
            }

            item.IsEquipped = true;
            item.EquippedTo = Name;
            Agility += item.Agility;
            Attack += item.Attack;
            Defence += item.Defence;
            MagicDefence += item.MagicDefence;
            MaxHealth += item.Health;
        }
    }
}
