using System.Collections.Generic;
using DungeonEscape.State;
using Nez.Tiled;

namespace DungeonEscape
{
    public interface IGame
    {
        TmxMap GetMap(int mapId);

        int CurrentMapId { get; set; }

        Party Party { get; }
        
        bool IsPaused { get; set; }

        public void UpdatePauseState();

        List<Item> Items { get; }
        
        List<Spell> Spells { get; }
    }
}