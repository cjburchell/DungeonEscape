namespace Redpoint.DungeonEscape.Scenes
{
    using Common.Components.UI;
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;

    public class CreatePlayerScene : Scene
    {
        public override void Initialize()
        {
            base.Initialize();
            
            if (!(Core.Instance is IGame game))
            {
                return;
            }
            
            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);
            
            var table = canvas.Stage.AddElement(new Table());
            table.SetFillParent(true);
            table.Top().PadLeft(10).PadTop(50);
            table.Add(new Label("Name:", BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).GetElement<Label>();
            var name = game.Names.Male[Random.NextInt(game.Names.Male.Count)];
            var textField = table.Add(new TextField(name, BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).GetElement<TextField>();
            table.Row().SetPadTop(20);
            var playButton = table.Add(new TextButton("Start", BasicWindow.Skin)).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).GetElement<TextButton>();
            playButton.OnClicked += _ =>
            {
                var party = new Party();
                var hero = new Hero
                {
                    Name = textField.GetText(),
                    Class = Class.Hero,
                    Gender = Gender.Male
                };
                hero.RollStats(game.ClassLevelStats);
                party.Members.Add(hero);
                
                game.LoadGame(new GameSave {Party = party});
            };
            playButton.ShouldUseExplicitFocusableControl = true;
            
            var backButton = table.Add(new TextButton("Back", BasicWindow.Skin)).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).GetElement<TextButton>();
            backButton.OnClicked += _ =>
            {
                Core.StartSceneTransition(new TransformTransition(() =>
                {
                    var scene = new MainMenu();
                    scene.Initialize();
                    return scene;
                }, TransformTransition.TransformTransitionType.SlideRight){Duration = 0.25f});
            };
            
            backButton.ShouldUseExplicitFocusableControl = true;
            canvas.Stage.SetGamepadFocusElement(playButton);
            backButton.GamepadLeftElement = playButton;
            backButton.GamepadRightElement = playButton;
            playButton.GamepadRightElement = backButton;
            playButton.GamepadLeftElement = backButton;
        }
    }
}