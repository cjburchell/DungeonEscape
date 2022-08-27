namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework.Input;
    using Nez;

    public class WindowInput : Component, IUpdatable
    {
        private readonly VirtualButton _hideWindowInput = new();
        private readonly VirtualButton _actionWindowInput = new();
        private readonly List<BasicWindow> _windows = new();
        public bool HandledHide = false;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            
            this._hideWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Escape));
            this._hideWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.B));
            
            this._actionWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Space));
            this._actionWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Enter));
            this._actionWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.A));
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();
            this._hideWindowInput.Deregister();
            this._actionWindowInput.Deregister();
        }

        public void AddWindow(BasicWindow window)
        {
            this._windows.Add(window);
        }
        
        public void RemoveWindow(BasicWindow window)
        {
            this._windows.Remove(window);
        }

        public void Update()
        {
            if (this._hideWindowInput.IsReleased && !this.HandledHide)
            {
                foreach (var window in this._windows.Where(window => window.IsVisible && window.IsFocused))
                {
                    window.CloseWindow();
                    return;
                }
            }
            else if (this._actionWindowInput.IsReleased)
            {
                foreach (var window in this._windows.Where(window => window.IsVisible && window.IsFocused))
                {
                    window.DoAction();
                    return;
                }
            }
        }
    }
}