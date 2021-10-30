using DungeonEscape.Scenes.Common.Components.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes
{
    public class MainMenu : Nez.Scene
    {
        private Table table;

        public override void Initialize()
        {
            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth * 32, MapScene.ScreenHeight * 32,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);
            
            this.table = canvas.Stage.AddElement(new Table());
            this.table.SetFillParent(true);
            this.table.Top().PadLeft(10).PadTop(50);
            this.table.Add(new Label("Dungeon Escape").SetFontScale(6));
            this.table.Row().SetPadTop(20);
            var playButton = this.table.Add(new TextButton("Start new quest", BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).Width(250).GetElement<TextButton>();
            playButton.OnClicked += _ =>
            {
                Core.StartSceneTransition(new TransformTransition(() =>
                {
                    var scene = new CreatePlayerScene();
                    scene.Initialize();
                    return scene;
                }, TransformTransition.TransformTransitionType.SlideLeft){Duration = 0.25f});
            };
            playButton.ShouldUseExplicitFocusableControl = true;
            this.table.Row().SetPadTop(20);
            var loadButton = this.table.Add(new TextButton("Continue quest", BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).Width(250).GetElement<TextButton>();
            loadButton.OnClicked += _ =>
            {
                Core.StartSceneTransition(new TransformTransition(() =>
                {
                    var scene = new ContinueQuestScene();
                    scene.Initialize();
                    return scene;
                }, TransformTransition.TransformTransitionType.SlideLeft){Duration = 0.25f});
            };
            
            loadButton.ShouldUseExplicitFocusableControl = true;
            canvas.Stage.SetGamepadFocusElement(playButton);
            canvas.Stage.GamepadActionButton = Buttons.A;
            playButton.GamepadDownElement = loadButton;
            playButton.GamepadUpElement = loadButton;
            loadButton.GamepadDownElement = playButton;
            loadButton.GamepadUpElement = playButton;

            base.Initialize();
        }
    }
}