namespace Redpoint.DungeonEscape.Scenes.Map
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Common.Components.UI;
    using Components;
    using Components.Objects;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using Nez;
    using Nez.AI.Pathfinding;
    using Nez.Console;
    using Nez.Tiled;
    using Nez.UI;
    using State;
    using Game = Game;

    public class MapScene : Scene
    {
        private const int ScreenSpaceRenderLayer = 999;
        
        public const int DefaultTileSize = 32;
        private const int ScreenTileWidth = 32;
        private const int ScreenTileHeight = 18;
        public const int ScreenWidth = ScreenTileWidth * DefaultTileSize;
        public const int ScreenHeight = ScreenTileHeight * DefaultTileSize;
        public const SceneResolutionPolicy SceneResolution = SceneResolutionPolicy.ShowAll;
        
        private readonly string _mapId;
        private readonly Vector2? _start;
        private readonly IGame _gameState;
        private Label _debugText;
        private List<RandomMonster> _randomMonsters = new();
        private UiSystem _ui;
        private readonly string _spawnId;
        private PlayerComponent _player;
        private bool _isOverWorld;
        private readonly MapInput _input = new();

        public MapScene(IGame game, string mapId, string spawnId, Vector2? start = null)
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
            var water2 = map.GetLayer<TmxLayer>("water2");

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
                if (!bool.Parse(item.Properties["Collideable"]) && item.Class != SpriteType.Warp.ToString())
                {
                    continue;
                }

                var x = (int) ((item.X + (int) (map.TileWidth / 2.0)) / map.TileWidth);
                var y = (int) ((item.Y - (int) (map.TileHeight / 2.0)) / map.TileHeight);
                itemLayer.SetTile(new TmxLayerTile(map, 1, x, y));
            }

            return new AstarGridGraph(new[] {wall, water, water2, itemLayer});
        }

        public override void Initialize()
        {
            base.Initialize();
            Debug.Log($"loading map {this._mapId}");
            var map = this._gameState.GetMap(this._mapId);
            this.SetDesignResolution(ScreenTileWidth * map.TileWidth, ScreenTileHeight * map.TileHeight,
                SceneResolution);
            
            this._isOverWorld = map.Properties != null && map.Properties.ContainsKey("overworld") && bool.Parse(map.Properties["overworld"]);

            var songPath = map.Properties != null && map.Properties.ContainsKey("song")?map.Properties["song"]:@"not-in-vain";
            this._gameState.Sounds.PlayMusic(new []{songPath});
            
            this._randomMonsters = this.LoadRandomMonsters();

            this._gameState.Party.CurrentMapId = this._mapId;
            this._gameState.Party.CurrentMapIsOverWorld = _isOverWorld;

            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            this._ui = new UiSystem(this.CreateEntity("ui-canvas").AddComponent(new UICanvas()), this._gameState.Sounds);
            this._ui.Canvas.SetRenderLayer(999);
            this._ui.Canvas.Stage.GamepadActionButton = null;
            
            this._debugText = this._ui.Canvas.Stage.AddElement(new Label("", BasicWindow.Skin));
            this._debugText.SetPosition(10, ScreenTileWidth * map.TileWidth/2.0f);
            this._debugText.SetIsVisible(this._gameState.Settings.MapDebugInfo);

            {
                var tiledEntity = this.CreateEntity("map");
                var tiledMapRenderer = tiledEntity.AddComponent(new TiledMapRenderer(map,
                    this._gameState.Party.HasShip && this._gameState.Party.CurrentMapIsOverWorld ? new[] {"wall", "water2"} : new[] {"wall", "water2", "water"}));
                tiledMapRenderer.RenderLayer = map.Height*map.TileHeight + 20;
                tiledMapRenderer.SetLayersToRender("wall", "wall2", "water", "water2", "floor", "floor2", "floor3");
                
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
                
                var itemEntity = this.CreateEntity($"item-{item.Id}");
                itemEntity.AddComponent(MapObject.Create(item, state,
                    map, this._ui, this._gameState));
            }

            var graph = CreateGraph(map);
            var sprites = map.GetObjectGroup("sprites");
            var tileSet = Game.LoadTileSet("Content/items2.tsx");
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
                        spriteItem.Setup(tileSet, _gameState.Skills);
                    }
                }
                

                if (!state.IsActive)
                {
                    continue;
                }

                var spriteEntity = this.CreateEntity($"sprite-{item.Id}");
                spriteEntity.AddComponent(Sprite.Create(item, state, map, this._ui, this._gameState, graph));
            }
            
            var spawn = new Vector2();
            if (this._start == null)
            {
                if (!string.IsNullOrEmpty(this._spawnId))
                {
                    if (map.GetObjectGroup("objects") != null &&
                        map.GetObjectGroup("objects").Objects.TryGetValue(this._spawnId, out var spawnObject))
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
            _player = null;
            var renderOffset = (map.Height * map.TileHeight);
            var order = 0;
            foreach (var hero in this._gameState.Party.ActiveMembers.OrderBy(i => i.IsDead))
            {
                if (first)
                {
                    var playerEntity = this.CreateEntity($"hero_{order}", spawn);
                    _player = playerEntity.AddComponent(new PlayerComponent(this._gameState, map, this._debugText, this._randomMonsters, this._ui, renderOffset)).GetComponent<PlayerComponent>();
                    this.Camera.Entity.AddComponent(new FollowCamera(playerEntity, FollowCamera.CameraStyle.CameraWindow));
                    first = false;
                    lastEntity = playerEntity;
                }
                else
                {
                    var followerEntity = this.CreateEntity($"hero_{order}", spawn);
                    followerEntity.AddComponent(new Follower( order, lastEntity, _player, this._gameState, renderOffset));
                    lastEntity = followerEntity;
                }

                order++;
            }
        }
        
        public static BiomeInfo GetCurrentBiome(TmxMap map, Vector2 pos)
        {
            var isOverWorld = map.Properties != null && map.Properties.ContainsKey("overworld") && bool.Parse(map.Properties["overworld"]);
            if (!isOverWorld)
            {
                var biome = map.Properties != null && map.Properties.ContainsKey("biome")? Enum.Parse<Biome>(map.Properties["biome"]):Biome.None;
                return new BiomeInfo {Type = biome};
            }

            var (x, y) = MapScene.ToMapGrid(pos, map);
            var tile = map.GetLayer<TmxLayer>("biomes")?.GetTile(x, y);
            if (tile == null)
            {
                return new BiomeInfo {Type = Biome.None};
            }

            return new BiomeInfo()
            {
                Type = Enum.Parse<Biome>(tile.TilesetTile.Class),
                MaxMonsterLevel = tile.TilesetTile.Properties.ContainsKey("MaxMonsterLevel")
                    ? int.Parse(tile.TilesetTile.Properties["MaxMonsterLevel"])
                    : 0,
                MinMonsterLevel = tile.TilesetTile.Properties.ContainsKey("MinMonsterLevel")
                    ? int.Parse(tile.TilesetTile.Properties["MinMonsterLevel"])
                    : 0,
            };
        }

        private List<RandomMonster> LoadRandomMonsters()
        {
            if (_isOverWorld)
            {
                return this._gameState.Monsters.Where(i => i.Biomes != null && i.Biomes.Any()).Select(monster => new RandomMonster { Data = monster, Rarity = monster.Rarity, Name = monster.Name, IsOverworld = true}).ToList();
            }
            
            var fileName = $"Content/data/{this._mapId}_monsters.json";
            if (!File.Exists(fileName))
            {
                Debug.Warn($"{fileName} not found");
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
                monster.Data = this._gameState.Monsters.FirstOrDefault(item => item.Name == monster.Name);
                if (monster.Data != null)
                {
                    list.Add(monster);
                }
            }

            return list;
        }

        [Command("map", "switches to map")]
        // ReSharper disable once UnusedMember.Global
        public static void SetMap(string mapId = null)
        {
            var game = Core.Instance as IGame;
            game?.SetMap(mapId);
        }
        
        [Command("fight", "fights a monster")]
        // ReSharper disable once UnusedMember.Global
        public static void StartFight(string monsterId = "")
        {
            var game = Core.Instance as IGame;
            var monster = game?.Monsters.FirstOrDefault(m => m.Name == monsterId);
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
                member.Setup(game, level, false);
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
            
            _input.HandleInput(_ui, _player, _gameState);
            
            base.Update();
        }
        
    }
}