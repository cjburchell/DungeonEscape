namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;

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
        public IEnumerable<StatValue> Stats { get; }
        bool RanAway { get; }
        IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells);
        IEnumerable<Skill> GetSkills(IEnumerable<Skill> availableSkills);
        List<StatusEffect> Status { get; }
        List<ItemInstance> Items { get; }
        int MagicDefence { get; set; }
        int CriticalAttack { get;  }
        int MaxMagic { get; set; }
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