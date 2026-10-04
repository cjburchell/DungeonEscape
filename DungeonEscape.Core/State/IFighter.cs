using Redpoint.DungeonEscape.Data;
using System.Collections.Generic;

namespace Redpoint.DungeonEscape.State
{
    public interface IFighter
    {
        string Name { get; }
        int Health { get; set; }
        int Magic { get; set; }
        int Attack { get; set; }
        int Defence { get; set; }
        int Agility { get; set; }
        bool IsDead { get; }
        int Level { get; }
        int MaxHealth { get; set; }
        IEnumerable<StatValue> Stats { get; }
        bool RanAway { get; }
        IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells);
        IEnumerable<Skill> GetSkills(IEnumerable<Skill> availableSkills);
        List<StatusEffect> Status { get; }
        List<ItemInstance> Items { get; }
        int MagicDefence { get; set; }
        int MaxMagic { get; set; }
        int Strength { get; set; }
        int Dexterity { get; set; }
        int Constitution { get; set; }
        int Intelligence { get; set; }
        int Wisdom { get; set; }
        int Charisma { get; set; }
        int ArmorClass { get; set; }
        int ProficiencyBonus { get; set; }
        int AttackBonus { get; set; }
        int DamageDice { get; set; }
        int DamageDie { get; set; }
        int DamageBonus { get; set; }
        void AddEffect(StatusEffect effect);
        void RemoveEffect(StatusEffect effect);
        void Equip(ItemInstance item);
        List<string> GetEquipmentId(IEnumerable<Slot> slots);
        string UpdateStatusEffects(IGame game);
        string CheckForExpiredStates(int round, DurationType durationType);
        void PlayDamageAnimation();
        bool CanHit(IFighter target);
        bool CanCriticalHit(IFighter target);
        int CalculateDamage(int attack, bool isPiercing = false, bool isMagic = false);
        string HitCheck();
    }
}
