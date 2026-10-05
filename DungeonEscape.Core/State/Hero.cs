using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Rules;

namespace Redpoint.DungeonEscape.State
{
    public class Hero : Fighter
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Class Class { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Gender Gender { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Species Species { get; set; }

        public ulong NextLevel { get; set; }
        public bool IsActive { get; set; }
        public int Order { get; set; }
        public int? SpriteFrameIndex { get; set; }
        public string SpriteTilesetPath { get; set; }
        public int? SpriteTileId { get; set; }
        public Dictionary<Slot, string> Slots { get; set; }
        public List<string> Skills { get; set; }
        public List<int> SpellSlots { get; set; }
        public List<int> UsedSpellSlots { get; set; }
        public List<string> PreparedSpells { get; set; }

        public Hero()
        {
            IsActive = true;
            Slots = new Dictionary<Slot, string>();
            Skills = new List<string>();
            SpellSlots = new List<int>();
            UsedSpellSlots = new List<int>();
        }

        public override IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells)
        {
            return GetPreparedSpells(availableSpells);
        }

        public IEnumerable<Spell> GetKnownSpells(IEnumerable<Spell> availableSpells)
        {
            return availableSpells.Where(spell =>
                spell.MinLevel <= Level &&
                HasClass(spell.Classes, Class) &&
                CanEverCastSpellLevel(spell.SpellLevel));
        }

        public IEnumerable<Spell> GetPreparedSpells(IEnumerable<Spell> availableSpells)
        {
            var knownSpells = GetKnownSpellList(availableSpells);
            EnsurePreparedSpells(knownSpells);
            return knownSpells.Where(IsSpellPrepared);
        }

        public void RefreshPreparedSpells(IEnumerable<Spell> availableSpells)
        {
            EnsurePreparedSpells(GetKnownSpellList(availableSpells));
        }

        public int GetPreparedSpellLimit()
        {
            return DndSpellcastingRules.GetPreparedSpellLimit(this);
        }

        public bool IsSpellPrepared(Spell spell)
        {
            return spell != null &&
                   PreparedSpells != null &&
                   PreparedSpells.Any(id => IsSpellId(id, spell));
        }

        public bool CanPrepareSpell(Spell spell, IEnumerable<Spell> availableSpells)
        {
            if (spell == null)
            {
                return false;
            }

            var knownSpells = GetKnownSpellList(availableSpells);
            EnsurePreparedSpells(knownSpells);
            return knownSpells.Contains(spell) &&
                   !IsSpellPrepared(spell) &&
                   PreparedSpells.Count < GetPreparedSpellLimit();
        }

        public bool PrepareSpell(Spell spell, IEnumerable<Spell> availableSpells)
        {
            if (!CanPrepareSpell(spell, availableSpells))
            {
                return false;
            }

            PreparedSpells.Add(GetSpellId(spell));
            return true;
        }

        public bool UnprepareSpell(Spell spell)
        {
            if (spell == null || PreparedSpells == null)
            {
                return false;
            }

            var removed = PreparedSpells.RemoveAll(id => IsSpellId(id, spell));
            return removed > 0;
        }

        public void RefreshSpellSlots()
        {
            SpellSlots = DndSpellcastingRules.GetMaxSpellSlots(Class, Level).ToList();
            EnsureUsedSpellSlotList();
            for (var i = 0; i < UsedSpellSlots.Count; i++)
            {
                var max = i < SpellSlots.Count ? SpellSlots[i] : 0;
                if (UsedSpellSlots[i] > max)
                {
                    UsedSpellSlots[i] = max;
                }
            }
        }

        public void RestoreSpellSlots()
        {
            RefreshSpellSlots();
            for (var i = 0; i < UsedSpellSlots.Count; i++)
            {
                UsedSpellSlots[i] = 0;
            }
        }

        public bool HasAvailableSpellSlot(int spellLevel)
        {
            RefreshSpellSlots();
            spellLevel = Math.Max(1, Math.Min(DndSpellcastingRules.MaxSpellLevel, spellLevel));
            for (var i = spellLevel - 1; i < SpellSlots.Count; i++)
            {
                if (SpellSlots[i] - UsedSpellSlots[i] > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool UseSpellSlot(int spellLevel)
        {
            RefreshSpellSlots();
            spellLevel = Math.Max(1, Math.Min(DndSpellcastingRules.MaxSpellLevel, spellLevel));
            for (var i = spellLevel - 1; i < SpellSlots.Count; i++)
            {
                if (SpellSlots[i] - UsedSpellSlots[i] > 0)
                {
                    UsedSpellSlots[i]++;
                    return true;
                }
            }

            return false;
        }

        public string GetSpellSlotSummary()
        {
            RefreshSpellSlots();
            var parts = new List<string>();
            for (var i = 0; i < SpellSlots.Count; i++)
            {
                if (SpellSlots[i] > 0)
                {
                    parts.Add((i + 1) + ":" + Math.Max(0, SpellSlots[i] - UsedSpellSlots[i]) + "/" + SpellSlots[i]);
                }
            }

            return parts.Count == 0 ? "None" : string.Join("  ", parts.ToArray());
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
            var classStats = classStatList.First(stats => IsClass(stats.Class, Class));
            Xp = 0;
            NextLevel = DndLevelProgressionRules.GetNextLevelXp(Level);

            MaxHealth = classStats.Stats.First(item => item.Type == StatType.HP).RollStartValue();
            Attack = classStats.Stats.First(item => item.Type == StatType.Attack).RollStartValue();
            Defence = classStats.Stats.First(item => item.Type == StatType.Defence).RollStartValue();
            MagicDefence = classStats.Stats.First(item => item.Type == StatType.MagicDefence).RollStartValue();
            MaxMagic = classStats.Stats.First(item => item.Type == StatType.Magic).RollStartValue();
            Agility = classStats.Stats.First(item => item.Type == StatType.Agility).RollStartValue();
            Skills = classStats.Skills.ToList();
            DndCharacterRules.ApplyStartingAbilityScores(this);
            DndStatRules.RefreshHeroDerivedStats(this);
            RestoreSpellSlots();

            MaxHealth = DndStatRules.GetHeroHitPointsForLevel(this, classStats, Level);
            Health = MaxHealth;
            Magic = 0;
            MaxMagic = 0;
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
            NextLevel = DndLevelProgressionRules.GetNextLevelXp(Level);
            if (!DndLevelProgressionRules.CanLevelUp(Level, Xp))
            {
                levelUpMessage = null;
                return false;
            }

            var classStats = classLevels.First(stats => IsClass(stats.Class, Class));
            var oldLevel = Level;
            var oldProficiencyBonus = DndStatRules.GetProficiencyBonus(Level);
            var oldSpellSlotSummary = GetSpellSlotSummary();
            Level++;
            NextLevel = DndLevelProgressionRules.GetNextLevelXp(Level);
            var oldMaxHealth = MaxHealth;

            levelUpMessage = Name + " has advanced to level " + Level + "\n";

            MaxHealth = DndStatRules.GetHeroHitPointsForLevel(this, classStats, Level);
            DndStatRules.RefreshHeroDerivedStats(this);
            RefreshSpellSlots();

            var health = MaxHealth - oldMaxHealth;
            if (health != 0) levelUpMessage += "Health +" + health + "\n";
            if (DndStatRules.GetProficiencyBonus(Level) != oldProficiencyBonus)
            {
                levelUpMessage += "Proficiency Bonus is now +" + DndStatRules.GetProficiencyBonus(Level) + "\n";
            }

            var newSpellSlotSummary = GetSpellSlotSummary();
            if (newSpellSlotSummary != oldSpellSlotSummary)
            {
                levelUpMessage += "Spell slots: " + newSpellSlotSummary + "\n";
            }

            if (availableSpells != null)
            {
                foreach (var spell in availableSpells.Where(spell => spell.MinLevel <= Level && spell.MinLevel > oldLevel && HasClass(spell.Classes, Class) && CanEverCastSpellLevel(spell.SpellLevel)))
                {
                    levelUpMessage += "Has learned the " + spell.Name + " Spell\n";
                }
            }

            levelUpMessage += "Next Level is " + NextLevel + " XP\n";
            RestoreSpellSlots();
            EnsurePreparedSpells(GetKnownSpellList(availableSpells));
            Magic = 0;
            MaxMagic = 0;
            Health = MaxHealth;
            return true;
        }

        private void EnsureUsedSpellSlotList()
        {
            if (SpellSlots == null)
            {
                SpellSlots = new List<int>();
            }

            if (UsedSpellSlots == null)
            {
                UsedSpellSlots = new List<int>();
            }

            while (SpellSlots.Count < DndSpellcastingRules.MaxSpellLevel)
            {
                SpellSlots.Add(0);
            }

            while (UsedSpellSlots.Count < DndSpellcastingRules.MaxSpellLevel)
            {
                UsedSpellSlots.Add(0);
            }
        }

        private void EnsurePreparedSpells(IList<Spell> knownSpells)
        {
            knownSpells = knownSpells ?? new List<Spell>();
            if (PreparedSpells == null)
            {
                PreparedSpells = new List<string>();
                foreach (var spell in knownSpells.Take(GetPreparedSpellLimit()))
                {
                    PreparedSpells.Add(GetSpellId(spell));
                }
            }

            PreparedSpells.RemoveAll(id => !knownSpells.Any(spell => IsSpellId(id, spell)));
            var limit = GetPreparedSpellLimit();
            if (limit >= 0 && PreparedSpells.Count > limit)
            {
                PreparedSpells.RemoveRange(limit, PreparedSpells.Count - limit);
            }
        }

        public bool CanUseItem(ItemInstance item)
        {
            return !IsDead &&
                   item.Item.Skill != null &&
                   item.Item.Skill.IsNonEncounterSkill &&
                   (item.Classes == null || HasClass(item.Classes, Class));
        }

        public bool CanAttuneItem(ItemInstance item)
        {
            if (item == null || item.Item == null || IsDead || !item.RequiresAttunement || item.IsAttuned)
            {
                return false;
            }

            if (item.Item.AttunementClasses != null &&
                item.Item.AttunementClasses.Count > 0 &&
                !item.Item.AttunementClasses.Contains(Class))
            {
                return false;
            }

            return item.Classes == null || HasClass(item.Classes, Class);
        }

        public bool AttuneItem(ItemInstance item)
        {
            if (!CanAttuneItem(item))
            {
                return false;
            }

            item.IsAttuned = true;
            return true;
        }

        public bool CanEquipItem(ItemInstance item)
        {
            return !IsDead &&
                   item.IsEquippable &&
                   !item.IsEquipped &&
                   (!item.RequiresAttunement || item.IsAttuned) &&
                   (item.Classes == null || HasClass(item.Classes, Class));
        }

        private static bool HasClass(IEnumerable<string> classes, Class heroClass)
        {
            return classes != null && classes.Any(item => IsClass(item, heroClass));
        }

        private bool CanEverCastSpellLevel(int spellLevel)
        {
            var slots = DndSpellcastingRules.GetMaxSpellSlots(Class, Level);
            spellLevel = Math.Max(1, Math.Min(DndSpellcastingRules.MaxSpellLevel, spellLevel));
            return slots.Length >= spellLevel && slots[spellLevel - 1] > 0;
        }

        private List<Spell> GetKnownSpellList(IEnumerable<Spell> availableSpells)
        {
            return availableSpells == null
                ? new List<Spell>()
                : availableSpells.Where(spell =>
                    spell != null &&
                    spell.MinLevel <= Level &&
                    HasClass(spell.Classes, Class) &&
                    CanEverCastSpellLevel(spell.SpellLevel)).ToList();
        }

        private static string GetSpellId(Spell spell)
        {
            return spell == null ? null : string.IsNullOrWhiteSpace(spell.DndSpell) ? spell.Name : spell.DndSpell;
        }

        private static bool IsSpellId(string id, Spell spell)
        {
            if (string.IsNullOrWhiteSpace(id) || spell == null)
            {
                return false;
            }

            return string.Equals(id, spell.Name, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, spell.DndSpell, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsClass(string className, Class heroClass)
        {
            return DndCharacterRules.IsClassNameMatch(className, heroClass);
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
            if (item.Type == ItemType.Weapon)
            {
                DamageDice = 0;
                DamageDie = 0;
                DamageBonus -= item.DamageBonus;
            }

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
            if (item.Type == ItemType.Weapon)
            {
                DamageDice = item.DamageDice;
                DamageDie = item.DamageDie;
                DamageBonus += item.DamageBonus;
            }
        }
    }
}
