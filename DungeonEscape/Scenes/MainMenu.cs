using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Nez.UI;

namespace DungeonEscape.Scenes
{
    public class MainMenu : Nez.Scene
    {
        private Table table;

        public override void Initialize()
        {
            this.ClearColor = Color.Black;
            //this.AddRenderer(new ScreenSpaceRenderer(100, 999));
            this.SetDesignResolution(1024, 768, SceneResolutionPolicy.None);
            this.AddRenderer(new DefaultRenderer());
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            //Canvas.IsFullScreen = true;
            canvas.SetRenderLayer(999);
            
            this.table = canvas.Stage.AddElement(new Table());
            this.table.SetFillParent(true);
            this.table.Top().PadLeft(10).PadTop(50);
            this.table.Add(new Label("Dungeon Escape").SetFontScale(6));
            this.table.Row().SetPadTop(20);
            this.table.Add(new Label("Main Menu").SetFontScale(2));
            this.table.Row().SetPadTop(20);
            var playButton = this.table.Add(new TextButton("Start New Game", Skin.CreateDefaultSkin()) ).SetMinHeight(30).GetElement<TextButton>();
            playButton.GetLabel().SetFontScale(2);
            playButton.OnClicked += butt=> MapScene.SetMap();
            
            
            this.table.Row().SetPadTop(20);
            var loadButton = this.table.Add(new TextButton("Continue Game", Skin.CreateDefaultSkin()) ).SetMinHeight(30).GetElement<TextButton>();
            loadButton.GetLabel().SetFontScale(2);
            loadButton.SetDisabled(true);
            
            base.Initialize();
        }
    }
}