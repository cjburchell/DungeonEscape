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
        private const string GameName = "Dungeon Escape";

        public static readonly string SavePath =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Redpoint\\DungeonEscape";
        private static readonly string SaveFile = $"{SavePath}\\save.json";
        private const string SaveFileVersion = "1.0";
        public static readonly string SettingsFile =  $"{SavePath}\\settings.json";
        private const string SettingsFileVersion = "1.0";
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
        
        public IEnumerable<GameSave> GameSaveSlots => this._gameFile.Saves.Where(i=> !i.IsQuick);
        public IEnumerable<GameSave> LoadableGameSaves => this._gameFile.Saves;
        public List<Skill> Skills { get; private set; } = new ();
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
        
        private GameFile _gameFile;

        public Game() : base(MapScene.ScreenWidth, MapScene.ScreenHeight, false, GameName)
        {
        }
        
        public void Save(GameSave save, bool isQuick = false)
        {
            if (save == null)
            {
                this.ReloadSaveGames();
                save = this._gameFile.Saves.FirstOrDefault(i => i.IsQuick == isQuick);
                if (save == null)
                {
                    save = new GameSave();
                    this._gameFile.Saves.Add(save);
                }
            }

            if (!isQuick)
            {
                this.Party.SavedMapId = this.Party.CurrentMapId;
                this.Party.SavedPoint = this.Party.CurrentPosition;
            }

            save.Party = this.Party;
            save.MapStates = this.MapStates;
            save.Time = DateTime.Now;
            save.IsQuick = isQuick;

            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
            
            File.WriteAllText(SaveFile,
                JsonConvert.SerializeObject(this._gameFile, Formatting.Indented,
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
            this.SetMap(this.Party.CurrentMapId, null, this.Party.CurrentPosition);
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

        public void ShowNewQuest()
        {
            StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new CreatePlayerScene(this.Sounds);
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

        public void ShowSettings()
        {
            StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new SettingsScene(this.Sounds);
                splash.Initialize();
                return splash;
            }));
        }

        protected override void Initialize()
        {
            base.Initialize();
            ExitOnEscapeKeypress = false;
            PauseOnFocusLost = true;

            if (File.Exists(SettingsFile))
            {
                this.Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsFile));
                if (this.Settings is not { Version: SettingsFileVersion })
                {
                    this.Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("Content/data/default_settings.json"));
                }
                
            }
            else
            {
                this.Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("Content/data/default_settings.json"));
            }
            
            this.Settings ??= new Settings {Version = SettingsFileVersion};
            this.Sounds.MusicVolume = this.Settings.MusicVolume;
            this.Sounds.SoundEffectsVolume = this.Settings.SoundEffectsVolume;
            
            this.ReloadSaveGames();
            
            this.Names = JsonConvert.DeserializeObject<Names>(File.ReadAllText("Content/data/names.json"));
            
            this.Quests = JsonConvert.DeserializeObject<List<Quest>>(File.ReadAllText("Content/data/quests.json"));
            this.Dialogs = JsonConvert.DeserializeObject<List<Dialog>>(File.ReadAllText("Content/data/dialog.json"));
            
            this.ClassLevelStats = JsonConvert.DeserializeObject<List<ClassStats>>(File.ReadAllText("Content/data/classLevels.json"));
            
            this.StatNames = JsonConvert.DeserializeObject<List<StatName>>(File.ReadAllText("Content/data/statnames.json"));
            this.ItemDefinitions = JsonConvert.DeserializeObject<List<ItemDefinition>>(File.ReadAllText("Content/data/itemdef.json"));
            this.Skills = JsonConvert.DeserializeObject<List<Skill>>(File.ReadAllText("Content/data/skills.json"));
            
            var tileSet = LoadTileSet("Content/items2.tsx");
            var items = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText("Content/data/customitems.json"));
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item.Id))
                    {
                        item.Id = item.Name;
                    }
                    
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
            this.Window.Title = GameName;
            if (this.Settings.IsFullScreen)
            {
                Screen.IsFullscreen = true;
                Screen.SetSize(Screen.MonitorWidth, Screen.MonitorHeight);
            }
            else
            {
                Screen.SetSize(MapScene.ScreenWidth, MapScene.ScreenHeight);
            }
            
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
            this._gameFile = LoadSaveGames(SaveFile);
        }

        public ISounds Sounds { get; } = new Sounds();

        private static GameFile LoadSaveGames(string fileName)
        {
            GameFile file = null;
            if (File.Exists(fileName))
            {
                file = JsonConvert.DeserializeObject<GameFile>(File.ReadAllText(fileName));
            }

            if (file is not { Version: SaveFileVersion })
            {
                file = new GameFile { Version = SaveFileVersion };
            }

            for (var i = file.Saves.Count(i => !i.IsQuick); i < MaxSaveSlots; i++)
            {
                file.Saves.Add(new GameSave()); 
            }

            return file;
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