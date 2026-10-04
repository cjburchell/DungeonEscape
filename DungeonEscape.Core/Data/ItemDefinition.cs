using Redpoint.DungeonEscape.State;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CollectionNeverUpdated.Global

namespace Redpoint.DungeonEscape.Data
{
    public class ItemDefinition
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemCategory Category { get; set; }

        [JsonProperty("Slots", ItemConverterType=typeof(StringEnumConverter))]
        public List<Slot> Slots { get; set; }

        public List<string> Classes { get; set; }
        public List<ItemName> Names { get; set; }

        public bool IsMagicItem { get; set; }
        public bool RequiresAttunement { get; set; }
        public List<Class> AttunementClasses { get; set; }
        public List<string> AttunementRequirements { get; set; }

        public int BaseStat { get; set; }
        public int DamageDice { get; set; }
        public int DamageDie { get; set; }
        public int DamageBonus { get; set; }
        public int ArmorClass { get; set; }
    }
}
