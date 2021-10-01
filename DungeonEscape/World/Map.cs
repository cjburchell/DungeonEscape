namespace DungeonEscape.World
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using GameFile;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using Newtonsoft.Json;
    using Point = GameFile.Point;

    public class Map
    {
        public const int TileSize = 32;
        private GameFile.Map MapFile;
        private readonly List<IVisual> visuals = new List<IVisual>();
        private readonly List<Tile> tiles = new List<Tile>();
        private readonly List<Sprite> sprites = new List<Sprite>();

        public Point DefaultStart => this.MapFile.DefaultStart;

        public int MapId => this.MapFile.Id;

        public bool Load(int mapId)
        {
            var filename = $"data/maps/map{mapId}.json";
            if (!File.Exists(filename))
                return false;
            
            var jsonString = File.ReadAllText(filename);
            this.MapFile = JsonConvert.DeserializeObject<GameFile.Map>(jsonString);

            return true;
        }

        public void LoadContent(ContentManager content)
        {
            visuals.Clear();
            foreach (var tile in this.MapFile.TileInfo)
            {
                try
                {
                    visuals.Add(new Image {Id = tile.Id, Texture = content.Load<Texture2D>(tile.Image)});
                }
                catch (ContentLoadException ex)
                {
                    Console.WriteLine($"Unable do load file {tile.Image} for {tile.OldId}({tile.Name}) : {ex.Message}");
                }
            }

            foreach (var spriteInfo in this.MapFile.SpriteInfo)
            {
                visuals.Add(new Image {Id = spriteInfo.Id, Texture = content.Load<Texture2D>(spriteInfo.Image)});
            }

            this.tiles.Clear();
            foreach (var instance in this.MapFile.Tiles)
            {
                tiles.Add(new Tile
                {
                    Instance = instance,
                    Visual = this.visuals.FirstOrDefault(item => item.Id == instance.Id),
                    Info = this.MapFile.TileInfo.FirstOrDefault(item => item.Id == instance.Id),
                    Location = new Vector2(instance.Position.X * TileSize, instance.Position.Y * TileSize),
                    Collideable = instance.Type != TileType.Ground || instance.Warp != null
                });
            }
            
            this.sprites.Clear();
            foreach (var instance in this.MapFile.Sprites)
            {
                sprites.Add(new Sprite
                {
                    Instance = instance,
                    Visual = this.visuals.FirstOrDefault(item => item.Id == instance.Id),
                    Info = this.MapFile.SpriteInfo.FirstOrDefault(item => item.Id == instance.Id),
                    Location = new Vector2(instance.StartPosition.X * TileSize, instance.StartPosition.Y * TileSize),
                    Collideable = true
                });
            }
        }
    
        public void Update(GameTime gameTime)
        {
            foreach (var tile in this.tiles)
            {
                tile.Update(gameTime);
            }
            
            foreach (var sprite in this.sprites)
            {
                sprite.Update(gameTime);
            }
        }

        public void DrawTiles(SpriteBatch spriteBatch)
        {
            foreach (var tile in this.tiles)
            {
                tile.Draw(spriteBatch);
            }
        }

        public void DrawSprites(SpriteBatch spriteBatch)
        {
            foreach (var sprite in this.sprites)
            {
                sprite.Draw(spriteBatch, 1);
            }
        }

        public (IEnumerable<Tile>, IEnumerable<Sprite>) ChekForCollision(Rectangle boundingBox)
        {
            var collidedTiles = this.tiles.Where(tile => tile.Collideable && boundingBox.Intersects(tile.BoundingBox));
            var collidedSprites = this.sprites.Where(sprite => sprite.Collideable && boundingBox.Intersects(sprite.BoundingBox));
            return (collidedTiles, collidedSprites);
        }
    }
}