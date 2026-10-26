namespace Redpoint.DungeonEscape.Data
{
    // One authored map group. Id is immutable and unique within its map.
    public sealed class StaticEncounter
    {
        public string Id { get; set; }
        public string MonsterId { get; set; }
        public int Count { get; set; }
        public string VictoryQuestId { get; set; }
        public int? VictoryQuestStage { get; set; }
    }
}
