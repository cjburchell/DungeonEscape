namespace Redpoint.DungeonEscape
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Nez.Tiled;
    using State;

    public interface IGame
    {
        TmxMap GetMap(int mapId);

        void Save();

        Party Party { get; }
        
        Names Names { get; }

        bool IsPaused { get; set; }
    
        public void UpdatePauseState();

        List<Item> Items { get; }
            
        List<Spell> Spells { get; }
        
        List<MapState> MapStates { get; }
        
        List<Monster> Monsters { get; }

        IEnumerable<GameSave> GameSaves { get; }
        bool InGame { get; }
        List<ClassStats> ClassLevelStats { get; }

        void ReloadSaveGames();
            
        void LoadGame(GameSave saveGame);
        void ResumeGame();
        void ShowMainMenu();
        void SetMap(int? mapId = null, int? spawnId = null, Vector2? point = null);
        void ShowLoadQuest();

        void StartFight(IEnumerable<Monster> monsters);
    }
}