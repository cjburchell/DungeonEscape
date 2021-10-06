using DungeonEscape.Components;

namespace DungeonEscape.Scene
{
    using System;
    using Microsoft.Xna.Framework;
    using Nez;
    using Point = GameFile.Point;

    public class MapScene : Scene
    {
        private const int ScreenSpaceRenderLayer = 999;
        public const int screenWidth = 16;
        public const int screenHeight = 15;
        private readonly int mapId;
        private readonly Point start;

        public MapScene(int mapId, Point start = null) : base()
        {
            this.mapId = mapId;
            this.start = start;
        }
        
        public override void Initialize()
        {
            base.Initialize();

            this.SetDesignResolution(screenWidth * 32, screenHeight * 32, Core.DebugRenderEnabled? SceneResolutionPolicy.BestFit:SceneResolutionPolicy.ShowAllPixelPerfect);

            Screen.SetSize(screenWidth * 32, screenHeight * 32);
            Console.WriteLine($"Loading Map {this.mapId}");
            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            var tiledEntity = this.CreateEntity("map");
            var map = this.Content.LoadTiledMap($"Content/map{this.mapId}.tmx");
            var tiledMapRenderer =  tiledEntity.AddComponent(new TiledMapRenderer(map, new[] {"wall", "water"}));
            tiledMapRenderer.RenderLayer = 10;

            var objects = map.GetObjectGroup("objects");
            var warpsEntity = this.CreateEntity("objects");
            foreach (var item in objects.Objects)
            {
                var collider = warpsEntity.AddComponent(new ObjectBoxCollider(item, new Rectangle{X = (int)item.X, Y= (int)item.Y, Width = (int)item.Width, Height = (int)item.Height}));
                collider.IsTrigger = true;
            }
            
            var topLeft = new Vector2(map.TileWidth, map.TileWidth);
            var bottomRight = new Vector2(map.TileWidth * (map.Width - 1),
                map.TileWidth * (map.Height - 1));
            tiledEntity.AddComponent(new CameraBounds(topLeft, bottomRight));

            int spawnX;
            int spawnY;
            if (this.start == null)
            {
                spawnX = int.Parse(map.Properties["DefaultStartX"]) * 32;
                spawnY = int.Parse(map.Properties["DefaultStartY"]) * 32;
            }
            else
            {
                spawnX = this.start.X * 32;
                spawnY = this.start.Y * 32;
            }

            var playerEntity = this.CreateEntity("player", new Vector2(spawnX, spawnY));
            playerEntity.AddComponent(new PlayerComponent());
            playerEntity.AddComponent(new BoxCollider(-8, -8, 16, 16));

            this.Camera.Entity.AddComponent(new FollowCamera(playerEntity));
        }
    }
}