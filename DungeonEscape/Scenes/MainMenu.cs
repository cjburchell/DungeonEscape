namespace Redpoint.DungeonEscape.Scenes
{
    using System.Linq;
    using Common.Components.UI;
    using Map;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Nez;
    using Nez.UI;

    public class MainMenu : Scene
    {
        private readonly ISounds _sounds;
        private Table _table;

        public MainMenu(ISounds sounds)
        {
            this._sounds = sounds;
        }

        private TextButton AddButton(string text)
        {
            this._table.Row().SetPadTop(20);
            var button = this._table.Add(new TextButton(text, BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).Width(250).GetElement<TextButton>();
            button.ShouldUseExplicitFocusableControl = true;
            return button;
        }

        public override void Initialize()
        {
            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);
            if (!(Core.Instance is IGame game))
            {
                return;
            }
            
            this._table = canvas.Stage.AddElement(new Table());
            this._table.SetFillParent(true);
            this._table.Top().PadLeft(10).PadTop(50);
            this._table.Add(new Label("Dungeon Escape", BasicWindow.Skin, "big_label"));
            var playButton = this.AddButton("New quest");
            playButton.OnClicked += _ =>
            {
                this._sounds.PlaySoundEffect("confirm");
                Core.StartSceneTransition(new TransformTransition(() =>
                {
                    var scene = new CreatePlayerScene(this._sounds);
                    scene.Initialize();
                    return scene;
                }, TransformTransition.TransformTransitionType.SlideLeft){Duration = 0.25f});
            };

            TextButton loadButton = null;
            if (game.LoadableGameSaves.Any(item => !item.IsEmpty))
            {
                loadButton = this.AddButton("Continue quest");
                loadButton.OnClicked += _ =>
                {
                    this._sounds.PlaySoundEffect("confirm");
                    Core.StartSceneTransition(new TransformTransition(() =>
                    {
                        var scene = new ContinueQuestScene(this._sounds);
                        scene.Initialize();
                        return scene;
                    }, TransformTransition.TransformTransitionType.SlideLeft) { Duration = 0.25f });
                };
            }

            var settingsButton = this.AddButton("Settings");
            settingsButton.OnClicked += _ =>
            {
                this._sounds.PlaySoundEffect("confirm");
                Core.StartSceneTransition(new TransformTransition(() =>
                {
                    var scene = new SettingsScene(this._sounds);
                    scene.Initialize();
                    return scene;
                }, TransformTransition.TransformTransitionType.SlideLeft){Duration = 0.25f});
            };
            
            this._table.Row().SetPadTop(20);
            var quitButton = this.AddButton("Quit");
            quitButton.OnClicked += _ =>
            {
                this._sounds.PlaySoundEffect("confirm");
                Core.Exit();
            };
            
            canvas.Stage.SetGamepadFocusElement(playButton);
            canvas.Stage.GamepadActionButton = Buttons.A;

            if (loadButton != null)
            {
                playButton.GamepadDownElement = loadButton;
                loadButton.GamepadDownElement = settingsButton;
                loadButton.GamepadUpElement = playButton;
                settingsButton.GamepadUpElement = loadButton;
            }
            else
            {
                playButton.GamepadDownElement = settingsButton;
                settingsButton.GamepadUpElement = playButton;
            }
            
            settingsButton.GamepadDownElement = quitButton;
            quitButton.GamepadDownElement = playButton;
            playButton.GamepadUpElement = quitButton;
            quitButton.GamepadUpElement = settingsButton;

            base.Initialize();
            this._sounds.PlayMusic(@"first-story");
        }
    }
}