namespace Redpoint.DungeonEscape.Scenes
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;

    public class ContinueQuestScene : Scene
    {
        private static Button CreateButton(GameSave item)
        {
            var button = new Button(BasicWindow.Skin);
            var itemName = new Label(item.Name, BasicWindow.Skin).SetAlignment(Align.Left);
            if (item.Level.HasValue && item.Time.HasValue)
            {
                button.Add(itemName).Left().Width(125);
                var level = new Label($"LV: {item.Level.Value}", BasicWindow.Skin).SetAlignment(Align.Left);
                var time = new Label(item.Time.Value.ToString("g"), BasicWindow.Skin).SetAlignment(Align.Left);
                button.Add(level).Width(100).Left();
                button.Add(time).Width(225).Left();
            }
            else
            {
                button.Add(itemName).Left().Width(450);
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
            var backButton = new TextButton("Back", BasicWindow.Skin);
            backButton.GamepadDownElement = backButton;
            backButton.GamepadUpElement = backButton;
            backButton.ShouldUseExplicitFocusableControl = true;
            canvas.Stage.SetGamepadFocusElement(backButton);
            
            var buttonList = table.Add(new ButtonList(backButton, backButton)).GetElement<ButtonList>();
            buttonList.OnClicked += button =>
            {
                game.LoadGame(button?.UserData as GameSave);
            };

            game.ReloadSaveGames();
            foreach (var save in game.GameSaves)
            {
                var button = CreateButton(save);
                button.UserData = save;
                buttonList.Add(button, 5).Width(470).Height(BasicWindow.ButtonHeight);
            }
            
            table.Row();
            table.Add(backButton).SetPadTop(5).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).GetElement<TextButton>();
            backButton.OnClicked += _ =>
            {
                if (game.InGame)
                {
                    game.ResumeGame();
                }
                else
                {
                    Core.StartSceneTransition(new TransformTransition(() =>
                    {
                        var scene = new MainMenu();
                        scene.Initialize();
                        return scene;
                    }, TransformTransition.TransformTransitionType.SlideRight){Duration = 0.25f});
                }
            };
        }
    }
}