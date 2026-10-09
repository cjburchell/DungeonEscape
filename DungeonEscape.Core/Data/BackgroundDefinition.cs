using System.Collections.Generic;

namespace Redpoint.DungeonEscape.Data
{
    public sealed class BackgroundDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
        public List<string> SkillProficiencies { get; set; } = new List<string>();

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? Id ?? string.Empty : Name;
        }
    }
}
