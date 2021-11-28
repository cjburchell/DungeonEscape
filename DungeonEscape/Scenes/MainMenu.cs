namespace Redpoint.DungeonEscape.Scenes
{
    using Common.Components.UI;
    using Map;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Nez;
    using Nez.UI;

    public class MainMenu : Scene
    {
        private Table _table;

        public override void Initialize()
        {
            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);
            
            this._table = canvas.Stage.AddElement(new Table());
            this._table.SetFillParent(true);
            this._table.Top().PadLeft(10).PadTop(50);
            this._table.Add(new Label("Dungeon Escape", BasicWindow.Skin, "big_label"));
            this._table.Row().SetPadTop(20);
            var playButton = this._table.Add(new TextButton("Start new quest", BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).Width(250).GetElement<TextButton>();
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
            this._table.Row().SetPadTop(20);
            var loadButton = this._table.Add(new TextButton("Continue quest", BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).Width(250).GetElement<TextButton>();
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