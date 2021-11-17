namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;

    public abstract class BasicWindow : Component
    {
        protected Window Window { get; private set; }
        protected readonly UiSystem Ui;
        private readonly string _title;
        private readonly Point _position;
        private readonly int _width;
        private readonly int _height;
        private readonly bool _focasable;

        public static readonly Skin Skin = Skin.CreateDefaultSkin();
        private static BasicWindow focusedWindow;

        public const int ButtonHeight = 30;
        public const int ButtonWidth = 80;
        private const int FontScale = 2;

        public bool IsFocused => focusedWindow == this;

        private bool _hasBeenAdded;
        private bool _isVisible;
        
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

        protected BasicWindow(UiSystem ui, string title, Point position, int width, int height, bool focasable = true)
        {
            this.Ui = ui;
            this._title = title;
            this._position = position;
            this._width = width;
            this._height = height;
            this._focasable = focasable;
            ui.Input?.AddWindow(this);
        }

        public override void OnAddedToEntity()
        {
            this.Window = new Window(this._title, Skin);
            this.Ui.Canvas.Stage.AddElement(this.Window);
            this.Window.SetPosition(this._position.X, this._position.Y);
            this.Window.SetWidth(this._width);
            this.Window.SetHeight(this._height);
            this.Window.SetMovable(false);
            this.Window.SetResizable(false);
            this.Window.GetTitleLabel().SetVisible(false);
            this.Window.GetTitleLabel().SetText(this._title);

            base.OnAddedToEntity();
            this.Window.SetVisible(this._isVisible);
        }

        public virtual void CloseWindow(bool remove = true)
        {
            this.Window?.SetVisible(false);
            this._isVisible = false;
            
            if (this._focasable)
            {
                this.Ui.Input?.RemoveWindow(this);
                this.Ui.Canvas.Stage.SetGamepadFocusElement(null);
            }

            if (!remove)
            {
                return;
            }

            if (this._hasBeenAdded)
            {
                this.Ui.Canvas.RemoveComponent(this);
            }

            this._hasBeenAdded = false;
        }

        public void ShowWindow()
        {
            if (!this._hasBeenAdded)
            {
                this.Ui.Canvas.AddComponent(this);
            }
            
            this._hasBeenAdded = true;
            this._isVisible = true;
            this.Window?.SetVisible(true);
            if (this._focasable)
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