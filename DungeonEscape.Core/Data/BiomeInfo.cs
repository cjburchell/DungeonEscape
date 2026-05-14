using Redpoint.DungeonEscape.State;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Redpoint.DungeonEscape.Data
{
    public class BiomeInfo
    {
        public Biome Type { get; set; }
        public int MinMonsterLevel { get; set; }
        public int MaxMonsterLevel { get; set; }
    }
}