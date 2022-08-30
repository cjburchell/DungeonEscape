namespace Redpoint.DungeonEscape
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using Nez;
    using Nez.Tiled;
    using Scenes;
    using Scenes.Fight;
    using Scenes.Map;
    using State;


    public class Game : Core, IGame
    {
        private const string SaveFile = "save.json";
        private const int MaxSaveSlots = 5;
        private bool _isPaused;
        private bool _deferredPause;
        
        public Party Party { get; private set; }
        
        public List<ClassStats> ClassLevelStats { get; private set; } = new();
        public List<MapState> MapStates { get; private set; } = new();
        public List<Monster> Monsters { get; } = new();
        public List<Item> CustomItems { get; } = new();
        public List<Spell> Spells { get; } = new();
        public List<Quest> Quests { get; private set; } = new();
        
        public List<Dialog> Dialogs { get; private set; } = new();
        
        public IEnumerable<GameSave> GameSaveSlots => this._saveSlots.Where(i=> !i.IsQuick);
        public IEnumerable<GameSave> LoadableGameSaves => this._saveSlots;
        public bool InGame { get; private set; }
        public bool IsPaused
        {
            get => this._isPaused;
            set
            {
                if (value)
                {
                    this._isPaused = true;
                }
                
                this._deferredPause = value;
            }
        }
        
        private List<GameSave> _saveSlots;
        
        public void Save(GameSave save, bool isQuick = false)
        {
            if (save == null)
            {
                this.ReloadSaveGames();
                save = this._saveSlots.FirstOrDefault(i => i.IsQuick == isQuick);
                if (save == null)
                {
                    save = new GameSave();
                    this._saveSlots.Add(save);
                }
            }
            
            this.Party.SavedMapId = this.Party.CurrentMapId;
            this.Party.SavedPoint = this.Party.CurrentPosition;
            save.Party = this.Party;
            save.MapStates = this.MapStates;
            save.Time = DateTime.Now;
            save.IsQuick = isQuick;
            
            File.WriteAllText(SaveFile,
                JsonConvert.SerializeObject(this._saveSlots, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
        }

        public void UpdatePauseState()
        {
            this._isPaused = this._deferredPause;
        }

        public void LoadGame(GameSave saveGame)
        {
            this.Party = saveGame.Party;
            this.MapStates = saveGame.MapStates;
            var tileSet = LoadTileSet("Content/items2.tsx");
            foreach (var partyItem in this.Party.Members.SelectMany(partyMember => partyMember.Items))
            {
                partyItem.Item.Setup(tileSet);
            }
            
            this.InGame = true;
            this.SetMap(this.Party.SavedMapId, null, this.Party.SavedPoint);
        }

        public void ResumeGame()
        {
            this.SetMap(this.Party.CurrentMapId, null, this.Party.CurrentPosition);
        }

        public void ShowMainMenu()
        {
            StartSceneTransition(new FadeTransition(() =>
            {
                this.InGame = false;
                var splash = new MainMenu(this.Sounds);
                splash.Initialize();
                return splash;
            }));
        }

        public void SetMap(int? mapId, int? spawnId , Vector2? point)
        {
            mapId ??= 0;
            this.IsPaused = true;
            if (this.Party.CurrentMapId != mapId)
            {
                if (this.Party.CurrentMapIsOverWorld)
                {
                    this.Save(null, true);
                }
                else
                {
                    var mapFile = this.GetMap(mapId.Value);
                    var isNewMapOverworld = mapFile.Properties != null && mapFile.Properties.ContainsKey("overworld") &&
                                            bool.Parse(mapFile.Properties["overworld"]);
                    if (isNewMapOverworld)
                    {
                        this.Save(null, true);
                    }
                    
                }
            }
            
            var map = new MapScene(this, mapId.Value, spawnId, point);
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
                var splash = new ContinueQuestScene(this.Sounds);
                splash.Initialize();
                return splash;
            }));
        }

        public void StartFight(IEnumerable<Monster> monsters, Biome biome)
        {
            StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new FightScene(this, monsters, biome);
                splash.Initialize();
                return splash;
            }));
        }

        protected override void Initialize()
        {
            base.Initialize();
            ExitOnEscapeKeypress = false;
            PauseOnFocusLost = true;
            
            this.Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("Content/data/gameSettings.json"));
            
            this.ReloadSaveGames();
            
            this.Names = JsonConvert.DeserializeObject<Names>(File.ReadAllText("Content/data/names.json"));
            
            this.Quests = JsonConvert.DeserializeObject<List<Quest>>(File.ReadAllText("Content/data/quests.json"));
            this.Dialogs = JsonConvert.DeserializeObject<List<Dialog>>(File.ReadAllText("Content/data/dialog.json"));
            
            this.ClassLevelStats = JsonConvert.DeserializeObject<List<ClassStats>>(File.ReadAllText("Content/data/classLevels.json"));
            
            this.StatNames = JsonConvert.DeserializeObject<List<StatName>>(File.ReadAllText("Content/data/statnames.json"));
            this.ItemDefinitions = JsonConvert.DeserializeObject<List<ItemDefinition>>(File.ReadAllText("Content/data/itemdef.json"));
            
            var tileSet = LoadTileSet("Content/items2.tsx");
            var items = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText("Content/data/customitems.json"));
            if (items != null)
            {
                foreach (var item in items)
                {
                    item.Setup(tileSet);
                    this.CustomItems.Add(item);
                }
            }
            
            var spellTiles = LoadTileSet("Content/items.tsx");
            var spells = JsonConvert.DeserializeObject<List<Spell>>(File.ReadAllText("Content/data/spells.json"));
            if (spells != null)
            {
                foreach (var spell in spells)
                {
                    spell.Setup(spellTiles);
                    this.Spells.Add(spell);
                }
            }
            
            var monsterTileSet = LoadTileSet("Content/allmonsters.tsx");
            var monsters = JsonConvert.DeserializeObject<List<Monster>>(File.ReadAllText("Content/data/allmonsters.json"));
            if (monsters != null)
            {
                foreach (var monster in monsters)
                {
                    var tile = monsterTileSet.Tiles.FirstOrDefault(item => item.Value.Id == monster.Id).Value;
                    monster.Setup(tile);
                    this.Monsters.Add(monster);
                }
            }
            

            DebugRenderEnabled = false;
            this.Window.AllowUserResizing = true;
            Screen.SetSize(MapScene.ScreenWidth, MapScene.ScreenHeight);
            Scene = new EmptyScene();
            StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new SplashScreen(this.Sounds);
                splash.Initialize();
                return splash;
            }));
        }

        public Settings Settings { get; private set; }

        public List<ItemDefinition> ItemDefinitions { get; private set; }

        public List<StatName> StatNames { get; private set; }

        public Names Names { get; private set; }

        public void ReloadSaveGames()
        {
            this._saveSlots = LoadSaveGames(SaveFile);
        }

        public ISounds Sounds { get; } = new Sounds();

        private static List<GameSave> LoadSaveGames(string fileName)
        {
            var saves = new List<GameSave>();
            if (File.Exists(fileName))
            {
                saves = JsonConvert.DeserializeObject<List<GameSave>>(File.ReadAllText(fileName)) ?? new List<GameSave>();
            }

            for (var i = saves.Count(i => !i.IsQuick); i < MaxSaveSlots; i++)
            {
                saves.Add(new GameSave()); 
            }

            return saves;
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