namespace DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;

    public abstract class BasicWindow : Component
    {
        protected Window Window { get; private set; }
        protected readonly UISystem ui;
        private readonly string title;
        private readonly Point position;
        private readonly int width;
        private readonly int height;
        private readonly bool focasable;

        public static readonly Skin Skin = Skin.CreateDefaultSkin();
        private static BasicWindow focusedWindow;

        public const int ButtonHeight = 30;
        public const int ButtonWidth = 80;
        private const int FontScale = 2;

        public bool IsFocused => focusedWindow == this;

        private bool hasBeenAdded;
        private bool isVisible;
        
        static BasicWindow()
        {
            var windowStyle = Skin.Get<WindowStyle>();
            windowStyle.Background = new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
            var textButtonStyle = new TextButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White, 1),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White, 1),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
                FontScale = FontScale
            };

            Skin.Add("default", textButtonStyle);
            
            var textButtonNoBorderStyle = new TextButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White),
                FontScale = FontScale
            };
            Skin.Add("no_border", textButtonNoBorderStyle);
            
            
            var buttonStyle = new ButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White, 1),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White, 1),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1)
            };
            Skin.Add("default", buttonStyle);
            
            var buttonNoBorderStyle = new ButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White)
            };
            Skin.Add("no_border", buttonNoBorderStyle);

            var labelStyle = Skin.Get<LabelStyle>();
            labelStyle.FontScale = FontScale;
            Skin.Add("default", labelStyle);
            
            var textFieldStyle = Skin.Get<TextFieldStyle>();
            textFieldStyle.Background =new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
        }

        protected BasicWindow(UISystem ui, string title, Point position, int width, int height, bool focasable = true)
        {
            this.ui = ui;
            this.title = title;
            this.position = position;
            this.width = width;
            this.height = height;
            this.focasable = focasable;
            ui.Input?.AddWindow(this);
        }

        public override void OnAddedToEntity()
        {
            this.Window = new Window(this.title, Skin);
            this.ui.Canvas.Stage.AddElement(this.Window);
            this.Window.SetPosition(this.position.X, this.position.Y);
            this.Window.SetWidth(this.width);
            this.Window.SetHeight(this.height);
            this.Window.SetMovable(false);
            this.Window.SetResizable(false);
            this.Window.GetTitleLabel().SetVisible(false);
            this.Window.GetTitleLabel().SetText(this.title);

            base.OnAddedToEntity();
            this.Window.SetVisible(this.isVisible);
        }

        public virtual void CloseWindow(bool remove = true)
        {
            this.Window?.SetVisible(false);
            this.isVisible = false;
            
            if (this.focasable)
            {
                this.ui.Input?.RemoveWindow(this);
                this.ui.Canvas.Stage.SetGamepadFocusElement(null);
            }

            if (!remove)
            {
                return;
            }

            if (this.hasBeenAdded)
            {
                this.ui.Canvas.RemoveComponent(this);
            }

            this.hasBeenAdded = false;
        }

        public void ShowWindow()
        {
            if (!this.hasBeenAdded)
            {
                this.ui.Canvas.AddComponent(this);
            }
            
            this.hasBeenAdded = true;
            this.isVisible = true;
            this.Window?.SetVisible(true);
            if (this.focasable)
            {
                focusedWindow = this;
            }
        }

        public bool IsVisible => this.Window != null && this.Window.IsVisible();

        public virtual void DoAction()
        {
        }
    }
}