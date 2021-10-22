using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Map.Components
{
    public abstract class GameWindow : Component
    {
        protected Window Window { get; private set; }
        private readonly UICanvas canvas;
        private readonly string title;
        private readonly Point position;
        private readonly int width;
        private readonly int height;

        public static readonly Skin Skin = Skin.CreateDefaultSkin();

        static GameWindow()
        {
            var windowStyle = Skin.Get<WindowStyle>();
            windowStyle.Background = new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
            var buttonStyle = new TextButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White, 1),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White, 1),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1)
            };

            Skin.Add("default", buttonStyle);
        }
        
        
        protected const int FontScale = 2;
        
        protected GameWindow(UICanvas canvas, string title, Point position, int width, int Height)
        {
            this.canvas = canvas;
            this.title = title;
            this.position = position;
            this.width = width;
            this.height = Height;
        }
        
        public override void OnAddedToEntity()
        {
            this.Window = this.canvas.Stage.AddElement(new Window(this.title, Skin));
            this.Window.SetPosition(this.position.X, this.position.Y);
            this.Window.SetWidth(this.width);
            this.Window.SetHeight(this.height);
            this.Window.SetMovable(false);
            this.Window.SetResizable(false);
            this.Window.GetTitleLabel().SetFontScale(FontScale);
            this.Window.GetTitleLabel().SetVisible(false);
            this.Window.SetVisible(false);
            
            base.OnAddedToEntity();
        }
        
        protected virtual void HideWindow()
        {
            this.Window.SetVisible(false);
        }

        protected virtual void ShowWindow()
        {
            this.Window.SetVisible(true);
        }
        
        protected bool IsVisible => this.Window != null && this.Window.IsVisible();
    }
}