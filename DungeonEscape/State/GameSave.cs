namespace Redpoint.DungeonEscape.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public class GameSave
    {
        [JsonIgnore]
        public string Name => this.Party?.Members.FirstOrDefault()?.Name ?? "Empty";
        [JsonIgnore]
        public int? Level => this.Party?.Members.FirstOrDefault()?.Level;
        public DateTime? Time { get; set; }
        public Party Party { get; set; }
        
        public List<MapState> MapStates { get; set; } = new();
    }
}