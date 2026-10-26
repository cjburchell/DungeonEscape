using System.Collections.Generic;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Data
{
    public sealed class StartingCharacter
    {
        public string Name { get; set; }
        public Class Class { get; set; }
        public Gender Gender { get; set; }
        public Species Species { get; set; }
        public string Background { get; set; }
        public int? SpriteFrameIndex { get; set; }
        public int[] AbilityScores { get; set; }
        public List<string> SkillProficiencies { get; set; }
    }
}
