namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class CombatActionRow
    {
        public string Label { get; set; }
        public CombatActionKind Kind { get; set; }
        public int SkillIndex { get; set; }
    }
}
