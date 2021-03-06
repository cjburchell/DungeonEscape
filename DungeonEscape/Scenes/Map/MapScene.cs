namespace Redpoint.DungeonEscape.Scenes.Map
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Common.Components.UI;
    using Components;
    using Components.Objects;
    using Components.UI;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Newtonsoft.Json;
    using Nez;
    using Nez.AI.Pathfinding;
    using Nez.Console;
    using Nez.Tiled;
    using Nez.UI;
    using State;
    using Game = DungeonEscape.Game;

    public class MapScene : Scene
    {
        private const int ScreenSpaceRenderLayer = 999;
        
        public const int DefaultTileSize = 32;
        private const int ScreenTileWidth = 32;
        private const int ScreenTileHeight = 18;
        public const int ScreenWidth = ScreenTileWidth * DefaultTileSize;
        public const int ScreenHeight = ScreenTileHeight * DefaultTileSize;
        public const SceneResolutionPolicy SceneResolution = SceneResolutionPolicy.ShowAll;
        
        private readonly int _mapId;
        private readonly Vector2? _start;
        private readonly IGame _gameState;
        private Label _debugText;
        private List<RandomMonster> _randomMonsters = new List<RandomMonster>();
        private VirtualButton _showCommandWindowInput;
        private VirtualButton _showExitWindowInput;
        private UiSystem _ui;
        private int? _spawnId;

        public MapScene(IGame game, int mapId, int? spawnId, Vector2? start = null)
        {
            this._mapId = mapId;
            this._start = start;
            this._spawnId = spawnId;
            this._gameState = game;
        }

        public static Point ToMapGrid(Vector2 pos, TmxMap map)
        {
            var (x, y) = pos;
            return new Point {X = (int) (x / map.TileWidth), Y = (int) (y / map.TileHeight)};
        }

        public static Vector2 ToRealLocation(Point point, TmxMap map)
        {
            var (x, y) = point;
            return new Vector2(x * map.TileWidth + map.TileWidth / 2,
                y * map.TileHeight + map.TileHeight / 2);
        }

        private static AstarGridGraph CreateGraph(TmxMap map)
        {
            var wall = map.GetLayer<TmxLayer>("wall");
            var water = map.GetLayer<TmxLayer>("water");

            var itemObjects = map.GetObjectGroup("items");
            var itemLayer = new TmxLayer
            {
                Width = wall.Width,
                Height = wall.Height,
                Tiles = new TmxLayerTile[wall.Width * wall.Height],
                Map = map
            };

            foreach (var item in itemObjects.Objects)
            {
                if (!bool.Parse(item.Properties["Collideable"]) && item.Type != SpriteType.Warp.ToString())
                {
                    continue;
                }

                var x = (int) ((item.X + (int) (map.TileWidth / 2.0)) / map.TileWidth);
                var y = (int) ((item.Y - (int) (map.TileHeight / 2.0)) / map.TileHeight);
                itemLayer.SetTile(new TmxLayerTile(map, 1, x, y));
            }

            return new AstarGridGraph(new[] {wall, water, itemLayer});
        }

        public override void Initialize()
        {
            base.Initialize();
            
            var map = this._gameState.GetMap(this._mapId);
            this.SetDesignResolution(ScreenTileWidth * map.TileWidth, ScreenTileHeight * map.TileHeight,
                SceneResolution);
            
            var isOverWorld = map.Properties != null && map.Properties.ContainsKey("overworld") && bool.Parse(map.Properties["overworld"]);

            var songPath = map.Properties != null && map.Properties.ContainsKey("song")?map.Properties["song"]:@"not-in-vain";
            this._gameState.Sounds.PlayMusic(songPath);
            
            this._randomMonsters = this.LoadRandomMonsters();

            this._gameState.Party.CurrentMapId = this._mapId;
            this._gameState.Party.CurrentMapIsOverWorld = isOverWorld;

            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            this._ui = new UiSystem(this.CreateEntity("ui-canvas").AddComponent(new UICanvas()), this._gameState.Sounds);
            this._ui.Canvas.SetRenderLayer(999);
            this._ui.Canvas.Stage.GamepadActionButton = null;
            
            this._debugText = this._ui.Canvas.Stage.AddElement(new Label("", BasicWindow.Skin));
            this._debugText.SetPosition(10, 20);
            this._debugText.SetIsVisible(this._gameState.Settings.MapDebugInfo);

            {
                var tiledEntity = this.CreateEntity("map");
                var tiledMapRenderer = tiledEntity.AddComponent(new TiledMapRenderer(map,
                    this._gameState.Party.HasShip && this._gameState.Party.CurrentMapIsOverWorld ? new[] {"wall"} : new[] {"wall", "water"}));
                tiledMapRenderer.RenderLayer = 50;
                tiledMapRenderer.SetLayersToRender("wall", "wall2", "water", "floor", "floor2");
                
                var topLeft = new Vector2(0, 0);
                var bottomRight = new Vector2(map.TileWidth * map.Width,
                    map.TileWidth * map.Height);
                tiledEntity.AddComponent(new CameraBounds(topLeft, bottomRight));
            }

            {
                var ceilingEntity = this.CreateEntity("ceiling");
                var ceilingMapRenderer = ceilingEntity.AddComponent(new TiledMapRenderer(map, null, false));
                ceilingMapRenderer.RenderLayer = 5;
                ceilingMapRenderer.SetLayersToRender("ceiling", "ceiling2");
            }

            var mapState = this._gameState.MapStates.FirstOrDefault(item => item.Id == this._mapId);
            if (mapState == null)
            {
                mapState = new MapState {Id = this._mapId};
                this._gameState.MapStates.Add(mapState);
            }

            var objects = map.GetObjectGroup("items");
            foreach (var item in objects.Objects)
            {
                var state = mapState.Objects.FirstOrDefault(i => item.Id == i.Id);
                if (state == null)
                {
                    state = new ObjectState {Id = item.Id};
                    mapState.Objects.Add(state);
                }
                
                if (!state.IsActive)
                {
                    continue;
                }
                
                var itemEntity = this.CreateEntity(item.Name);
                itemEntity.AddComponent(MapObject.Create(item, state,
                    map, this._ui, this._gameState));
            }

            var graph = CreateGraph(map);
            var sprites = map.GetObjectGroup("sprites");
            var tileSet = Game.LoadTileSet("Content/items.tsx");
            foreach (var item in sprites.Objects)
            {
                var state = mapState.Sprites.FirstOrDefault(i => item.Id == i.Id);
                if (state == null)
                {
                    state = new SpriteState {Id = item.Id};
                    mapState.Sprites.Add(state);
                }

                if (state.Items != null)
                {
                    foreach (var spriteItem in state.Items)
                    {
                        spriteItem.Setup(tileSet);
                    }
                }
                

                if (!state.IsActive)
                {
                    continue;
                }

                var spriteEntity = this.CreateEntity(item.Name);
                spriteEntity.AddComponent(Sprite.Create(item, state, map, this._ui, this._gameState, graph));
            }
            
            var spawn = new Vector2();
            if (this._start == null)
            {
                if (this._spawnId.HasValue)
                {
                    var key = $"spawn{this._spawnId.Value}";
                    if (map.GetObjectGroup("objects") != null &&
                        map.GetObjectGroup("objects").Objects.TryGetValue(key, out var spawnObject))
                    {
                        spawn.X = spawnObject.X + spawnObject.Width / 2.0f;
                        spawn.Y = spawnObject.Y + spawnObject.Height / 2.0f;
                    }
                }
                else if (this._gameState.Party.CurrentMapIsOverWorld && this._gameState.Party.OverWorldPosition != Vector2.Zero)
                {
                    spawn = this._gameState.Party.OverWorldPosition;
                }
                else
                {
                    if (map.GetObjectGroup("objects") != null &&
                        map.GetObjectGroup("objects").Objects.TryGetValue("spawn", out var spawnObject))
                    {
                        spawn.X = spawnObject.X + spawnObject.Width / 2.0f;
                        spawn.Y = spawnObject.Y + spawnObject.Height / 2.0f;
                    }
                }
            }
            else
            {
                spawn = this._start.Value;
            }

            var first = true;
            Entity lastEntity = null;
            PlayerComponent player = null;
            foreach (var hero in this._gameState.Party.Members)
            {
                if (first)
                {
                    var playerEntity = this.CreateEntity(hero.Name, spawn);
                    player = playerEntity.AddComponent(new PlayerComponent( hero, this._gameState, map, this._debugText, this._randomMonsters, this._ui)).GetComponent<PlayerComponent>();
                    this.Camera.Entity.AddComponent(new FollowCamera(playerEntity, FollowCamera.CameraStyle.CameraWindow));
                    first = false;
                    lastEntity = playerEntity;
                }
                else
                {
                    var followerEntity = this.CreateEntity(hero.Name, spawn);
                    followerEntity.AddComponent(new Follower( hero, lastEntity, player, this._gameState));
                    lastEntity = followerEntity;
                }
            }

            this._showCommandWindowInput = new VirtualButton();
            this._showCommandWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
            this._showCommandWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.X));
            
            this._showExitWindowInput = new VirtualButton();
            this._showExitWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Escape));
            this._showExitWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.Y));
        }
        
        public static Biome GetCurrentBiome(TmxMap map, Vector2 pos)
        {
            var isOverWorld = map.Properties != null && map.Properties.ContainsKey("overworld") && bool.Parse(map.Properties["overworld"]);
            if (!isOverWorld)
            {
                var biome = map.Properties != null && map.Properties.ContainsKey("biome")? Enum.Parse<Biome>(map.Properties["biome"]):Biome.None;
                return biome;
            }

            var (x, y) = MapScene.ToMapGrid(pos, map);
            var tile = map.GetLayer<TmxLayer>("biomes")?.GetTile(x, y);
            if (tile == null)
            {
                return Biome.None;
            }

            var tileset = tile.Tileset;
            return (Biome) (tile.Gid - tileset.FirstGid);
        }

        private List<RandomMonster> LoadRandomMonsters()
        {
            var fileName = $"Content/data/monsters{this._mapId}.json";
            if (!File.Exists(fileName))
            {
                return new List<RandomMonster>();
            }

            var random = JsonConvert.DeserializeObject<List<RandomMonster>>(File.ReadAllText(fileName));
            if (random == null)
            {
                return new List<RandomMonster>();
            }

            var list = new List<RandomMonster>();
            foreach (var monster in random)
            {
                monster.Data = this._gameState.Monsters.FirstOrDefault(item => item.Id == monster.Id);
                if (monster.Data != null)
                {
                    list.Add(monster);
                }
            }

            return list;

        }

        [Command("map", "switches to map")]
        // ReSharper disable once UnusedMember.Global
        public static void SetMap(int mapId = 0)
        {
            var game = Core.Instance as IGame;
            game?.SetMap(mapId);
        }
        
        [Command("fight", "fights a monster")]
        // ReSharper disable once UnusedMember.Global
        public static void StartFight(int monsterId = 0)
        {
            var game = Core.Instance as IGame;
            var monster = game?.Monsters.FirstOrDefault(m => m.Id == monsterId);
            if (monster != null)
            {
                game.StartFight(new[]{monster}, Biome.Grassland);
            }
        }
        
        [Command("level", "fights a monster")]
        // ReSharper disable once UnusedMember.Global    
        public static void SetLevel(int level = 1)
        {
            if (!(Core.Instance is IGame game))
            {
                return;
            }
            
            foreach (var member in game.Party.Members)
            {
                member.RollStats(game.ClassLevelStats, level);
            }
        }

        public override void Update()
        {
            this._ui.Input.HandledHide = false;
            if (this._gameState.IsPaused)
            {
                base.Update();
                return;
            }
            
            if (this._showCommandWindowInput.IsReleased)
            {
                var menuItems = new List<string> {"Status"};
                if (this._gameState.Party.Members.Count(member => member.GetSpells(this._gameState.Spells).Count() != 0) != 0)
                {
                    menuItems.Add("Spells");
                }

                if (this._gameState.Party.Items.Count != 0)
                {
                    menuItems.Add("Items");
                }
                
                this._gameState.IsPaused = true;
                void UnPause()
                {
                    this._gameState.IsPaused = false;
                }
                
                var commandMenu = new CommandMenu(this._ui);
                commandMenu.Show(menuItems, result =>
                {
                    switch (result)
                    {
                        case "Spells":
                            this.ShowSpell(UnPause);
                            break;
                        case "Status":
                            this.ShowHeroStatus(UnPause);
                            break;
                        case "Items":
                            this.ShowItems(UnPause);
                            break;
                        default:
                            UnPause();
                            break;
                    }   
                });
            }
            else if(this._showExitWindowInput.IsReleased)
            {
                this._ui.Input.HandledHide = true;
                this._gameState.IsPaused = true;
                var commandMenu = new SelectWindow<string>(this._ui, null, new Point(20,20), 200);
                commandMenu.Show(new []{"Main Menu", "Load Quest", "Quit"}, result =>
                {
                    switch (result)
                    {
                        case "Main Menu":
                            this._gameState.ShowMainMenu();
                            break;
                        case "Load Quest":
                            this._gameState.ShowLoadQuest();
                            break;
                        case "Quit":
                            Core.Exit();
                            break;
                        default:
                            this._gameState.IsPaused = false;
                            break;
                    }
                });
            }
            
            base.Update();
        }

        private void ShowHeroStatus(Action done)
        {
            if (this._gameState.Party.Members.Count == 1)
            {
                var statusWindow = new HeroStatusWindow(this._ui);
                statusWindow.Show(this._gameState.Party.Members.First(), done);
            }
            else
            {
                var selectWindow = new SelectHeroWindow(this._ui);
                selectWindow.Show( this._gameState.Party.Members, hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    var statusWindow = new HeroStatusWindow(this._ui);
                    statusWindow.Show(hero, done);
                });
            }
        }

        private void CastSpell(IFighter caster, Spell spell, Action done)
        {
            if (spell.Targets == Target.Group)
            {
                var result = spell.Cast(this._gameState.Party.Members, caster, this._gameState);
                if (string.IsNullOrEmpty(result))
                {
                    done();
                    return;
                }
                
                new TalkWindow(this._ui).Show(result, done);
                return;
            }

            Func<Hero,bool> filter = hero => !hero.IsDead;
            if (spell.Type == SpellType.Revive)
            {
                filter = hero => hero.IsDead;
            }
                
            if(this._gameState.Party.Members.Count(filter) == 1 && spell.Type != SpellType.Revive)
            {
                var result = spell.Cast(this._gameState.Party.Members.Where(filter), caster, this._gameState);
                new TalkWindow(this._ui).Show(result, done);
                return;
            }
                
            new SelectHeroWindow(this._ui).Show(this._gameState.Party.Members.Where(filter), target =>
            {
                if (target == null)
                {
                    done();
                    return;
                }

                new TalkWindow(this._ui).Show(spell.Cast(new[] {target}, caster, this._gameState), done);
            });
        }

        private void ShowSpell(Action done)
        {
            if (this._gameState.Party.Members.Count == 1)
            {
                var hero = this._gameState.Party.Members.First();
                var spellWindow = new SpellWindow(this._ui, hero);
                spellWindow.Show(hero.GetSpells(this._gameState.Spells).Where(item => item.IsNonEncounterSpell), spell=>
                {
                    if (spell == null)
                    {
                        done();
                        return;
                    }

                    this.CastSpell(this._gameState.Party.Members.First(), spell, done);

                });
            }
            else
            {
                var selectWindow = new SelectHeroWindow(this._ui);
                selectWindow.Show(this._gameState.Party.Members.Where(item => item.GetSpells(this._gameState.Spells).Count() != 0), hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    
                    var spellWindow = new SpellWindow(this._ui, hero);
                    spellWindow.Show(hero.GetSpells(this._gameState.Spells).Where(item => item.IsNonEncounterSpell), spell =>
                    {
                        if (spell == null)
                        {
                            done();
                            return;
                        }
                        
                        this.CastSpell(hero, spell, done);
                    });
                });
            }
        }

        private void ShowItems(Action done)
        {
            if (this._gameState.Party.Items.Count == 0)
            {
                new TalkWindow(this._ui).Show("The party has no items", done);
            }
            else
            {
                var inventoryWindow = new InventoryWindow(this._ui);
                inventoryWindow.Show(this._gameState.Party.Items, item =>
                {
                    if (item == null)
                    {
                        done();
                        return;
                    }
                    
                    var menuItems = new List<string>();
                    if (this._gameState.Party.Members.Count(hero => hero.CanUseItem(item)) != 0)
                    {
                        menuItems.Add(item.IsEquippable ? "Equip" : "Use");
                    }
                    menuItems.Add("Drop");
                    
                    var selectWindow = new SelectWindow<string>(this._ui, "Select", new Point(20, 20));
                    selectWindow.Show(menuItems, action =>
                    {
                        switch (action)
                        {
                            case "Equip":
                            case "Use":
                                if (this._gameState.Party.Members.Count == 1)
                                {
                                    var result = UseItem(this._gameState.Party.Members.First(), item, this._gameState.Party);
                                    if (string.IsNullOrEmpty(result))
                                    {
                                        done();
                                    }
                                    else
                                    {
                                        new TalkWindow(this._ui).Show(result, done);
                                    }
                                }
                                else
                                {
                                    var selectHero = new SelectHeroWindow(this._ui);
                                    selectHero.Show(this._gameState.Party.Members.Where(hero => hero.CanUseItem(item)),
                                        hero =>
                                        {
                                            if (hero == null)
                                            {
                                                done();
                                                return;
                                            }
                                        
                                            var result = UseItem(hero, item, this._gameState.Party);
                                            if (string.IsNullOrEmpty(result))
                                            {
                                                done();
                                            }
                                            else
                                            {
                                                new TalkWindow(this._ui).Show(result, done);
                                            }
                                        });
                                }
                                break;
                            case "Drop":
                            {
                                if (item.IsEquipped)
                                {
                                    item.UnEquip(this._gameState.Party.Members);
                                }

                                this._gameState.Party.Items.Remove(item);
                                new TalkWindow(this._ui).Show($"You dropped {item.Name}", done);
                                break;
                            }
                            default:
                                done();
                                break;
                        }
                    });
                });
            }
        }
        
        private static string UseItem(IFighter hero, ItemInstance item, Party party)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (item.Type)
            {
                case ItemType.OneUse:
                    hero.Use(item);
                    party.Items.Remove(item);
                    return  $"{hero.Name} used {item.Name}";
                case ItemType.Armor:
                case ItemType.Weapon:
                    var oldItem = party.Items.FirstOrDefault(i => i.Id == hero.GetEquipmentId(item.Type));
                    oldItem?.UnEquip(party.Members);
                    item.UnEquip(party.Members);
                    hero.Equip(item);
                    return  $"{hero.Name} equipped {item.Name}";
                default:
                    return $"{hero.Name} is unable to use {item.Name}";
            }
        }
    }
}