using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using DungeonEscape.Scenes;
using DungeonEscape.Scenes.Map.Components.Objects;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;
using Nez.ImGuiTools;
using Nez.Tiled;

namespace DungeonEscape
{
    public class DungeonEscapeGame : Core, IGame
    {
        public Player Player { get; } = new Player();
        public bool IsPaused { get; set; }
        public List<Item> Items { get; } = new List<Item>();

        public int CurrentMapId { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
            
            var imGui = new ImGuiManager();
            RegisterGlobalManager(imGui);
            imGui.SetEnabled(false);
            
            ExitOnEscapeKeypress = false;
            PauseOnFocusLost = false;
            
            var tileset = LoadTileSet($"Content/items.tsx");
            foreach (var (_, tile) in tileset.Tiles)
            {
                this.Items.Add(new Item(tile));
            }

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

        static TmxTileset LoadTileSet(string path)
        {
            using var stream = TitleContainer.OpenStream(path);
            var xDocTileset = XDocument.Load(stream);

            var tsxDir = Path.GetDirectoryName(path);
            var tileSet = new TmxTileset().LoadTmxTileset(null, xDocTileset.Element("tileset"), 0, tsxDir);
            tileSet.TmxDirectory = tsxDir;

            return tileSet;
        }

        public TmxMap GetMap(int mapId)
        {
            return Content.LoadTiledMap($"Content/map{mapId}.tmx");
        }
    }
}