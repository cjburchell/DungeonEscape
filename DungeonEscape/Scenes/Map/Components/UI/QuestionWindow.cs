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
        private VirtualButton acceptWindowInput;
        private int textIndex;

        public QuestionWindow(UICanvas canvas) : base(canvas, "", new Point(20, 20), 472,150)
        {
        }
        
        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            var table = this.Window.AddElement(new Table());

            this.closeButton =new TextButton("No", Skin);
            this.closeButton.GetLabel().SetFontScale(FontScale);
            this.closeButton.OnClicked += _ =>
            {
                this.CloseWindow(false);
            };
            
            this.acceptButton =new TextButton("Yes", Skin);
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
            
            this.acceptWindowInput = new VirtualButton();
            this.acceptWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Space));
            this.acceptWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.A));
        }
        
        public override void OnRemovedFromEntity()
        {
            this.hideWindowInput.Deregister();
            this.acceptWindowInput.Deregister();
        }

        protected override void HideWindow()
        {
            base.HideWindow();
            this.Window.GetStage().SetGamepadFocusElement(null);
            this.closeButton.GamepadRightElement = null;
            this.acceptButton.GamepadLeftElement = null;
            this.closeButton.GamepadLeftElement = null;
            this.acceptButton.GamepadRightElement = null;
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
            if (!this.IsVisible)
            {
                return;
            }

            if (this.textIndex <= this.textToShow.Length)
            {
                if (this.hideWindowInput.IsPressed)
                {
                    this.textIndex = this.textToShow.Length;
                    this.textLabel.SetText(this.textToShow);
                }
                else
                {
                    this.textLabel.SetText(this.textToShow.Substring(0, this.textIndex));
                    this.textIndex++;
                }
            }
            else
            {
                this.closeButton.SetVisible(true);
                this.acceptButton.SetVisible(true);
                this.Window.GetStage().SetGamepadFocusElement(this.acceptButton);
                this.closeButton.GamepadRightElement = this.acceptButton;
                this.acceptButton.GamepadLeftElement = this.closeButton;
                this.closeButton.GamepadLeftElement = this.acceptButton;
                this.acceptButton.GamepadRightElement = this.closeButton;
                if (this.hideWindowInput.IsPressed)
                {
                    this.CloseWindow(false);
                }
            }
        }

        public void Show(string question, Action<bool> doneAction, string[] buttonText = null)
        {
            this.done = doneAction;
            this.textIndex = 0;
            this.textToShow = question ?? "";
            this.textLabel.SetText("");
            if (buttonText != null)
            {
                this.acceptButton.GetLabel().SetText(buttonText[0]);
                this.closeButton.GetLabel().SetText(buttonText[1]);
            }
            else
            {
                this.acceptButton.GetLabel().SetText("Yes");
                this.closeButton.GetLabel().SetText("No");
            }
            
            this.acceptButton.SetVisible(false);
            this.closeButton.SetVisible(false);
            this.ShowWindow();
        }
    }
}