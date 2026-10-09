using Redpoint.DungeonEscape.State;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Redpoint.DungeonEscape.Data
{
    using System.Collections.Generic;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ClassStats
    {
        public string Class { get; set; }
        public string PrimaryAbility { get; set; }
        public int HitDie { get; set; }
        public int DefaultImage { get; set; }

        public List<string> SkillProficiencies { get; set; } = new List<string>();
        public List<string> SkillOptions { get; set; } = new List<string>();
        public int SkillChoiceCount { get; set; }
    }
}
