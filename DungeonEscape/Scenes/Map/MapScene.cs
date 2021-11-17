namespace Redpoint.DungeonEscape.Scenes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Common.Components.UI;
    using Map.Components;
    using Map.Components.Objects;
    using Map.Components.UI;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Newtonsoft.Json;
    using Nez;
    using Nez.AI.Pathfinding;
    using Nez.Console;
    using Nez.Tiled;
    using Nez.UI;
    using State;

    public class MapScene : Scene
    {
        private const int ScreenSpaceRenderLayer = 999;
        
        public const int DefaultTileSize = 32;
        private const int ScreenTileWidth = 32;
        private const int ScreenTileHeight = 18;
        public const int ScreenWidth = ScreenTileWidth * DefaultTileSize;
        public const int ScreenHeight = ScreenTileHeight * DefaultTileSize;
        public const SceneResolutionPolicy SceneResolution = SceneResolutionPolicy.ShowAll;
        
        private readonly int mapId;
        private readonly Point? start;
        private readonly IGame gameState;
        private Label debugText;
        private List<RandomMonster> randomMonsters = new List<RandomMonster>();
        private VirtualButton showCommandWindowInput;
        private VirtualButton showExitWindowInput;
        private UISystem ui;

        public MapScene(IGame game, int mapId, Point? start = null)
        {
            this.mapId = mapId;
            this.start = start;
            this.gameState = game;
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
            
            var map = this.gameState.GetMap(this.mapId);
            this.SetDesignResolution(ScreenTileWidth * map.TileWidth, ScreenTileHeight * map.TileHeight,
                SceneResolution);


            this.randomMonsters = this.LoadRandomMonsters();

            this.gameState.Party.CurrentMapId = this.mapId;

            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            this.ui = new UISystem(this.CreateEntity("ui-canvas").AddComponent(new UICanvas()));
            this.ui.Canvas.SetRenderLayer(999);
            this.ui.Canvas.Stage.GamepadActionButton = null;
            
            this.debugText = this.ui.Canvas.Stage.AddElement(new Label("", BasicWindow.Skin));
            this.debugText.SetPosition(10, 20);

            var tiledEntity = this.CreateEntity("map");
            var tiledMapRenderer = tiledEntity.AddComponent(new TiledMapRenderer(map,
                this.gameState.Party.HasShip && this.mapId == 0 ? new[] {"wall"} : new[] {"wall", "water"}));
            tiledMapRenderer.RenderLayer = 50;
            tiledMapRenderer.SetLayersToRender("wall", "wall2", "water", "floor", "floor2");
            map.GetObjectGroup("objects").Visible = false;

            var mapState = this.gameState.MapStates.FirstOrDefault(item => item.Id == this.mapId);
            if (mapState == null)
            {
                mapState = new MapState {Id = this.mapId};
                this.gameState.MapStates.Add(mapState);
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
                var itemEntity = this.CreateEntity(item.Name);
                itemEntity.AddComponent(MapObject.Create(item, state,
                    map, this.ui, this.gameState));
            }

            var graph = CreateGraph(map);
            var sprites = map.GetObjectGroup("sprites");
            foreach (var item in sprites.Objects)
            {
                var state = mapState.Sprites.FirstOrDefault(i => item.Id == i.Id);
                if (state == null)
                {
                    state = new SpriteState {Id = item.Id};
                    mapState.Sprites.Add(state);
                }
                var spriteEntity = this.CreateEntity(item.Name);
                spriteEntity.AddComponent(Sprite.Create(item, state, map, this.ui, this.gameState, graph));
            }

            var topLeft = new Vector2(0, 0);
            var bottomRight = new Vector2(map.TileWidth * map.Width,
                map.TileWidth * map.Height);
            tiledEntity.AddComponent(new CameraBounds(topLeft, bottomRight));

            var spawn = new Vector2();
            if (this.start == null)
            {
                var spawnObject = map.GetObjectGroup("objects").Objects["spawn"];
                spawn.X = spawnObject.X + map.TileWidth / 2.0f;
                spawn.Y = spawnObject.Y - map.TileHeight / 2.0f;
            }
            else
            {
                spawn = ToRealLocation(this.start.Value, map);
            }

            var playerEntity = this.CreateEntity("player", spawn);


            playerEntity.AddComponent(new PlayerComponent(this.gameState, map, this.debugText, this.randomMonsters, this.ui));

            this.Camera.Entity.AddComponent(new FollowCamera(playerEntity, FollowCamera.CameraStyle.CameraWindow));
            
            this.showCommandWindowInput = new VirtualButton();
            this.showCommandWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
            this.showCommandWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.X));
            
            this.showExitWindowInput = new VirtualButton();
            this.showExitWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Escape));
            this.showExitWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.Y));
        }

        private List<RandomMonster> LoadRandomMonsters()
        {
            var fileName = $"Content/monsters{this.mapId}.json";
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
                monster.Data = this.gameState.Monsters.FirstOrDefault(Item => Item.Id == monster.Id);
                if (monster.Data != null)
                {
                    list.Add(monster);
                }
            }

            return list;

        }

        [Command("map", "switches to map")]
        // ReSharper disable once UnusedMember.Global
        public static void SetMap(int? mapId = null, Point? point = null)
        {
            var game = Core.Instance as IGame;
            game?.SetMap(mapId, point);
        }

        public override void Update()
        {
            this.ui.Input.HandledHide = false;
            if (this.gameState.IsPaused)
            {
                base.Update();
                return;
            }
            
            if (this.showCommandWindowInput.IsReleased)
            {
                var menuItems = new List<string> {"Status"};
                if (this.gameState.Party.Members.Count(member => member.GetSpells(this.gameState.Spells).Count() != 0) != 0)
                {
                    menuItems.Add("Spells");
                }

                if (this.gameState.Party.Items.Count != 0)
                {
                    menuItems.Add("Items");
                }
                
                this.gameState.IsPaused = true;
                void unPause()
                {
                    this.gameState.IsPaused = false;
                }
                
                var commandMenu = new CommandMenu(this.ui);
                commandMenu.Show(menuItems, result =>
                {
                    switch (result)
                    {
                        case "Spells":
                            this.ShowSpell(unPause);
                            break;
                        case "Status":
                            this.ShowHeroStatus(unPause);
                            break;
                        case "Items":
                            this.ShowItems(unPause);
                            break;
                        default:
                            unPause();
                            break;
                    }   
                });
            }
            else if(this.showExitWindowInput.IsReleased)
            {
                this.ui.Input.HandledHide = true;
                this.gameState.IsPaused = true;
                var commandMenu = new SelectWindow<string>(this.ui, "menu", new Point(20,20), 200);
                commandMenu.Show(new []{"Main Menu", "Load Quest", "Quit"}, result =>
                {
                    switch (result)
                    {
                        case "Main Menu":
                            this.gameState.ShowMainMenu();
                            break;
                        case "Load Quest":
                            this.gameState.ShowLoadQuest();
                            break;
                        case "Quit":
                            Core.Exit();
                            break;
                        default:
                            this.gameState.IsPaused = false;
                            break;
                    }
                });
            }
            
            base.Update();
        }

        private void ShowHeroStatus(Action done)
        {
            if (this.gameState.Party.Members.Count == 1)
            {
                var statusWindow = new HeroStatusWindow(this.ui);
                statusWindow.Show(this.gameState.Party.Members.First(), done);
            }
            else
            {
                var selectWindow = new SelectHeroWindow(this.ui);
                selectWindow.Show( this.gameState.Party.Members, hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    var statusWindow = new HeroStatusWindow(this.ui);
                    statusWindow.Show(hero, done);
                });
            }
        }

        private void CastSpell(Fighter caster, Spell spell, Action done)
        {
            if (spell.Targets == Target.Group)
            {
                var result = spell.Cast(this.gameState.Party.Members, caster, this.gameState);
                if (string.IsNullOrEmpty(result))
                {
                    done();
                    return;
                }
                
                new TalkWindow(this.ui).Show(result, done);
                return;
            }

            Func<Hero,bool> filter = hero => !hero.IsDead;
            if (spell.Type == SpellType.Revive)
            {
                filter = hero => hero.IsDead;
            }
                
            if(this.gameState.Party.Members.Count(filter) == 1 && spell.Type != SpellType.Revive)
            {
                var result = spell.Cast(this.gameState.Party.Members.Where(filter), caster, this.gameState);
                new TalkWindow(this.ui).Show(result, done);
                return;
            }
                
            new SelectHeroWindow(this.ui).Show(this.gameState.Party.Members.Where(filter), target =>
            {
                if (target == null)
                {
                    done();
                    return;
                }

                new TalkWindow(this.ui).Show(spell.Cast(new[] {target}, caster, this.gameState), done);
            });
        }

        private void ShowSpell(Action done)
        {
            if (this.gameState.Party.Members.Count == 1)
            {
                var spellWindow = new SpellWindow(this.ui);
                spellWindow.Show(this.gameState.Party.Members.First().GetSpells(this.gameState.Spells).Where(item => item.IsNonEncounterSpell), spell=>
                {
                    if (spell == null)
                    {
                        done();
                        return;
                    }

                    this.CastSpell(this.gameState.Party.Members.First(), spell, done);

                });
            }
            else
            {
                var selectWindow = new SelectHeroWindow(this.ui);
                selectWindow.Show(this.gameState.Party.Members.Where(item => item.GetSpells(this.gameState.Spells).Count() != 0), hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    
                    var spellWindow = new SpellWindow(this.ui);
                    spellWindow.Show(hero.GetSpells(this.gameState.Spells).Where(item => item.IsNonEncounterSpell), spell =>
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
            if (this.gameState.Party.Items.Count == 0)
            {
                new TalkWindow(this.ui).Show("The party has no items", done);
            }
            else
            {
                var inventoryWindow = new InventoryWindow(this.ui);
                inventoryWindow.Show(this.gameState.Party.Items, item =>
                {
                    if (item == null)
                    {
                        done();
                        return;
                    }
                    
                    var menuItems = new List<string>();
                    if (this.gameState.Party.Members.Count(hero => hero.CanUseItem(item)) != 0)
                    {
                        menuItems.Add(item.IsEquippable ? "Equip" : "Use");
                    }
                    menuItems.Add("Drop");
                    
                    var selectWindow = new SelectWindow<string>(this.ui, "Select", new Point(20, 20));
                    selectWindow.Show(menuItems, action =>
                    {
                        switch (action)
                        {
                            case "Equip":
                            case "Use":
                                if (this.gameState.Party.Members.Count == 1)
                                {
                                    var result = UseItem(this.gameState.Party.Members.First(), item, this.gameState.Party);
                                    if (string.IsNullOrEmpty(result))
                                    {
                                        done();
                                    }
                                    else
                                    {
                                        new TalkWindow(this.ui).Show(result, done);
                                    }
                                }
                                else
                                {
                                    var selectHero = new SelectHeroWindow(this.ui);
                                    selectHero.Show(this.gameState.Party.Members.Where(hero => hero.CanUseItem(item)),
                                        hero =>
                                        {
                                            if (hero == null)
                                            {
                                                done();
                                                return;
                                            }
                                        
                                            var result = UseItem(hero, item, this.gameState.Party);
                                            if (string.IsNullOrEmpty(result))
                                            {
                                                done();
                                            }
                                            else
                                            {
                                                new TalkWindow(this.ui).Show(result, done);
                                            }
                                        });
                                }
                                break;
                            case "Drop":
                            {
                                if (item.IsEquipped)
                                {
                                    item.UnEquip(this.gameState.Party.Members);
                                }

                                this.gameState.Party.Items.Remove(item);
                                new TalkWindow(this.ui).Show($"You dropped {item.Name}", done);
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
        
        private static string UseItem(Hero hero, ItemInstance item, Party party)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (item.Type)
            {
                case ItemType.OneUse:
                    item.Use(hero);
                    party.Items.Remove(item);
                    return  $"{hero.Name} used {item.Name}";
                case ItemType.Armor:
                case ItemType.Weapon:
                case ItemType.Shield:
                    item.Equip(hero, party.Items, party.Members);
                    return  $"{hero.Name} equipped {item.Name}";
                default:
                    return $"{hero.Name} is unable to use {item.Name}";
            }
        }
    }
}