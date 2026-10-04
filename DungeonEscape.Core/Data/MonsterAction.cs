namespace Redpoint.DungeonEscape.Data
{
    public sealed class MonsterAction
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ItemId { get; set; }
        public int AttackBonus { get; set; }
        public int DamageDice { get; set; }
        public int DamageDie { get; set; }
        public int DamageBonus { get; set; }
        public string DamageType { get; set; }
        public string Reach { get; set; }
        public string Range { get; set; }
        public int Count { get; set; }
    }
}
