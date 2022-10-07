// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global
namespace Redpoint.DungeonEscape
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Nez.Tiled;
    using State;

    public interface IGame
    {
        TmxMap GetMap(string mapId);

        void Save(GameSave save, bool isQuick = false);

        Party Party { get; }

        string GenerateName(Gender gender);

        bool IsPaused { get; set; }
    
        public void UpdatePauseState();

        List<Item> CustomItems { get; }
            
        List<Spell> Spells { get; }
        
        List<MapState> MapStates { get; }
        
        List<Monster> Monsters { get; }
        
        List<Quest> Quests { get; }
        
        List<Dialog> Dialogs { get; }
        
        List<ItemDefinition> ItemDefinitions { get; }
        
        List<StatName> StatNames { get; }

        IEnumerable<GameSave> GameSaveSlots { get; }
        bool InGame { get; }
        List<ClassStats> ClassLevelStats { get; }
        
        Settings Settings { get; }

        void ReloadSaveGames();
        
        ISounds Sounds { get; }
        IEnumerable<GameSave> LoadableGameSaves { get; }
        List<Skill> Skills { get; }

        void LoadGame(GameSave saveGame);
        void ResumeGame();
        void ShowMainMenu();
        void SetMap(string mapId = null, string spawnId = null, Vector2? point = null);
        void ShowLoadQuest();

        void ShowNewQuest();
        
        void StartFight(IEnumerable<Monster> monsters, Biome biome);
        
        void ShowSettings();
        string AdvanceQuest(string questId, int? nextStage, bool checkLevelUp = true);
        string CheckQuest(Item item, bool checkLevelUp = true);

        Item CreateRandomItem(int maxLevel, int minLevel = 1, Rarity? rarity = null);
        Item CreateRandomEquipment(int maxLevel, int minLevel = 1, Rarity? rarity = null,
            ItemType? type = null, Class? itemClass = null, Slot? slot = null);
        
        Item CreateChestItem(int level, Rarity? rarity = null);
        Item CreateGold(int gold);
        Item GetCustomItem(string itemId);
    }
}