using System.Collections.Generic;
using DungeonEscape.Scenes;
using Nez;
using Nez.ImGuiTools;
using Nez.Tiled;

namespace DungeonEscape
{
    public class DungeonEscapeGame : Core, IGame
    {
        public State.Player Player { get; } = new State.Player();
        public bool IsPaused { get; set; }
        private readonly Dictionary<int, TmxMap> loadedMaps = new Dictionary<int, TmxMap>();
        
        public int CurrentMapId { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
            
            var imGui = new ImGuiManager();
            RegisterGlobalManager(imGui);
            imGui.SetEnabled(false);
            
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