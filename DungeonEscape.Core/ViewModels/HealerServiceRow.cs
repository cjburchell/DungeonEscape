using System.Collections.Generic;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class HealerServiceRow
    {
        public HealerService Service { get; set; }
        public string Label { get; set; }
        public int Cost { get; set; }
        public List<Hero> Targets { get; set; }
        public bool NeedsTarget { get; set; }
    }
}
