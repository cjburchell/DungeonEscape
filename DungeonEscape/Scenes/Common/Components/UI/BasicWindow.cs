using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    using System;

    public abstract class BasicWindow : Component
    {
        protected Window Window { get; }
        protected readonly UISystem ui;
        private readonly string title;
        private readonly Point position;
        private readonly int width;
        private readonly int height;

        public static readonly Skin Skin = Skin.CreateDefaultSkin();
        private static BasicWindow focusedWindow;
        private readonly string id;

        public const int ButtonHeight = 30;
        public const int ButtonWidth = 80;
        public const int FontScale = 2;

        public bool IsFocused => focusedWindow == this;
        
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
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White, 1),
            };
            Skin.Add("default", buttonStyle);
            
            var buttonNoBorderStyle = new ButtonStyle
            {
                Up = new BorderPrimitiveDrawable(Color.Black, Color.White),
                Down = new BorderPrimitiveDrawable(Color.LightGray, Color.White),
                Over = new BorderPrimitiveDrawable(Color.Gray, Color.White),
                Checked = new BorderPrimitiveDrawable(Color.Gray, Color.White),
            };
            Skin.Add("no_border", buttonNoBorderStyle);

            var labelStyle = Skin.Get<LabelStyle>();
            labelStyle.FontScale = FontScale;
            Skin.Add("default", labelStyle);
            
            var textFieldStyle = Skin.Get<TextFieldStyle>();
            textFieldStyle.Background =new BorderPrimitiveDrawable(Color.Black, Color.White, 1);
        }

        protected BasicWindow(UISystem ui, string title, Point position, int width, int height)
        {
            this.id = Guid.NewGuid().ToString();
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
            Console.WriteLine($"{this.id} {this.title} OnAddedToEntity");
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
            this.ui.Canvas.RemoveComponent(this);
            this.Window.GetStage().SetGamepadFocusElement(null);
            
            Console.WriteLine($"{this.id} {this.title} Close");
        }

        protected void ShowWindow()
        {
            this.Window.SetVisible(true);
            focusedWindow = this;
        }

        public bool IsVisible => this.Window != null && this.Window.IsVisible();

        public virtual void DoAction()
        {
        }
    }
}