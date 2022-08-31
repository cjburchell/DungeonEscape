// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    
    public class GameFile
    {
        public string Version { get; set; }
        public List<GameSave> Saves { get; set; } = new();
    }
}