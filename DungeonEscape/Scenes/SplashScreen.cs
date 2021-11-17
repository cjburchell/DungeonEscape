namespace Redpoint.DungeonEscape.Scenes
{
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.Sprites;
    using Nez.Textures;

    public class SplashScreen : Scene
    {
        private bool _inTransition;
        
        public override void Initialize()
        {
            this.SetDesignResolution(640, 480, MapScene.SceneResolution);
            var texture = this.Content.LoadTexture("Content/images/ui/splash.png");
            var splash = this.CreateEntity("splash");
            var renderer = new SpriteRenderer(new Sprite(texture)) {Origin = Vector2.Zero};
            splash.AddComponent(renderer);
            base.Initialize();
        }

        public override void Update()
        {
            base.Update();

            if (this._inTransition || !(Time.TimeSinceSceneLoad > 2.0f))
            {
                return;
            }

            this._inTransition = true;
            
            Core.StartSceneTransition(new FadeTransition(() =>
            {
                var splash = new MainMenu();
                splash.Initialize();
                return splash;
            }));
        }
    }
}