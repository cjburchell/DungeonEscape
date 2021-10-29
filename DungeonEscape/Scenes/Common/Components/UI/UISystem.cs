namespace DungeonEscape.Scenes.Common.Components.UI
{
    using Nez;

    public class UISystem
    {
        public UISystem(UICanvas canvas)
        {
            this.Canvas = canvas;
            this.Input = canvas.AddComponent(new WindowInput());
            canvas.SetRenderLayer(999);
            canvas.Stage.GamepadActionButton = null;
        }

        public UICanvas Canvas { get;  }
        public WindowInput Input { get;  }
    }
}