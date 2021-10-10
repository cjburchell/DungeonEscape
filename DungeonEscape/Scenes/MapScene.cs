
using System;
using DungeonEscape.Components;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scene
{
    public class MapScene : Nez.Scene
    {
        private const int ScreenSpaceRenderLayer = 999;
        public const int screenWidth = 16;
        public const int screenHeight = 15;
        private readonly int mapId;
        private readonly Vector2? start;
        private readonly IGame gameState;
        private PlayerComponent playerComponent;

        public MapScene(IGame game, int mapId, Vector2? start = null)
        {
            this.mapId = mapId;
            this.start = start;
            this.gameState = game;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            var map = this.gameState.GetMap(this.mapId);
            this.SetDesignResolution(screenWidth * map.TileWidth, screenHeight * map.TileHeight, SceneResolutionPolicy.ShowAll);

            this.gameState.CurrentMapId = this.mapId;
            Console.WriteLine($"Loading Map {this.mapId}");
            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            var tiledEntity = this.CreateEntity("map");
            var tiledMapRenderer =  tiledEntity.AddComponent(new TiledMapRenderer(map, new[] {"wall", "water"}));
            tiledMapRenderer.RenderLayer = 10;
            tiledMapRenderer.SetLayersToRender(new[]{ "wall", "water", "floor", "items", "sprites"});
            map.GetObjectGroup("objects").Visible = false;

            var objects = map.GetObjectGroup("items");
            objects.Visible = true;
            
            foreach (var item in objects.Objects)
            {
                var itemEntity = this.CreateEntity(item.Name);
                var collider = itemEntity.AddComponent(new ObjectBoxCollider(item, new Rectangle{X = (int)item.X, Y= (int)item.Y-map.TileHeight, Width = (int)item.Width, Height = (int)item.Height}));
                collider.IsTrigger = true;
                
                if (!bool.Parse(item.Properties["Collideable"]))
                {
                    continue;
                }
                
                var offsetWidth =(int)( item.Width * (1.0f / 4.0f));
                var offsetHeight =(int)( item.Height * (1.0f / 4.0f));
                itemEntity.AddComponent(new BoxCollider(new Rectangle{X = (int)item.X + offsetWidth/2, Y= (int)item.Y-map.TileHeight + offsetHeight/2, Width = (int)item.Width-offsetWidth, Height = (int)item.Height - offsetHeight}));
            }
            
            var sprites = map.GetObjectGroup("sprites");
            sprites.Visible = true;
            foreach (var item in sprites.Objects)
            {
                var spriteEntity = this.CreateEntity(item.Name);
                var collider = spriteEntity.AddComponent(new ObjectBoxCollider(item, new Rectangle{X = (int)item.X, Y= (int)item.Y-map.TileHeight, Width = (int)item.Width, Height = (int)item.Height}));
                collider.IsTrigger = true;

                if (!bool.Parse(item.Properties["Collideable"]))
                {
                    continue;
                }

                var offsetWidth =(int)( item.Width * (1.0f / 4.0f));
                var offsetHeight =(int)( item.Height * (1.0f / 4.0f));
                spriteEntity.AddComponent(new BoxCollider(new Rectangle{X = (int)item.X + offsetWidth/2, Y= (int)item.Y-map.TileHeight, Width = (int)item.Width-offsetWidth, Height = (int)item.Height - offsetHeight/2}));
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
            playerComponent = playerEntity.AddComponent(new PlayerComponent(this.gameState));
            playerEntity.AddComponent(new BoxCollider(-8, -8, 16, 16));

            this.Camera.Entity.AddComponent(new FollowCamera(playerEntity));
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
            playerComponent.IsInTransition = false;
        }
    }
}