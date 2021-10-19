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

        protected static readonly Skin skin = Skin.CreateDefaultSkin();

        static GameWindow()
        {
            var windowStyle = skin.Get<WindowStyle>();
            windowStyle.Background = new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
            skin.Add("default", TextButtonStyle.Create(Color.Black, Color.Aqua, Color.Gray));
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
            this.Window = this.canvas.Stage.AddElement(new Window(this.title, skin));
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