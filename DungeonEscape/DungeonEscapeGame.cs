using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using DungeonEscape.Scenes;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;
using Nez.ImGuiTools;
using Nez.Tiled;
using Newtonsoft.Json;

namespace DungeonEscape
{
    using System.Linq;
    using Scenes.Fight;

    public class DungeonEscapeGame : Core, IGame
    {
        private const string saveFile = "save.json";
        private const int maxSaveSlots = 5;
        private bool isPaused;
        private bool deferredPause;
        
        public Party Party { get; set; }
        public List<MapState> MapStates { get; private set; } = new List<MapState>();
        public List<Monster> Monsters { get; } = new List<Monster>();
        public List<Item> Items { get; } = new List<Item>();
        public List<Spell> Spells { get; } = new List<Spell>();
        public IEnumerable<GameSave> GameSaves => this.saveSlots;
        public bool InGame { get; set; }
        public bool IsPaused
        {
            get => this.isPaused;
            set
            {
                if (value)
                {
                    this.isPaused = true;
                }
                
                this.deferredPause = value;
            }
        }
        
        private GameSave[] saveSlots;
        
        public void Save()
        {
            File.WriteAllText(saveFile,
                JsonConvert.SerializeObject(this.saveSlots, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
        }

        public void UpdatePauseState()
        {
            this.isPaused = this.deferredPause;
        }
        
        public IEnumerable<Spell> GetSpellList(IEnumerable<int> spells)
        {
            return spells.Select(spellId => this.Spells.FirstOrDefault(item => item.Id == spellId)).Where(spell => spell != null);
        }

        public void LoadGame(GameSave saveGame)
        {
            this.Party = saveGame.Party;
            this.MapStates = saveGame.MapStates;
            foreach (var partyItem in this.Party.Items)
            {
                partyItem.UpdateItem(this.Items);
            }

            this.InGame = true;
            this.SetMap(this.Party.SavedMapId, this.Party.SavedPoint);
        }

        public void ResumeGame()
        {
            this.SetMap(this.Party.CurrentMapId, this.Party.CurrentPosition);
        }

        public void ShowMainMenu()
        {
            StartSceneTransition(new FadeTransition(() =>
            {
                this.InGame = false;
                var splash = new MainMenu();
                splash.Initialize();
                return splash;
            }));
        }

        public void SetMap(int? mapId, Point? point)
        {
            mapId ??= 0;
            this.IsPaused = true;
            var map = new MapScene(this, mapId.Value, point);
            var transition = new FadeTransition(() =>
            {
                map.Initialize();
                return map;
            });
            transition.OnTransitionCompleted += () => { this.IsPaused = false; };

            StartSceneTransition(transition);
        }

        public void ShowLoadQuest()
        {
            StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new ContinueQuestScene();
                splash.Initialize();
                return splash;
            }));
        }

        public void StartFight(IEnumerable<Monster> monsters)
        {
            StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new FightScene(this, monsters);
                splash.Initialize();
                return splash;
            }));
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            var imGui = new ImGuiManager();
            RegisterGlobalManager(imGui);
            imGui.SetEnabled(false);
            
            ExitOnEscapeKeypress = false;
            PauseOnFocusLost = false;

            this.ReloadSaveGames();

            var tileSet = LoadTileSet($"Content/items.tsx");
            foreach (var (_, tile) in tileSet.Tiles)
            {
                this.Items.Add(new Item(tile));
            }
            
            var spellTileset = LoadTileSet($"Content/spells.tsx");
            foreach (var (_, tile) in spellTileset.Tiles)
            {
                this.Spells.Add(new Spell(tile));
            }
            
            var monsterTileset = LoadTileSet($"Content/allmonsters.tsx");
            foreach (var (_, tile) in monsterTileset.Tiles)
            {
                this.Monsters.Add(new Monster(tile, this.Spells));
            }

            DebugRenderEnabled = false;
            Window.AllowUserResizing = true;
            Screen.SetSize(MapScene.ScreenWidth, MapScene.ScreenHeight);
            Scene = new EmptyScene();
            StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new SplashScreen();
                splash.Initialize();
                return splash;
            }));
        }

        public void ReloadSaveGames()
        {
            this.saveSlots = this.LoadSaveGames(saveFile);
        }

        private GameSave[] LoadSaveGames(string fileName)
        {
            var saves = new List<GameSave>();
            if (File.Exists(fileName))
            {
                saves = JsonConvert.DeserializeObject<List<GameSave>>(File.ReadAllText(fileName)) ?? new List<GameSave>();
            }

            for (var i = saves.Count; i < maxSaveSlots; i++)
            {
                saves.Add(new GameSave()); 
            }

            return saves.ToArray();
        }

        public static TmxTileset LoadTileSet(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            
            using var stream = TitleContainer.OpenStream(path);
            var xDocTileSet = XDocument.Load(stream);

            var tsxDir = Path.GetDirectoryName(path);
            var tileSet = new TmxTileset().LoadTmxTileset(null, xDocTileSet.Element("tileset"), 0, tsxDir);
            tileSet.TmxDirectory = tsxDir;

            return tileSet;
        }

        public TmxMap GetMap(int mapId)
        {
            return Content.LoadTiledMap($"Content/map{mapId}.tmx");
        }
    }
}