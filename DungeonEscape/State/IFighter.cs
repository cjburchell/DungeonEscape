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
        bool RanAway { get; }
        IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells);

        List<StatusEffect> Status { get; }
        List<ItemInstance> Items { get; }
        void AddEffect(StatusEffect effect);
        void RemoveEffect(StatusEffect effect);
        void Use(ItemInstance item);
        void Equip(ItemInstance item);
        List<string> GetEquipmentId(IEnumerable<Slot> slots);
        string UpdateStatusEffects(int round, DurationType durationType, IGame game);
        void PlayDamageAnimation();
    }
}