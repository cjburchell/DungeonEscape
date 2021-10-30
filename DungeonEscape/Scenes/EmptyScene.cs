namespace DungeonEscape.Scenes
{
    public class EmptyScene : Nez.Scene
    {
        public override void Initialize()
        {
            base.Initialize();
            this.SetDesignResolution(MapScene.ScreenWidth * 32, MapScene.ScreenHeight * 32, MapScene.SceneResolution);
            Nez.Screen.SetSize(MapScene.ScreenWidth * 32, MapScene.ScreenHeight * 32);
        }
    }
}