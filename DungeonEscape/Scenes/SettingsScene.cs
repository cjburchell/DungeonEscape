using System.IO;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace Redpoint.DungeonEscape.Scenes
{
    using Common.Components.UI;
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;

    public class SettingsScene : Scene
    {
        private readonly ISounds _sounds;

        public SettingsScene(ISounds sounds)
        {
            this._sounds = sounds;
        }
        
        private const int LabelColumnWidth = 200;
        private const int DataColumnWidth = 200;

        public override void Initialize()
        {
            base.Initialize();

            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);
            if (Core.Instance is not IGame game)
            {
                return;
            }

            var table = canvas.Stage.AddElement(new Table());
            table.SetFillParent(true);
            table.Top().PadLeft(10).PadTop(50);
            table.Add(new Label("Settings", BasicWindow.Skin, "medium_label")).SetColspan(2);
            table.Row().SetPadTop(20);

            table.Add(new Label("Full Screen", BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            var fullScreenCheckbox = table.Add(new CheckBox("Full Screen", BasicWindow.Skin))
                .Height(BasicWindow.ButtonHeight).Width(DataColumnWidth).SetAlign(Align.Left).GetElement<TextButton>();
            fullScreenCheckbox.ShouldUseExplicitFocusableControl = true;
            fullScreenCheckbox.IsChecked = game.Settings.IsFullScreen;
            fullScreenCheckbox.OnChanged += isChecked =>
            {
                this._sounds.PlaySoundEffect("confirm");
                game.Settings.IsFullScreen = isChecked;
                Screen.IsFullscreen = game.Settings.IsFullScreen;
                if (game.Settings.IsFullScreen)
                {
                    Screen.SetSize(Screen.MonitorWidth, Screen.MonitorHeight);
                }
                else
                {
                    Screen.SetSize(MapScene.ScreenWidth, MapScene.ScreenHeight);
                }
            };

            table.Row().SetPadTop(20);

            table.Add(new Label("Music Volume", BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            var musicSlider = table.Add(new Slider(0.0f, 1.0f, 0.1f, false, BasicWindow.Skin))
                .Height(BasicWindow.ButtonHeight).Width(DataColumnWidth).GetElement<Slider>();
            musicSlider.ShouldUseExplicitFocusableControl = true;
            musicSlider.Value = game.Settings.MusicVolume;
            musicSlider.OnChanged += volume =>
            {
                this._sounds.PlaySoundEffect("confirm");
                _sounds.MusicVolume = volume;
                game.Settings.MusicVolume = volume;
            };

            table.Row().SetPadTop(20);

            table.Add(new Label("Sound Effect Volume", BasicWindow.Skin).SetAlignment(Align.Left))
                .Width(LabelColumnWidth);
            var fxSlider = table.Add(new Slider(0.0f, 1.0f, 0.1f, false, BasicWindow.Skin))
                .Height(BasicWindow.ButtonHeight).Width(DataColumnWidth).GetElement<Slider>();
            fxSlider.ShouldUseExplicitFocusableControl = true;
            fxSlider.Value = game.Settings.SoundEffectsVolume;
            fxSlider.OnChanged += volume =>
            {
                this._sounds.PlaySoundEffect("confirm");
                _sounds.SoundEffectsVolume = volume;
                game.Settings.SoundEffectsVolume = volume;
            };

            table.Row().SetPadTop(20);

#if DEBUG
            table.Add(new Label("No Monsters", BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            var noMonstersCheckbox = table.Add(new CheckBox("No Monsters", BasicWindow.Skin))
                .Height(BasicWindow.ButtonHeight).Width(DataColumnWidth).SetAlign(Align.Left).GetElement<TextButton>();
            noMonstersCheckbox.ShouldUseExplicitFocusableControl = true;
            noMonstersCheckbox.IsChecked = game.Settings.NoMonsters;
            noMonstersCheckbox.OnChanged += isChecked =>
            {
                this._sounds.PlaySoundEffect("confirm");
                game.Settings.NoMonsters = isChecked;
            };
#endif

            table.Row().SetPadTop(20);


            var backButton = new TextButton("Done", BasicWindow.Skin);
            backButton.ShouldUseExplicitFocusableControl = true;

            table.Row();
            table.Add(backButton).SetPadTop(5).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight)
                .SetColspan(2).GetElement<TextButton>();
            backButton.OnClicked += _ =>
            {
                if (!Directory.Exists(DungeonEscape.Game.SavePath))
                {
                    Directory.CreateDirectory(DungeonEscape.Game.SavePath);
                }

                File.WriteAllText(DungeonEscape.Game.SettingsFile,
                    JsonConvert.SerializeObject(game.Settings, Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        }));


                this._sounds.PlaySoundEffect("confirm");
                if (game.InGame)
                {
                    game.ResumeGame();
                }
                else
                {
                    Core.StartSceneTransition(new TransformTransition(() =>
                    {
                        var scene = new MainMenu(this._sounds);
                        scene.Initialize();
                        return scene;
                    }, TransformTransition.TransformTransitionType.SlideRight) { Duration = 0.25f });
                }
            };

            canvas.Stage.SetGamepadFocusElement(backButton);
            canvas.Stage.GamepadActionButton = Buttons.A;

            backButton.GamepadDownElement = fullScreenCheckbox;
            fullScreenCheckbox.GamepadDownElement = musicSlider;
            musicSlider.GamepadDownElement = fxSlider;
#if DEBUG
            fxSlider.GamepadDownElement = noMonstersCheckbox;
            noMonstersCheckbox.GamepadDownElement = backButton;
#else
            fxSlider.GamepadDownElement = backButton;
#endif

            
            fullScreenCheckbox.GamepadUpElement = backButton;
            musicSlider.GamepadUpElement = fullScreenCheckbox;
            fxSlider.GamepadUpElement = musicSlider;
#if DEBUG
            noMonstersCheckbox.GamepadUpElement = fxSlider;
            backButton.GamepadUpElement = noMonstersCheckbox;
#else
            backButton.GamepadUpElement = fxSlider;
#endif
            this._sounds.PlayMusic(new[] { "first-story" });
        }
    }
}