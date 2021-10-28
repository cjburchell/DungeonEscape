using System;
using System.Collections.Generic;
using DungeonEscape.Scenes.Map.Components;
using DungeonEscape.Scenes.Map.Components.Objects;
using DungeonEscape.Scenes.Map.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using Nez.Tiled;
using Nez.UI;

namespace DungeonEscape.Scenes
{
    using System.Linq;
    using Common.Components.UI;
    using Microsoft.Xna.Framework.Input;

    public class MapScene : Nez.Scene
    {
        private const int ScreenSpaceRenderLayer = 999;
        public const int ScreenWidth = 16;
        public const int ScreenHeight = 15;
        private readonly int mapId;
        private readonly Point? start;
        private readonly IGame gameState;
        private Label debugText;
        private readonly List<Monster> randomMonsters = new List<Monster>();
        private VirtualButton showCommandWindowInput;
        private UISystem ui;

        private MapScene(IGame game, int mapId, Point? start = null)
        {
            this.mapId = mapId;
            this.start = start;
            this.gameState = game;
        }

        public static Point ToMapGrid(Vector2 pos, TmxMap map)
        {
            return new Point {X = (int) (pos.X / map.TileWidth), Y = (int) (pos.Y / map.TileHeight)};
        }

        public static Vector2 ToRealLocation(Point point, TmxMap map)
        {
            return new Vector2(point.X * map.TileWidth + map.TileWidth / 2,
                point.Y * map.TileHeight + map.TileHeight / 2);
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

            Console.WriteLine($"Loading Map {this.mapId}");
            var map = this.gameState.GetMap(this.mapId);
            this.SetDesignResolution(ScreenWidth * map.TileWidth, ScreenHeight * map.TileHeight,
                SceneResolutionPolicy.ShowAll);


            var randomMonsterTileSet = DungeonEscapeGame.LoadTileSet($"Content/monsters{this.mapId}.tsx");
            if (randomMonsterTileSet != null)
            {
                foreach (var (_, tile) in randomMonsterTileSet.Tiles)
                {
                    this.randomMonsters.Add(new Monster(tile, this.gameState.Spells));
                }
            }

            this.gameState.CurrentMapId = this.mapId;

            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            this.ui = new UISystem(this.CreateEntity("ui-canvas").AddComponent(new UICanvas()));
            this.debugText = ui.Canvas.Stage.AddElement(new Label(""));
            this.debugText.SetFontScale(2).SetPosition(10, 20);

            var tiledEntity = this.CreateEntity("map");
            var tiledMapRenderer = tiledEntity.AddComponent(new TiledMapRenderer(map,
                this.gameState.Party.HasShip && this.mapId == 0 ? new[] {"wall"} : new[] {"wall", "water"}));
            tiledMapRenderer.RenderLayer = 50;
            tiledMapRenderer.SetLayersToRender("wall", "water", "floor");
            map.GetObjectGroup("objects").Visible = false;

            var objects = map.GetObjectGroup("items");
            foreach (var item in objects.Objects)
            {
                var itemEntity = this.CreateEntity(item.Name);
                itemEntity.AddComponent(MapObject.Create(item, map.TileHeight, map.TileWidth,
                    map.GetTilesetTile(item.Tile.Gid), ui, this.gameState));
            }

            var graph = CreateGraph(map);
            var sprites = map.GetObjectGroup("sprites");
            foreach (var item in sprites.Objects)
            {
                var spriteEntity = this.CreateEntity(item.Name);
                spriteEntity.AddComponent(Sprite.Create(item, map, ui, this.gameState, graph));
            }

            var topLeft = new Vector2(0, 0);
            var bottomRight = new Vector2(map.TileWidth * (map.Width),
                map.TileWidth * (map.Height));
            tiledEntity.AddComponent(new CameraBounds(topLeft, bottomRight));

            var spawn = new Vector2();
            if (this.start == null)
            {
                var spawnObject = map.GetObjectGroup("objects").Objects["spawn"];
                spawn.X = spawnObject.X + (map.TileWidth / 2.0f);
                spawn.Y = spawnObject.Y - (map.TileHeight / 2.0f);
            }
            else
            {
                spawn = ToRealLocation(this.start.Value, map);
            }

            var playerEntity = this.CreateEntity("player", spawn);


            playerEntity.AddComponent(new PlayerComponent(this.gameState, map, this.debugText, this.randomMonsters, ui));

            this.Camera.Entity.AddComponent(new FollowCamera(playerEntity, FollowCamera.CameraStyle.CameraWindow));
            
            this.showCommandWindowInput = new VirtualButton();
            this.showCommandWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
            this.showCommandWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.X));
        }
        
        [Nez.Console.Command("map", "switches to map")]
        public static void SetMap(int mapId = 0, Point? point = null)
        {
            if (!(Core.Instance is IGame game))
            {
                return;
            }

            game.IsPaused = true;
            var map = new MapScene(game, mapId, point);
            var transition = new FadeTransition(() =>
            {
                map.Initialize();
                return map;
            });
            transition.OnTransitionCompleted += () => { game.IsPaused = false; };

            Core.StartSceneTransition(transition);
        }

        public override void Update()
        {
            base.Update();
            if (!this.gameState.IsPaused && this.showCommandWindowInput.IsReleased)
            {
                var menuItems = new List<string> {"Status", "Stats"};
                if (this.gameState.Party.Members.Count(member => member.Spells.Count != 0) != 0)
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
                
                var commandMenu = this.ui.Canvas.AddComponent(new CommandMenu(this.ui));
                commandMenu.Show(menuItems, result =>
                {
                    switch (result)
                    {
                        case "Status":
                            this.ShowStatus(unPause);
                            break;
                        case "Spells":
                            this.ShowSpell(unPause);
                            break;
                        case "Stats":
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
        }

        private void ShowStatus(Action done)
        {
            var statusWindow = this.ui.Canvas.AddComponent(new PartyStatusWindow(this.ui));
            statusWindow.Show(this.gameState.Party, done);
        }

        private void ShowHeroStatus(Action done)
        {
            if (this.gameState.Party.Members.Count == 1)
            {
                var statusWindow = this.ui.Canvas.AddComponent(new HeroStatusWindow(this.ui));
                statusWindow.Show(this.gameState.Party.Members.First(), done);
            }
            else
            {
                var selectWindow = this.ui.Canvas.AddComponent(new SelectHeroWindow(this.ui));
                selectWindow.Show( this.gameState.Party.Members, hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    var statusWindow = this.ui.Canvas.AddComponent(new HeroStatusWindow(this.ui));
                    statusWindow.Show(hero, done);
                });
            }
        }

        private void CastSpell(Hero caster, Spell spell, Action done)
        {
            
            if (caster.Magic < spell.Cost)
            {
                var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                talkWindow.Show($"{caster.Name}: I do not have enough magic to cast {spell.Name}.", done);
                return;
            }

            if (spell.Type == SpellType.Heal)
            {
                if (this.gameState.Party.Members.Count == 1)
                {
                    var result = Spell.CastHeal(this.gameState.Party.Members.First(), caster, spell);
                    var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                    talkWindow.Show(result, done);
                }
                else
                {
                    var selectWindow = this.ui.Canvas.AddComponent(new SelectHeroWindow(this.ui));
                    selectWindow.Show(this.gameState.Party.Members.Where(item => item.Spells.Count != 0), hero =>
                    {
                        if (hero == null)
                        {
                            done();
                        }
                        else
                        {
                            var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                            talkWindow.Show(Spell.CastHeal(hero, caster, spell), done);
                        }
                    });
                }
            }
            else if (spell.Type == SpellType.Outside)
            {
                var result = Spell.CastOutside(caster, spell, this.gameState);
                if (string.IsNullOrEmpty(result))
                {
                    done();
                }
                else
                {
                    var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                    talkWindow.Show(result, done);
                }
            }
            else
            {
                var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                talkWindow.Show($"{caster.Name} casts {spell.Name} but it did not work", done);
            }
        }
        
        private void ShowSpell(Action done)
        {
            if (this.gameState.Party.Members.Count == 1)
            {
                var spellWindow = this.ui.Canvas.AddComponent(new SpellWindow(this.ui));
                spellWindow.Show(this.gameState.Party.Members.First().Spells.Where(item => item.IsNonEncounterSpell), spell=>
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
                var selectWindow = this.ui.Canvas.AddComponent(new SelectHeroWindow(this.ui));
                selectWindow.Show(this.gameState.Party.Members.Where(item => item.Spells.Count != 0), hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    
                    var spellWindow = this.ui.Canvas.AddComponent(new SpellWindow(this.ui));
                    spellWindow.Show(hero.Spells.Where(item => item.IsNonEncounterSpell), spell =>
                    {
                        if (spell == null)
                        {
                            done();
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
                var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                talkWindow.Show("The party has no items", done);
            }
            else
            {
                var inventoryWindow = this.ui.Canvas.AddComponent(new InventoryWindow(this.ui));
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
                        menuItems.Add("Use");
                    }
                    menuItems.Add("Drop");
                    
                    var selectWindow = this.ui.Canvas.AddComponent(new SelectWindow<string>(this.ui, "Select", new Point(150, 30)));
                    selectWindow.Show(menuItems, action =>
                    {
                        switch (action)
                        {
                            case "Use":
                                var selectHero = this.ui.Canvas.AddComponent(new SelectHeroWindow(this.ui));
                                selectHero.Show(this.gameState.Party.Members.Where(hero => hero.CanUseItem(item)),
                                    hero =>
                                    {
                                        var result = this.UseItem(hero, item, this.gameState.Party);
                                        if (string.IsNullOrEmpty(result))
                                        {
                                            done();
                                        }
                                        else
                                        {
                                            var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                                            talkWindow.Show(result, done);
                                        }
                                    });
                                break;
                            case "Drop":
                            {
                                if (item.IsEquipped)
                                {
                                    item.Unequip();
                                }

                                this.gameState.Party.Items.Remove(item);
                                var talkWindow = this.ui.Canvas.AddComponent(new TalkWindow(this.ui));
                                talkWindow.Show($"You dropped {item.Name}", done);
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
        
        private string UseItem(Hero hero, ItemInstance item, Party party)
        {
            switch (item.Type)
            {
                case ItemType.OneUse:
                    item.Use(hero);
                    party.Items.Remove(item);
                    return  $"{hero.Name} used {item.Name}";
                case ItemType.Armor:
                case ItemType.Weapon:
                case ItemType.Shield:
                    item.Equip(hero);
                    return  $"{hero.Name} equipped {item.Name}";
                default:
                    return $"{hero.Name} is unable to use {item.Name}";
            }
        }
    }
}