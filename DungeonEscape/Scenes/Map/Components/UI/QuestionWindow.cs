using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class QuestionWindow : GameWindow, IUpdatable
    {
        private VirtualButton hideWindowInput;
        private string textToShow = "";
        private TextButton closeButton;
        private TextButton acceptButton;
        private Label textLabel;
        private Action<bool> done;
        
        public QuestionWindow(UICanvas canvas) : base(canvas, "", new Point(20, 20), 472,150)
        {
        }
        
        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            var table = this.Window.AddElement(new Table());

            this.closeButton =new TextButton("No", skin);
            this.closeButton.GetLabel().SetFontScale(FontScale);
            this.closeButton.OnClicked += _ =>
            {
                this.CloseWindow(false);
            };
            
            this.acceptButton =new TextButton("Yes", skin);
            this.acceptButton.GetLabel().SetFontScale(FontScale);
            this.acceptButton.OnClicked += _ =>
            {
                this.CloseWindow(true);
            };
            
            this.textLabel = new Label(this.textToShow);
            this.textLabel.SetFontScale(FontScale);

            // layout
            table.SetFillParent(true);
            table.Top().PadLeft(10).PadTop(10).PadRight(10);
            table.Add(this.textLabel).Height(105).Width(452).SetColspan(2);
            table.Row().SetPadTop(0);
            table.Add(this.acceptButton).Height(30).Width(80);
            table.Add(this.closeButton).Height(30).Width(80);
            
            this.hideWindowInput = new VirtualButton();
            this.hideWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
            this.hideWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.RightControl));
            this.hideWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.B));
        }
        
        public override void OnRemovedFromEntity()
        {
            this.hideWindowInput.Deregister();
        }

        protected override void HideWindow()
        {
            base.HideWindow();
            this.Window.GetStage().SetGamepadFocusElement(null);
            this.acceptButton.GamepadDownElement = null;
            this.closeButton.GamepadDownElement = null;
            this.acceptButton.GamepadUpElement = null;
            this.closeButton.GamepadUpElement = null;
        }

        protected override void ShowWindow()
        {
            base.ShowWindow();
            this.Window.GetStage().SetGamepadFocusElement(this.acceptButton);
            this.closeButton.GamepadDownElement = this.acceptButton;
            this.acceptButton.GamepadDownElement = this.closeButton;
            this.closeButton.GamepadUpElement = this.acceptButton;
            this.acceptButton.GamepadUpElement = this.closeButton;
        }
        
        private void CloseWindow(bool action)
        {
            if (!this.IsVisible)
            {
                return;
            }

            this.HideWindow();
            this.done?.Invoke(action);
        }
        
        public void Update()
        {
            if (this.hideWindowInput.IsPressed)
            {
                this.CloseWindow(false);   
            }
        }

        public void Show(string question, Action<bool> doneAction)
        {
            this.done = doneAction;
            this.textToShow = question ?? "";
            this.textLabel.SetText(this.textToShow);
            this.ShowWindow();
        }
    }
}