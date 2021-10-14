using System;
using DungeonEscape.Components;
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
        private Components.Player player;

        private MapScene(IGame game, int mapId, Vector2? start = null)
        {
            this.mapId = mapId;
            this.start = start;
            this.gameState = game;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            var map = this.gameState.GetMap(this.mapId);
            this.SetDesignResolution(ScreenWidth * map.TileWidth, ScreenHeight * map.TileHeight, SceneResolutionPolicy.ShowAll);

            this.gameState.CurrentMapId = this.mapId;
            Console.WriteLine($"Loading Map {this.mapId}");
            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            var tiledEntity = this.CreateEntity("map");
            var tiledMapRenderer =  tiledEntity.AddComponent(new TiledMapRenderer(map, new[] {"wall", "water"}));
            tiledMapRenderer.RenderLayer = 10;
            tiledMapRenderer.SetLayersToRender("wall", "water", "floor");
            map.GetObjectGroup("objects").Visible = false;

            var objects = map.GetObjectGroup("items");
            objects.Visible = true;
            
            foreach (var item in objects.Objects)
            {
                var itemEntity = this.CreateEntity(item.Name);
                itemEntity.AddComponent(MapObject.Create(item, map.TileHeight, map.TileWidth, map.GetTilesetTile(item.Tile.Gid)));
            }
            
            var sprites = map.GetObjectGroup("sprites");
            sprites.Visible = true;
            foreach (var item in sprites.Objects)
            {
                var spriteEntity = this.CreateEntity(item.Name);
                spriteEntity.AddComponent(Sprite.Create(item, map));
            }
            
            var topLeft = new Vector2(map.TileWidth, map.TileWidth);
            var bottomRight = new Vector2(map.TileWidth * (map.Width - 1),
                map.TileWidth * (map.Height - 1));
            tiledEntity.AddComponent(new CameraBounds(topLeft, bottomRight));

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

            Console.WriteLine();
            var playerEntity = this.CreateEntity("player", spawn);
            this.player = playerEntity.AddComponent(new Components.Player(this.gameState));
            
            this.Camera.Entity.AddComponent(new FollowCamera(playerEntity));
            
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);

            canvas.AddComponent(new CommandMenu(canvas, this.player));
        }

        
        [Nez.Console.Command("map", "switches to map")]
        public static void SetMap(int mapId = 0, Vector2? point = null)
        {
            var map = new MapScene(Core.Instance as IGame, mapId, point);
            var transition = new FadeTransition(() =>
            {
                map.Initialize();
                return map;
            });
            transition.OnTransitionCompleted += map.FinishedTransition;
            
            Core.StartSceneTransition(transition);
        }

        private void FinishedTransition()
        {
            Console.WriteLine("FinishedTransition");
            this.player.IsControllable = true;
        }
    }
}