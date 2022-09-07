namespace Redpoint.DungeonEscape.Scenes
{
    using System;
    using Common.Components.UI;
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;
    using Random = Nez.Random;

    public class CreatePlayerScene : Scene
    {
        private readonly ISounds _sounds;
        private Hero _hero;
        private Table _statusTable;

        private const int LabelColumnWidth = 150;
        private const int DataColumnWidth = 75;

        public CreatePlayerScene(ISounds sounds)
        {
            this._sounds = sounds;
            
        }
        public override void Initialize()
        {
            base.Initialize();
            
            if (!(Core.Instance is IGame game))
            {
                return;
            }

            this._hero = new Hero
            {
                Name = game.Names.Male[Random.NextInt(game.Names.Male.Count)],
                Class = Class.Hero,
                Gender = Gender.Male
            };
            
            this._hero.Setup(game);
            
            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);
            
            var mainTable = canvas.Stage.AddElement(new Table());
            mainTable.SetFillParent(true);
            mainTable.Top().PadLeft(10).PadTop(50);
            mainTable.Add(new Label("New Quest", BasicWindow.Skin, "medium_label")).SetColspan(2);
            mainTable.Row().SetPadTop(20);
            
            const int heroWidth = MapScene.DefaultTileSize;
            var texture = this.Content.LoadTexture("Content/images/sprites/hero.png");

            var contentTable = mainTable.Add(new Table()).GetElement<Table>();
            contentTable.Add(new Label("Name:", BasicWindow.Skin).SetAlignment(Align.Left)).Height(BasicWindow.ButtonHeight).Width(75);
            var nameField = contentTable.Add(new TextField(this._hero.Name, BasicWindow.Skin).SetAlignment(Align.Left)).Height(BasicWindow.ButtonHeight).Width(DataColumnWidth).GetElement<TextField>();
            var nameChanged = false;
            nameField.OnTextChanged += (_, s) =>
            {
                nameChanged = true;
                this._hero.Name = s;
            };
            
            var nameButton = new TextButton("Generate Name", BasicWindow.Skin);
            contentTable.Add(nameButton).Width(BasicWindow.ButtonWidth * 2).Height(BasicWindow.ButtonHeight).SetPadLeft(5);
            nameButton.OnClicked += _ =>
            {
                this._hero.Name =  this._hero.Gender == Gender.Male ? game.Names.Male[Random.NextInt(game.Names.Male.Count)] : game.Names.Female[Random.NextInt(game.Names.Female.Count)];
                nameField.SetText(this._hero.Name);
                nameChanged = false;
            };
            
            contentTable.Row().SetPadTop(3);
            
            contentTable.Add(new Label("Gender:", BasicWindow.Skin).SetAlignment(Align.Left)).Height(BasicWindow.ButtonHeight).Width(75);
            var genderField =
                new SelectBox<string>(BasicWindow.Skin).SetItems(Gender.Male.ToString(), Gender.Female.ToString());
            contentTable.Add(genderField).Height(BasicWindow.ButtonHeight).Width(DataColumnWidth);
            genderField.OnChanged += _ =>
            {
                this._hero.Gender = Enum.Parse<Gender>(genderField.GetSelected());
                if (!nameChanged)
                {
                    this._hero.Name =  this._hero.Gender == Gender.Male ? game.Names.Male[Random.NextInt(game.Names.Male.Count)] : game.Names.Female[Random.NextInt(game.Names.Female.Count)];
                    nameField.SetText(this._hero.Name);
                    nameChanged = false;
                }
                
     
                this._hero.SetupImage(texture);
            };
            contentTable.Row().SetPadTop(3);
            
            contentTable.Add(new Label("Class:", BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).Width(75);
            var classField = new SelectBox<string>(BasicWindow.Skin).SetItems(
                Class.Hero.ToString(),
                Class.Soldier.ToString(),
                Class.Cleric.ToString(),
                Class.Wizard.ToString(),
                Class.Fighter.ToString(),
                Class.Merchant.ToString(),
                Class.Thief.ToString(),
                Class.Sage.ToString());
            contentTable.Add(classField).Height(BasicWindow.ButtonHeight).Width(DataColumnWidth);
            classField.OnChanged += _ =>
            {
                this._hero.Class = Enum.Parse<Class>(classField.GetSelected());
                this._hero.Setup(game);
                this.UpdateStatus();
                this._hero.SetupImage(texture);
            };
            
            var rollButton = new TextButton("Re-roll", BasicWindow.Skin);
            contentTable.Add(rollButton).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).SetPadLeft(5);
            rollButton.OnClicked += _ =>
            {
                this._hero.Setup(game);
                this.UpdateStatus();
            };

            var border = new Table().SetBackground(new BorderPrimitiveDrawable(Color.Black, Color.White, 1));
            this._statusTable = new Table();
            this._hero.SetupImage(texture);
            border.Add(this._hero.Image).Width(heroWidth).SetPadLeft(5).SetPadRight(5);
            border.Add(this._statusTable);
            mainTable.Add(border).SetPadLeft(10);
            
            mainTable.Row().SetPadTop(20);
            var  buttonTable = new Table();
            mainTable.Add(buttonTable).SetColspan(3).SetFillX();
            
            var playButton = buttonTable.Add(new TextButton("Start", BasicWindow.Skin)).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).SetPadRight(5).GetElement<TextButton>();
            playButton.OnClicked += _ =>
            {
                this._sounds.PlaySoundEffect("confirm");
                var party = new Party();
                party.PlayerName = this._hero.Name;
                party.Members.Add(this._hero);
                game.LoadGame(new GameSave {Party = party});
            };
            playButton.ShouldUseExplicitFocusableControl = true;
            
            var backButton = buttonTable.Add(new TextButton("Back", BasicWindow.Skin)).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).SetPadLeft(5).GetElement<TextButton>();
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
            
            backButton.ShouldUseExplicitFocusableControl = true;
            nameField.ShouldUseExplicitFocusableControl = true;
            nameButton.ShouldUseExplicitFocusableControl = true;
            genderField.ShouldUseExplicitFocusableControl = true;
            classField.ShouldUseExplicitFocusableControl = true;
            rollButton.ShouldUseExplicitFocusableControl = true;
            backButton.ShouldUseExplicitFocusableControl = true;
            playButton.ShouldUseExplicitFocusableControl = true;
            
            canvas.Stage.SetGamepadFocusElement(playButton);
            
            nameField.GamepadDownElement = nameButton;
            nameButton.GamepadDownElement = genderField;
            genderField.GamepadDownElement = classField;
            classField.GamepadDownElement = playButton;
            playButton.GamepadDownElement = nameField;
            backButton.GamepadDownElement = nameField;
            
            nameField.GamepadUpElement = playButton;
            nameButton.GamepadUpElement = nameField;
            genderField.GamepadUpElement = nameButton;
            classField.GamepadUpElement = genderField;
            playButton.GamepadUpElement = classField;
            backButton.GamepadUpElement = classField;
            
            backButton.GamepadLeftElement = playButton;
            backButton.GamepadRightElement = playButton;
            playButton.GamepadRightElement = backButton;
            playButton.GamepadLeftElement = backButton;
            
            rollButton.GamepadLeftElement = classField;
            rollButton.GamepadRightElement = classField;
            classField.GamepadLeftElement = rollButton;
            classField.GamepadRightElement = rollButton;
            
            this.UpdateStatus();
            this._sounds.PlayMusic(new [] {"first-story"});
        }
        
         private void UpdateStatus()
        {
            this._statusTable.Clear();
            this._statusTable.Row();
            this._statusTable.Add(new Label("Health:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.MaxHealth}",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Magic:", BasicWindow.Skin).SetAlignment(Align.Left))
                .Width(LabelColumnWidth);
            if (this._hero.MaxMagic != 0)
            {
                this._statusTable
                    .Add(new Label($"{this._hero.MaxMagic}", BasicWindow.Skin).SetAlignment(
                        Align.Left)).Width(DataColumnWidth);
            }
            this._statusTable.Row();

            this._statusTable.Add(new Label("Attack:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Attack}",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Defence:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Defence}",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Magic Defence:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.MagicDefence}",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Agility:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Agility}", BasicWindow. Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);

            this._statusTable.Validate();
        }
    }
}