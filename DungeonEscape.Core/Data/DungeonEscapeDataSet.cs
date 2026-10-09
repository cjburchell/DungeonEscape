using System.Collections.Generic;

namespace Redpoint.DungeonEscape.Data
{
    public sealed class DungeonEscapeDataSet
    {
        public List<Item> CustomItems { get; set; }
        public List<Skill> Skills { get; set; }
        public List<Spell> Spells { get; set; }
        public List<Monster> Monsters { get; set; }
        public List<Quest> Quests { get; set; }
        public List<Dialog> Dialogs { get; set; }
        public List<ClassStats> ClassLevels { get; set; }
        public List<SpeciesDefinition> Species { get; set; }
        public List<BackgroundDefinition> Backgrounds { get; set; }
        public Names Names { get; set; }
        public TiledMapInfo TestMap { get; set; }

        public DungeonEscapeDataSet()
        {
            CustomItems = new List<Item>();
            Skills = new List<Skill>();
            Spells = new List<Spell>();
            Monsters = new List<Monster>();
            Quests = new List<Quest>();
            Dialogs = new List<Dialog>();
            ClassLevels = new List<ClassStats>();
            Species = new List<SpeciesDefinition>();
            Backgrounds = new List<BackgroundDefinition>();
        }

        public void Link()
        {
            foreach (var item in CustomItems)
            {
                item.Setup(Skills);
            }

            foreach (var spell in Spells)
            {
                spell.Setup(Skills);
            }
        }
    }
}
