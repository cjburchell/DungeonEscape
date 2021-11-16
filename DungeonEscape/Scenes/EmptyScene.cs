namespace DungeonEscape.Scenes
{
    using Nez;

    public class EmptyScene : Scene
    {
        public override void Initialize()
        {
            base.Initialize();
            this.SetDesignResolution(MapScene.ScreenWidth , MapScene.ScreenHeight, MapScene.SceneResolution);
            Screen.SetSize(MapScene.ScreenWidth, MapScene.ScreenHeight);
        }
    }
}