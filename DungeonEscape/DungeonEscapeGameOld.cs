using Microsoft.Xna.Framework;

namespace DungeonEscape
{
    using System;
    using System.Linq;
    using GameFile;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using World;
    using Map = World.Map;

    public class DungeonEscapeGameOld : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Map map;
        private Camera camera = new Camera();
        private Player player;
        private const int OverWorldMapId = 0;

        private const int screenWidth = 16;
        private const int screenHeight = 15;


        public DungeonEscapeGameOld()
        {
            this.graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.Window.AllowUserResizing = true;
            
            IsMouseVisible = false;
        }

        protected override void Initialize()
        {
            this.graphics.IsFullScreen = false;
            this.graphics.PreferredBackBufferHeight = 768;
            this.graphics.PreferredBackBufferWidth = 1024;
            this.graphics.ApplyChanges();
            base.Initialize();
        }
        

        private bool LoadMap(int id)
        {
            this.map = new Map();
            if (!this.map.Load(id))
                return false;
            
            Console.WriteLine($"Loaded map {id}");
            this.map.LoadContent(this.Content);
            this.player.Location = new Vector2(this.map.DefaultStart.X * Map.TileSize,
                this.map.DefaultStart.Y * Map.TileSize);

            return true;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            this.player = new Player();
            this.player.Visual = new Image {Texture = this.Content.Load<Texture2D>("images/sprites/player")};
            LoadMap(OverWorldMapId);
        }

        private bool MPressed = false;
        private int CurrentMap = OverWorldMapId;
        private bool NPressed = false;

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            
            const float playerSpeed = 128F;
            var elapsedGame = (float)gameTime.ElapsedGameTime.TotalSeconds; // Get current time in seconds
            var distance = elapsedGame * playerSpeed;

            if (keyboardState.IsKeyDown(Keys.M))
                MPressed = true;
            if (keyboardState.IsKeyUp(Keys.M) && this.MPressed)
            {
                while (!this.LoadMap(++CurrentMap))
                {
                    if (this.CurrentMap > 100)
                    {
                        this.CurrentMap = -1;
                    }
                }
                MPressed = false;
            }
            
            if (keyboardState.IsKeyDown(Keys.N))
                NPressed = true;
            if (keyboardState.IsKeyUp(Keys.N) && this.NPressed)
            {
                while (!this.LoadMap(--CurrentMap))
                {
                    if (this.CurrentMap == -1)
                    {
                        this.CurrentMap = 100;
                    }
                }
                NPressed = false;
            }
            
            if (keyboardState.IsKeyDown(Keys.R))
                this.LoadMap(CurrentMap);
                
            
            var playerLocation = this.player.Location;
            if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
                playerLocation.Y -= distance;

            if(keyboardState.IsKeyDown(Keys.Down)|| keyboardState.IsKeyDown(Keys.S))
                playerLocation.Y += distance;

            if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
                playerLocation.X -= distance;

            if(keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
                playerLocation.X += distance;

            var oldLocation = this.player.Location;
            this.player.Location = playerLocation;

            var (tiles, sprites) = this.map.ChekForCollision(this.player.BoundingBox);
            var tileList = tiles.ToList();
            var spriteList = sprites.ToList();
            if ( tileList.Count != 0 )
            {
                this.player.Location = oldLocation;
            }
            else if ( spriteList.Count != 0)
            {
                var warpTile = spriteList.FirstOrDefault(item => item.Instance.Type == SpriteType.Warp && item.Instance.Warp != null);
                if(warpTile != null)
                {
                    if (this.map.MapId == OverWorldMapId)
                    {
                        this.player.OverWorldLocation = oldLocation;
                    }
                    
                    this.LoadMap(warpTile.Instance.Warp.MapId);
                    if (warpTile.Instance.Warp.Location != null)
                    {
                        this.player.Location = new Vector2(warpTile.Instance.Warp.Location.X * Map.TileSize,
                            warpTile.Instance.Warp.Location.Y * Map.TileSize);

                    }
                    else if(warpTile.Instance.Warp.MapId == OverWorldMapId)
                    {
                        this.player.Location = this.player.OverWorldLocation;
                    }
                }
                else
                {
                    if (spriteList.Any(item => item.Instance.Collideable))
                    {
                        //this.player.Location = oldLocation;
                    }
                }
            }

            this.player.Update(gameTime);
            this.map.Update(gameTime);
            this.camera.Pos = this.player.Location;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {

            const int virtualWidth = screenWidth * Map.TileSize;
            const int virtualHeight = screenHeight * Map.TileSize;
            var rt = new RenderTarget2D(this.graphics.GraphicsDevice, virtualWidth, virtualHeight);
            GraphicsDevice.SetRenderTarget(rt);
            spriteBatch.Begin(SpriteSortMode.FrontToBack, transformMatrix: camera.GetTransormation(GraphicsDevice.Viewport));
            this.map.DrawTiles(spriteBatch);
            this.map.DrawSprites(spriteBatch);
            this.player.Draw(this.spriteBatch, 1);
            spriteBatch.End();
            
            
            GraphicsDevice.SetRenderTarget(null);

            var scaleWidth =  (double)GraphicsDevice.Viewport.Width / virtualWidth;
            var scaleHeight =  (double)GraphicsDevice.Viewport.Height / virtualHeight;
            double scale;
            var xOffset = 0;
            var yOffset = 0;
            if (scaleWidth < scaleHeight)
            {
                scale = scaleWidth;
                yOffset = (int) ((GraphicsDevice.Viewport.Height - (virtualHeight * scale)) / 2);
            }
            else
            {
                scale = scaleHeight;
                xOffset = (int) ((GraphicsDevice.Viewport.Width - (virtualWidth * scale)) / 2);
            }

            var rect = new Rectangle(xOffset, yOffset, (int) (virtualWidth * scale), (int) (virtualHeight * scale));
            
            spriteBatch.Begin();

            spriteBatch.Draw(rt, rect, Color.White);

            spriteBatch.End();
            
            base.Draw(gameTime);
        }
    }
}