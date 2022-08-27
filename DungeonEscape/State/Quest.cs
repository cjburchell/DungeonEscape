// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;

    public class Quest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinimumLevel { get; set; } = 0;
        public int? Xp { get; set; }
        public int? Gold { get; set; }
        public int? Item { get; set; }
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<QuestStage> Stages { get; set; }
    }
}