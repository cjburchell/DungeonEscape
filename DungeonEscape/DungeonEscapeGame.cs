using DungeonEscape.Scene;
using Nez;

namespace DungeonEscape
{
    public class DungeonEscapeGame : Core
    {
        protected override void Initialize()
        {
            base.Initialize();
            DebugRenderEnabled = false;
            Window.AllowUserResizing = true;
            Screen.SetSize(MapScene.screenWidth * 32, MapScene.screenHeight * 32);
            Scene = new EmptyScene();
            StartSceneTransition(new FadeTransition(() =>
            {
                var map = new MapScene(0);
                map.Initialize();
                return map;
            }));
        }
    }
}