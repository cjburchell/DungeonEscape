using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public abstract class BasicWindow : Component
    {
        protected Window Window { get; private set; }
        protected readonly UICanvas canvas;
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

        protected BasicWindow(UICanvas canvas, WindowInput input, string title, Point position, int width, int height)
        {
            this.canvas = canvas;
            this.title = title;
            this.position = position;
            this.width = width;
            this.height = height;
            input.AddWindow(this);
        }
        
        public override void OnAddedToEntity()
        {
            this.Window = this.canvas.Stage.AddElement(new Window(this.title, Skin));
            this.Window.SetPosition(this.position.X, this.position.Y);
            this.Window.SetWidth(this.width);
            this.Window.SetHeight(this.height);
            this.Window.SetMovable(false);
            this.Window.SetResizable(false);
            this.Window.GetTitleLabel();
            this.Window.GetTitleLabel().SetVisible(false);
            this.Window.SetVisible(false);
            
            base.OnAddedToEntity();
        }

        public virtual void HideWindow()
        {
            this.Window.SetVisible(false);
        }

        protected virtual void ShowWindow()
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