using System.Collections.Generic;
using DungeonEscape.Scene;
using Microsoft.Xna.Framework;
using Nez;
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
            DebugRenderEnabled = false;
            Window.AllowUserResizing = true;
            Screen.SetSize(MapScene.screenWidth * 32, MapScene.screenHeight * 32);
            Scene = new EmptyScene();
            MapScene.SetMap(0);
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