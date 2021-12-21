namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Nez;

    public class UiSystem
    {
        public UiSystem(UICanvas canvas, ISounds sounds, bool noInput = false)
        {
            this.Sounds = sounds;
            this.Canvas = canvas;
            if (!noInput)
            {
                this.Input = canvas.AddComponent(new WindowInput());
            }
        }

        public UICanvas Canvas { get;  }
        public WindowInput Input { get;  }
        
        public ISounds Sounds { get; }
    }
}