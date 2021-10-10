using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;

namespace DungeonEscape.Scene
{
    public class SplashScreen : Nez.Scene
    {
        private bool inTransistion;
        
        public override void Initialize()
        {
            this.SetDesignResolution(640, 480, SceneResolutionPolicy.ShowAll);
            var texture = this.Content.LoadTexture("Content/images/ui/splash.png");
            var splash = this.CreateEntity("splash");
            var renderer = new SpriteRenderer(new Sprite(texture)) {Origin = Vector2.Zero};
            splash.AddComponent(renderer);

            splash.Position = Vector2.Zero;
            base.Initialize();
        }

        public override void Update()
        {
            base.Update();

            if (!inTransistion && Time.TimeSinceSceneLoad > 3.0f)
            {
                inTransistion = true;
                MapScene.SetMap();
            }
        }
    }
}