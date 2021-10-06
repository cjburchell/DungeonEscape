namespace DungeonEscape.Scene
{
    using Nez;

    public class EmptyScene : Scene
    {
        public override void Initialize()
        {
            base.Initialize();
            this.SetDesignResolution(MapScene.screenWidth * 32, MapScene.screenHeight * 32, SceneResolutionPolicy.ShowAll);
            Screen.SetSize(MapScene.screenWidth * 32, MapScene.screenHeight * 32);
        }
    }
}