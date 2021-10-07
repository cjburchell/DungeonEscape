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

        public MapScene(int mapId, Point start = null)
        {
            this.mapId = mapId;
            this.start = start;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            var map = this.Content.LoadTiledMap($"Content/map{this.mapId}.tmx");
            this.SetDesignResolution(screenWidth * map.TileWidth, screenHeight * map.TileHeight, SceneResolutionPolicy.ShowAll);
            
            Console.WriteLine($"Loading Map {this.mapId}");
            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            var tiledEntity = this.CreateEntity("map");
            var tiledMapRenderer =  tiledEntity.AddComponent(new TiledMapRenderer(map, new[] {"wall", "water"}));
            tiledMapRenderer.RenderLayer = 10;
            map.GetObjectGroup("objects").Visible = false;
           

            var objects = map.GetObjectGroup("items");
            objects.Visible = true;
            var itemsEntity = this.CreateEntity("items");
            foreach (var item in objects.Objects)
            {
                var collider = itemsEntity.AddComponent(new ObjectBoxCollider(item, new Rectangle{X = (int)item.X, Y= (int)item.Y-map.TileHeight, Width = (int)item.Width, Height = (int)item.Height}));
                collider.IsTrigger = true;
                
                if (!bool.Parse(item.Properties["Collideable"]))
                {
                    continue;
                }
                
                var offsetWidth =(int)( item.Width * (1.0f / 4.0f));
                var offsetHeight =(int)( item.Height * (1.0f / 4.0f));
                itemsEntity.AddComponent(new BoxCollider(new Rectangle{X = (int)item.X + offsetWidth/2, Y= (int)item.Y-map.TileHeight + offsetHeight/2, Width = (int)item.Width-offsetWidth, Height = (int)item.Height - offsetHeight}));
            }
            
            var sprites = map.GetObjectGroup("sprites");
            sprites.Visible = true;
            var spritessEntity = this.CreateEntity("sprites");
            foreach (var item in sprites.Objects)
            {
                var collider = spritessEntity.AddComponent(new ObjectBoxCollider(item, new Rectangle{X = (int)item.X, Y= (int)item.Y-map.TileHeight, Width = (int)item.Width, Height = (int)item.Height}));
                collider.IsTrigger = true;

                if (!bool.Parse(item.Properties["Collideable"]))
                {
                    continue;
                }

                var offsetWidth =(int)( item.Width * (1.0f / 4.0f));
                var offsetHeight =(int)( item.Height * (1.0f / 4.0f));
                spritessEntity.AddComponent(new BoxCollider(new Rectangle{X = (int)item.X + offsetWidth/2, Y= (int)item.Y-map.TileHeight, Width = (int)item.Width-offsetWidth, Height = (int)item.Height - offsetHeight/2}));
            }
            
            var topLeft = new Vector2(map.TileWidth, map.TileWidth);
            var bottomRight = new Vector2(map.TileWidth * (map.Width - 1),
                map.TileWidth * (map.Height - 1));
            tiledEntity.AddComponent(new CameraBounds(topLeft, bottomRight));

            float spawnX;
            float spawnY;
            if (this.start == null)
            {
                var spawnObject = map.GetObjectGroup("objects").Objects["spawn"];
                spawnX = spawnObject.X + (map.TileHeight/2.0f);
                spawnY = spawnObject.Y - (map.TileWidth/2.0f);
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