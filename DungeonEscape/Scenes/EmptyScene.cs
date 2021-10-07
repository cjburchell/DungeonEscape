namespace DungeonEscape.Scene
{
    public class EmptyScene : Nez.Scene
    {
        public override void Initialize()
        {
            base.Initialize();
            this.SetDesignResolution(MapScene.screenWidth * 32, MapScene.screenHeight * 32, SceneResolutionPolicy.ShowAll);
            Nez.Screen.SetSize(MapScene.screenWidth * 32, MapScene.screenHeight * 32);
        }
    }
}