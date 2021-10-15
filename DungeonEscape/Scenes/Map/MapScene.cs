using System;
using DungeonEscape.Scenes.Map.Components;
using DungeonEscape.Scenes.Map.Components.Objects;
using DungeonEscape.Scenes.Map.Components.UI;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes
{
    public class MapScene : Nez.Scene
    {
        private const int ScreenSpaceRenderLayer = 999;
        public const int ScreenWidth = 16;
        public const int ScreenHeight = 15;
        private readonly int mapId;
        private readonly Vector2? start;
        private readonly IGame gameState;

        private MapScene(IGame game, int mapId, Vector2? start = null)
        {
            this.mapId = mapId;
            this.start = start;
            this.gameState = game;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            Console.WriteLine($"Loading Map {this.mapId}");
            var map = this.gameState.GetMap(this.mapId);
            this.SetDesignResolution(ScreenWidth * map.TileWidth, ScreenHeight * map.TileHeight, SceneResolutionPolicy.ShowAll);

            this.gameState.CurrentMapId = this.mapId;
            
            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            
            
            var spawn = new Vector2();
            if (this.start == null)
            {
                var spawnObject = map.GetObjectGroup("objects").Objects["spawn"];
                spawn.X = spawnObject.X + (map.TileHeight / 2.0f);
                spawn.Y = spawnObject.Y - (map.TileWidth / 2.0f);
            }
            else
            {
                spawn = this.start.Value;
            }
            
            var playerEntity = this.CreateEntity("player", spawn);

            this.Camera.Entity.AddComponent(new FollowCamera(playerEntity));

            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);
            canvas.AddComponent(new CommandMenu(canvas, this.gameState));
            var talkWindow = canvas.AddComponent(new TalkWindow(canvas, this.gameState));
            
            var tiledEntity = this.CreateEntity("map");
            var tiledMapRenderer =  tiledEntity.AddComponent(new TiledMapRenderer(map, new[] {"wall", "water"}));
            tiledMapRenderer.RenderLayer = 50;
            tiledMapRenderer.SetLayersToRender("wall", "water", "floor");
            map.GetObjectGroup("objects").Visible = false;

            var objects = map.GetObjectGroup("items");
            objects.Visible = true;
            
            foreach (var item in objects.Objects)
            {
                var itemEntity = this.CreateEntity(item.Name);
                itemEntity.AddComponent(MapObject.Create(item, map.TileHeight, map.TileWidth, map.GetTilesetTile(item.Tile.Gid), talkWindow));
            }
            
            var sprites = map.GetObjectGroup("sprites");
            sprites.Visible = true;
            foreach (var item in sprites.Objects)
            {
                var spriteEntity = this.CreateEntity(item.Name);
                spriteEntity.AddComponent(Sprite.Create(item, map, talkWindow, this.gameState));
            }
            
            var topLeft = new Vector2(map.TileWidth, map.TileWidth);
            var bottomRight = new Vector2(map.TileWidth * (map.Width - 1),
                map.TileWidth * (map.Height - 1));
            tiledEntity.AddComponent(new CameraBounds(topLeft, bottomRight));
            
            playerEntity.AddComponent(new Player(this.gameState));
        }

        
        [Nez.Console.Command("map", "switches to map")]
        public static void SetMap(int mapId = 0, Vector2? point = null)
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
            transition.OnTransitionCompleted += () => {
                Console.WriteLine("FinishedTransition");
                game.IsPaused = false;
            };
            
            Core.StartSceneTransition(transition);
        }
    }
}