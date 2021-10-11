using System.Collections.Generic;
using DungeonEscape.Scenes;
using Microsoft.Xna.Framework;
using Nez;
using Nez.ImGuiTools;
using Nez.Tiled;

namespace DungeonEscape
{
    public class Player
    {
        public Vector2 OverWorldPos { get; set; } = Vector2.Zero;
    }

    public interface IGame
    {
        TmxMap GetMap(int mapId);

        int CurrentMapId { get; set; }

        Player Player { get; }
    }

    public class DungeonEscapeGame : Core, IGame
    {
        public Player Player { get; } = new Player();
        private readonly Dictionary<int, TmxMap> loadedMaps = new Dictionary<int, TmxMap>();
        
        public int CurrentMapId { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
            
            var imGui = new ImGuiManager();
            RegisterGlobalManager(imGui);
            imGui.Enabled = false;
            
            ExitOnEscapeKeypress = false;
            PauseOnFocusLost = false;

            DebugRenderEnabled = false;
            Window.AllowUserResizing = true;
            Screen.SetSize(MapScene.ScreenWidth * 32, MapScene.ScreenHeight * 32);
            Scene = new EmptyScene();
            StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new SplashScreen();
                splash.Initialize();
                return splash;
            }));
        }

        public TmxMap GetMap(int mapId)
        {
            if (this.loadedMaps.ContainsKey(mapId))
            {
                return this.loadedMaps[mapId];
            }

            var map = Content.LoadTiledMap($"Content/map{mapId}.tmx");
            this.loadedMaps.Add(mapId, map);
            return map;
        }
    }
}