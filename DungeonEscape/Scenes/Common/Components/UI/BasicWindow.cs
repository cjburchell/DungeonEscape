namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Nez;
    using Nez.UI;

    public abstract class BasicWindow : Component
    {
        protected Window Window { get; private set; }
        protected readonly UiSystem Ui;
        private readonly string _title;
        private readonly Point _position;
        protected readonly int Width;
        protected readonly int Height;
        private readonly bool _focasable;

        public static readonly Skin Skin = Skin.CreateDefaultSkin();
        private static BasicWindow focusedWindow;

        public const int ButtonHeight = 30;
        public const int ButtonWidth = 80;
        private const int FontScale = 1;

        public bool IsFocused => focusedWindow == this;

        private bool _hasBeenAdded;
        private bool _isVisible;
        
        static BasicWindow()
        {
            var font = new NezSpriteFont(Core.Content.Load<SpriteFont>("fonts/Arial_bold"));
            var windowStyle = Skin.Get<WindowStyle>();
            windowStyle.TitleFont = font;
            windowStyle.Background = new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
            var textButtonStyle = new TextButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White, 1),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White, 1),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
                FontScale = FontScale,
                Font = font
            };

            Skin.Add("default", textButtonStyle);
            
            var textButtonNoBorderStyle = new TextButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White),
                FontScale = FontScale,
                Font = font
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
            labelStyle.Font = font;
            Skin.Add("default", labelStyle);

            var bigLabelStyle = new LabelStyle
            {
                FontScale = FontScale,
                Font = new NezSpriteFont(Core.Content.Load<SpriteFont>("fonts/Arial_bold_big"))
            };
            Skin.Add("big_label", bigLabelStyle);
            
            var textFieldStyle = Skin.Get<TextFieldStyle>();
            textFieldStyle.Font = font;
            textFieldStyle.Background =new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
            
            var selectBoxStyle = Skin.Get<SelectBoxStyle>();
            selectBoxStyle.Font = font;
            selectBoxStyle.Background =new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
            Skin.Add("default", selectBoxStyle);
            
            var listBoxStyle = Skin.Get<ListBoxStyle>();
            listBoxStyle.Font = font;
            listBoxStyle.Background =new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
            Skin.Add("default", selectBoxStyle);
        }

        protected BasicWindow(UiSystem ui, string title, Point position, int width, int height, bool focasable = true)
        {
            this.Ui = ui;
            this._title = title;
            this._position = position;
            this.Width = width;
            this.Height = height;
            this._focasable = focasable;
            ui.Input?.AddWindow(this);
        }

        public override void OnAddedToEntity()
        {
            this.Window = new Window(this._title, Skin);
            this.Ui.Canvas.Stage.AddElement(this.Window);
            this.Window.SetPosition(this._position.X, this._position.Y);
            this.Window.SetWidth(this.Width);
            this.Window.SetHeight(this.Height);
            this.Window.SetMovable(false);
            this.Window.SetResizable(false);
            var title = this.Window.GetTitleLabel();
            title.SetFontScale(FontScale);

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