namespace Redpoint.DungeonEscape.Data
{
    public sealed class QuestObjective
    {
        // Stable identity local to this quest; display text may change independently.
        public string Id { get; set; }
        public string Description { get; set; }
        public string MapId { get; set; }
        public string EncounterId { get; set; }
    }
}
