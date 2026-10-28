using System.Collections.Generic;

namespace Redpoint.DungeonEscape.State
{
    public class ActiveQuest
    {
        public string Id { get; set; }
        public bool Completed { get; set; }
        public List<QuestStageState> Stages { get; set; }
        public int CurrentStage { get; set; }
        public List<string> CompletedObjectiveIds { get; set; } = new List<string>();
    }
}
