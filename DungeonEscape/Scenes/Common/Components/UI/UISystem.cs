namespace DungeonEscape.Scenes.Common.Components.UI
{
    using Nez;

    public class UISystem
    {
        public UISystem(UICanvas canvas, bool noInput = false)
        {
            this.Canvas = canvas;
            if (!noInput)
            {
                this.Input = canvas.AddComponent(new WindowInput());
            }
        }

        public UICanvas Canvas { get;  }
        public WindowInput Input { get;  }
    }
}