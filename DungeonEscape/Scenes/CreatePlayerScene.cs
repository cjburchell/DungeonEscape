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

        private const int LabelColumnWidth = 125;
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
            
            this._hero.RollStats(game.ClassLevelStats);
            
            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            var canvas = this.CreateEntity("ui-canvas").AddComponent(new UICanvas());
            canvas.SetRenderLayer(999);
            
            const int heroWidth = MapScene.DefaultTileSize;
            var imageTable = canvas.Stage.AddElement(new Table());
            imageTable.SetFillParent(true);
            imageTable.Top().PadLeft(10).PadTop(50);
            var texture = this.Content.LoadTexture("Content/images/sprites/hero.png");
            
            this._hero.SetupImage(texture);
            imageTable.Add(this._hero.Image).Width(heroWidth).SetPadLeft(5).SetPadRight(5);
            
            var table = imageTable.Add(new Table()).GetElement<Table>();
            table.Add(new Label("Name:", BasicWindow.Skin).SetAlignment(Align.Left)).Height(BasicWindow.ButtonHeight).Width(LabelColumnWidth);
            var nameField = table.Add(new TextField(this._hero.Name, BasicWindow.Skin).SetAlignment(Align.Left)).Height(BasicWindow.ButtonHeight).Width(DataColumnWidth).GetElement<TextField>();
            var nameChanged = false;
            nameField.OnTextChanged += (_, s) =>
            {
                nameChanged = true;
                this._hero.Name = s;
            };
            
            var nameButton = new TextButton("Generate Name", BasicWindow.Skin);
            table.Add(nameButton).Width(BasicWindow.ButtonWidth * 2).Height(BasicWindow.ButtonHeight).SetPadLeft(5);
            nameButton.OnClicked += _ =>
            {
                this._hero.Name =  this._hero.Gender == Gender.Male ? game.Names.Male[Random.NextInt(game.Names.Male.Count)] : game.Names.Female[Random.NextInt(game.Names.Female.Count)];
                nameField.SetText(this._hero.Name);
                nameChanged = false;
            };
            
            table.Row().SetPadTop(3);
            
            table.Add(new Label("Gender:", BasicWindow.Skin).SetAlignment(Align.Left)).Height(BasicWindow.ButtonHeight).Width(LabelColumnWidth);
            var genderField =
                new SelectBox<string>(BasicWindow.Skin).SetItems(Gender.Male.ToString(), Gender.Female.ToString());
            table.Add(genderField).Height(BasicWindow.ButtonHeight).Width(DataColumnWidth);
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
            table.Row().SetPadTop(3);
            
            table.Add(new Label("Class:", BasicWindow.Skin)).Height(BasicWindow.ButtonHeight).Width(LabelColumnWidth);
            var classField = new SelectBox<string>(BasicWindow.Skin).SetItems(
                Class.Hero.ToString(),
                Class.Soldier.ToString(),
                Class.Cleric.ToString(),
                Class.Wizard.ToString(),
                Class.Fighter.ToString(),
                Class.Merchant.ToString(),
                Class.Thief.ToString(),
                Class.Sage.ToString());
            table.Add(classField).Height(BasicWindow.ButtonHeight).Width(DataColumnWidth);
            classField.OnChanged += _ =>
            {
                this._hero.Class = Enum.Parse<Class>(classField.GetSelected());
                this._hero.RollStats(game.ClassLevelStats);
                this.UpdateStatus();
                this._hero.SetupImage(texture);
            };
            table.Row();
            
            this._statusTable = new Table();
            table.Add(this._statusTable).SetColspan(2);
            
            table.Row().SetPadTop(10);

            var rollButton = new TextButton("Re-roll", BasicWindow.Skin);
            table.Add(rollButton).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).SetColspan(3);
            rollButton.OnClicked += _ =>
            {
                this._hero.RollStats(game.ClassLevelStats);
                this.UpdateStatus();
            };
            
            table.Row().SetPadTop(20);
            
            var  buttonTable = new Table();
            table.Add(buttonTable).SetColspan(3).SetFillX();
            
            var playButton = buttonTable.Add(new TextButton("Start", BasicWindow.Skin)).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).SetPadRight(5).GetElement<TextButton>();
            playButton.OnClicked += _ =>
            {
                this._sounds.PlaySoundEffect("confirm");
                var party = new Party();
                party.Members.Add(this._hero);
                game.LoadGame(new GameSave {Party = party});
            };
            playButton.ShouldUseExplicitFocusableControl = true;
            
            var backButton = buttonTable.Add(new TextButton("Back", BasicWindow.Skin)).Width(BasicWindow.ButtonWidth).Height(BasicWindow.ButtonHeight).SetPadLeft(5).GetElement<TextButton>();
            backButton.OnClicked += _ =>
            {
                this._sounds.PlaySoundEffect("confirm");
                Core.StartSceneTransition(new TransformTransition(() =>
                {
                    var scene = new MainMenu(this._sounds);
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
            this.UpdateStatus();
            this._sounds.PlayMusic(@"first-story");
        }
        
         private void UpdateStatus()
        {
            this._statusTable.Clear();
            this._statusTable.Row();
            this._statusTable.Add(new Label("Health:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Health}/{this._hero.MaxHealth}",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Magic:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Magic}/{this._hero.MaxMagic}",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Attack:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Attack}",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Defence:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Defence}",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);
            this._statusTable.Row();
            this._statusTable.Add(new Label("Agility:",  BasicWindow.Skin).SetAlignment(Align.Left)).Width(LabelColumnWidth);
            this._statusTable.Add(new Label($"{this._hero.Agility}", BasicWindow. Skin).SetAlignment(Align.Left)).Width(DataColumnWidth);

            this._statusTable.Validate();
        }
    }
}