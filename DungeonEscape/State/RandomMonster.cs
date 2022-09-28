
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Redpoint.DungeonEscape.State
{
    using Newtonsoft.Json;


    public class RandomMonster
    {
        public string Name { get; set; }
        
        [JsonIgnore]
        public Monster Data { get; set; }
        
        [JsonIgnore]
        public bool IsOverworld { get; set; }
        
        public int Probability { get; set; } = 1;

        public bool InBiome(Biome biome)
        {
            return !IsOverworld || Data.InBiome(biome);
        }
    }
}