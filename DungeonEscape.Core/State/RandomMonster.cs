
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Newtonsoft.Json.Converters;

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
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Rarity Rarity { get; set; } = Rarity.Common;

        public bool InBiome(Biome biome)
        {
            return !IsOverworld || Data.InBiome(biome);
        }
    }
}