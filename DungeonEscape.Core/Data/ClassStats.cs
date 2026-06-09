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
        public List<Stats> Stats { get; set; } = new List<Stats>();
        public ulong FirstLevel { get; set; }

        public List<string> Skills { get; set; } = new List<string>();
    }
}
