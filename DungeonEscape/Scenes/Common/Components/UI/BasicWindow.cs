using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public abstract class BasicWindow : Component
    {
        protected Window Window { get; private set; }
        protected readonly UISystem ui;
        private readonly string title;
        private readonly Point position;
        private readonly int width;
        private readonly int height;

        public static readonly Skin Skin = Skin.CreateDefaultSkin();
        private static BasicWindow focusedWindow;

        protected static BasicWindow FocusedWindow
        {
            set => focusedWindow = value;
        }

        public bool IsFocused => focusedWindow == this;
        
        static BasicWindow()
        {
            var windowStyle = Skin.Get<WindowStyle>();
            windowStyle.Background = new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
            var buttonStyle = new TextButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White, 1),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White, 1),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
                FontScale = 2
            };

            Skin.Add("default", buttonStyle);

            var labelStyle = Skin.Get<LabelStyle>();
            labelStyle.FontScale = 2;
            Skin.Add("default", labelStyle);
        }

        protected BasicWindow(UISystem ui, string title, Point position, int width, int height)
        {
            this.ui = ui;
            this.title = title;
            this.position = position;
            this.width = width;
            this.height = height;
            ui.Input.AddWindow(this);
            this.Window = new Window(this.title, Skin);
        }

        public override void OnAddedToEntity()
        {
            this.ui.Canvas.Stage.AddElement(this.Window);
            this.Window.SetPosition(this.position.X, this.position.Y);
            this.Window.SetWidth(this.width);
            this.Window.SetHeight(this.height);
            this.Window.SetMovable(false);
            this.Window.SetResizable(false);
            this.Window.GetTitleLabel();
            this.Window.GetTitleLabel().SetVisible(false);

            base.OnAddedToEntity();
        }

        public virtual void CloseWindow()
        {
            this.Window.SetVisible(false);
            this.ui.Input.RemoveWindow(this);
        }

        protected void ShowWindow()
        {
            this.Window.SetVisible(true);
            FocusedWindow = this;
        }

        public bool IsVisible => this.Window != null && this.Window.IsVisible();

        public virtual void DoAction()
        {
        }
    }
}