using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Redpoint.DungeonEscape.Scenes.Map.Components.Objects;

namespace Redpoint.DungeonEscape.State
{
    public class BaseState{
        public string Name { get; set; }
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        public int Id { get; set; }
        public List<Item> Items { get; set; }
        public bool IsActive { get; set; } = true;
        [JsonConverter(typeof(StringEnumConverter))]
        public SpriteType Type { get; set; }
    }
    
    public class ObjectState : BaseState
    {
        public bool? Collideable { get; set; }

        public bool? IsOpen { get; set; }
        
        public int? Level { get; set; }
    }
}