using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;

namespace DungeonEscape.Scene
{
    public class SplashScreen : Nez.Scene
    {
        public override void Initialize()
        {
            this.AddRenderer(new DefaultRenderer());
            this.SetDesignResolution(640, 480, SceneResolutionPolicy.ShowAll);
            var texture = this.Content.LoadTexture("Content/images/ui/splash.png");
            var splash = this.CreateEntity("splash", Vector2.Zero);
            splash.Components.Add(new SpriteRenderer(new Sprite(texture, 0,0,640,480)));
            splash.Position = Vector2.Zero;
            base.Initialize();
        }

        public override void Update()
        {
            if (Time.TimeSinceSceneLoad > 10.0f)
            {
               MapScene.SetMap();
            }
            
            base.Update();
        }
    }
}