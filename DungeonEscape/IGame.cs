using System.Collections.Generic;
using DungeonEscape.State;
using Nez.Tiled;

namespace DungeonEscape
{
    using Microsoft.Xna.Framework;

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
        bool InGame { get; set; }    
        
        IEnumerable<Spell> GetSpellList(IEnumerable<int> spells);
        void LoadGame(GameSave saveGame);
        void ResumeGame();
        void ShowMainMenu();
        void SetMap(int? mapId = null, Point? point = null);
        void ShowLoadQuest();

        void StartFight(List<Monster> monsters);
    }
}