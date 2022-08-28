namespace Redpoint.DungeonEscape.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public class GameSave
    {
        [JsonIgnore]
        public string Name
        {
            get
            {
                var name = this.Party?.Members.FirstOrDefault()?.Name ?? "Empty";
                return this.IsQuick ? $"Quick ({name})" : name;
            }
        }

        [JsonIgnore]
        public int? Level => this.Party?.Members.FirstOrDefault()?.Level;
        public DateTime? Time { get; set; }
        public Party Party { get; set; }

        public bool IsQuick { get; set; }

        public List<MapState> MapStates { get; set; } = new();
    }
}