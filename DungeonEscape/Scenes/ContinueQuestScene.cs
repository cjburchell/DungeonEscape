namespace Redpoint.DungeonEscape.Scenes
{
    using System.Linq;
    using Common.Components.UI;
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;

    public class ContinueQuestScene : Scene
    {
        private readonly ISounds _sounds;

        public ContinueQuestScene(ISounds sounds)
        {
            this._sounds = sounds;
        }
        private static Button CreateButton(GameSave item)
        {
            var button = new Button(BasicWindow.Skin);
            var itemName = new Label(item.Name, BasicWindow.Skin);
            if (!item.IsEmpty)
            {
                button.Add(itemName).Left().Width(200);
                if (item.Level != null)
                {
                    var level = new Label($"LV: {item.Level.Value}", BasicWindow.Skin);
                    button.Add(level).Width(100).Left();
                }
                
                if (item.Time != null)
                {
                    var time = new Label(item.Time.Value.ToString("g"), BasicWindow.Skin);
                    button.Add(time).Width(250).Left();
                }
            }
            else
            {
                button.Add(itemName).Left().Width(550);
                button.SetDisabled(true);
            }
            
            return button;
        }
        
         public override void Initialize()
        {
            base.Initialize();
            
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
            
            var table = canvas.Stage.AddElement(new Table());
            table.SetFillParent(true);
            table.Top().PadLeft(10).PadTop(50);
            table.Add(new Label("Continue Quest", BasicWindow.Skin, "medium_label"));
            table.Row().SetPadTop(20);
            
            var backButton = new TextButton("Back", BasicWindow.Skin);
            backButton.GamepadDownElement = backButton;
            backButton.GamepadUpElement = backButton;
            backButton.ShouldUseExplicitFocusableControl = true;
            canvas.Stage.SetGamepadFocusElement(backButton);
            
            var buttonList = table.Add(new ButtonList(this._sounds,backButton, backButton)).GetElement<ButtonList>();
            buttonList.OnClicked += button =>
            {
                this._sounds.PlaySoundEffect("confirm");
                game.LoadGame(button?.UserData as GameSave);
            };

            game.ReloadSaveGames();
            foreach (var save in game.LoadableGameSaves.OrderByDescending(i => i.Time))
            {
                var button = CreateButton(save);
                button.UserData = save;
                buttonList.Add(button, 5).Width(570).Height(BasicWindow.ButtonHeight);
            }
            
            table.Row();
            table.Add(backButton).SetPadTop(5).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).GetElement<TextButton>();
            backButton.OnClicked += _ =>
            {
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
                    }, TransformTransition.TransformTransitionType.SlideRight){Duration = 0.25f});
                }
            };
            
            this._sounds.PlayMusic(new [] {"first-story"});
        }
    }
}