using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Redpoint.DungeonEscape.State
{
    public enum SpriteType
    {
        Door,
        Chest,
        Warp,
        Npc,
        NpcHeal,
        NpcStore,
        NpcSave,
        NpcKey,
        NpcPartyMember,
        Monster,
        Static,
        HiddenItem
    }

    public class BaseState
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public List<Item> Items { get; set; }
        public bool IsActive { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SpriteType Type { get; set; }

        public BaseState()
        {
            IsActive = true;
        }
    }

    public class ObjectState : BaseState
    {
        public bool? Collideable { get; set; }
        public bool? IsOpen { get; set; }
        public int? Level { get; set; }
    }
}
