using System.Collections.Generic;
using DungeonEscape.State;
using Nez.Tiled;

namespace DungeonEscape
{
    public interface IGame
    {
        TmxMap GetMap(int mapId);

        void Save();

        Party Party { get; set; }

        bool IsPaused { get; set; }
    
        public void UpdatePauseState();

        List<Item> Items { get; }
            
        List<Spell> Spells { get; }
        
        List<MapState> MapStates { get; }

        IEnumerable<GameSave> GameSaves { get; }
        IEnumerable<Spell> GetSpellList(IEnumerable<int> spells);
        void LoadGame(GameSave saveGame);
    }
}