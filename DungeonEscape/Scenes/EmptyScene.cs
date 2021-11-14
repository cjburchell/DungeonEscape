namespace DungeonEscape.Scenes
{
    public class EmptyScene : Nez.Scene
    {
        public override void Initialize()
        {
            base.Initialize();
            this.SetDesignResolution(MapScene.ScreenWidth , MapScene.ScreenHeight, MapScene.SceneResolution);
            Nez.Screen.SetSize(MapScene.ScreenWidth, MapScene.ScreenHeight);
        }
    }
}