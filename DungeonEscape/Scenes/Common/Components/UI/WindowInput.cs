using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Nez;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class WindowInput : Component, IUpdatable
    {
        private readonly VirtualButton hideWindowInput = new VirtualButton();
        private readonly VirtualButton actionWindowInput = new VirtualButton();
        private readonly List<BasicWindow> windows = new List<BasicWindow>();
        public bool HandledHide = false;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            
            this.hideWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Escape));
            this.hideWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.B));
            
            this.actionWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Space));
            this.actionWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Enter));
            this.actionWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.A));
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();
            this.hideWindowInput.Deregister();
            this.actionWindowInput.Deregister();
        }

        public void AddWindow(BasicWindow window)
        {
            this.windows.Add(window);
        }
        
        public void RemoveWindow(BasicWindow window)
        {
            this.windows.Remove(window);
        }

        public void Update()
        {
            if (this.hideWindowInput.IsReleased && !this.HandledHide)
            {
                foreach (var window in this.windows.Where(window => window.IsVisible && window.IsFocused))
                {
                    window.CloseWindow();
                    return;
                }
            }
            else if (this.actionWindowInput.IsReleased)
            {
                foreach (var window in this.windows.Where(window => window.IsVisible && window.IsFocused))
                {
                    window.DoAction();
                    return;
                }
            }
        }
    }
}