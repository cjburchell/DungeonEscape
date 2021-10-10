using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;

namespace DungeonEscape.Scenes
{
    public class MainMenu : Nez.Scene
    {
        private bool inTransition;

        public override void Initialize()
        {
            this.SetDesignResolution(640, 480, SceneResolutionPolicy.ShowAll);
            var texture = this.Content.LoadTexture("Content/images/ui/start1.png");
            var splash = this.CreateEntity("splash");
            var renderer = new SpriteRenderer(new Sprite(texture)) {Origin = Vector2.Zero};
            splash.AddComponent(renderer);
            base.Initialize();
        }
        
        public override void Update()
        {
            base.Update();

            if (inTransition || !(Time.TimeSinceSceneLoad > 2.0f))
            {
                return;
            }
            
            inTransition = true;
            MapScene.SetMap();
        }
    }
}