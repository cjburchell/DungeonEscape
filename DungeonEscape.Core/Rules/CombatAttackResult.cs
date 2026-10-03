namespace Redpoint.DungeonEscape.Rules
{
    public sealed class CombatAttackResult
    {
        public int Roll { get; set; }
        public int Total { get; set; }
        public int TargetArmorClass { get; set; }
        public bool Hit { get; set; }
        public bool Critical { get; set; }
        public int Damage { get; set; }
    }
}
