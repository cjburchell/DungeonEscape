namespace Redpoint.DungeonEscape.Unity.Map
{
    public sealed class Transition
    {
        public string MapId { get; set; }
        public string SpawnId { get; set; }
        public bool UseSavedOverWorldPosition { get; set; }
    }
}
