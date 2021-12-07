// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;

    public class QuestStage
    {

        public int Number { get; set; }
        public string Description { get; set; }
    }
    
    public class Quest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? Xp { get; set; }
        public int? Gold { get; set; }
        public int? Item { get; set; }
        public bool Completed { get; set; }
        public List<QuestStage> Stages { get; set; }
    }
}