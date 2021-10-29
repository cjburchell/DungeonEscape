using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class TextWindow : BasicWindow, IUpdatable
    {

        private string textToShow = "";
        private Label textLabel;
        private Action<string> done;
        private int textIndex;

        public TextWindow(UISystem ui,string title, Point position, int width = 472, int height = 150) : base(ui, title, position, width, height)
        {
        }
        
        private Button firstButton;
        private Button lastButton;
        private Table buttonTable;
        private ScrollPane scrollPane;
        private IEnumerable<string> buttonsText;

        private void AddButton(Button button)
        {
            button.OnClicked += _ =>
            {
                this.CloseWindow(button);
            };

            button.ShouldUseExplicitFocusableControl = true;
            if (this.firstButton == null)
            {
                this.firstButton = button;
                this.lastButton = button;
            }
            else
            {
                this.firstButton.GamepadLeftElement = button;
                this.lastButton.GamepadRightElement = button;
            }
			
            button.GamepadRightElement = this.firstButton;
            button.GamepadLeftElement = this.lastButton;
            this.lastButton = button;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.buttonTable = this.Window.AddElement(new Table());
            this.textLabel = new Label(this.textToShow, Skin);
            
            this.scrollPane = new ScrollPane(this.textLabel, Skin);

            // layout
            this.buttonTable.SetFillParent(true);
            this.Window.GetStage().SetGamepadFocusElement(null);
            
            this.textIndex = 0;
            this.textLabel.SetText("");
            this.buttonTable.ClearChildren();
            this.buttonTable.Top().PadLeft(10).PadTop(10).PadRight(10);
            var buttonTexts = buttonsText.ToList();
            this.textLabel.FillParent = true;
            this.textLabel.SetAlignment(Align.TopLeft);
            this.textLabel.SetWrap(false);
            this.buttonTable.Add(this.scrollPane).Height(105).Width(452).SetColspan(buttonTexts.Count);
            this.buttonTable.Row().SetPadTop(0);
            this.firstButton = null;
            this.lastButton = null;
            foreach (var buttonText in buttonTexts)
            {
                var buttonControl = new TextButton(buttonText, Skin) {UserData = buttonText};
                this.AddButton(buttonControl);
                this.buttonTable.Add(buttonControl).Height(ButtonHeight).Width(ButtonWidth);
                buttonControl.SetVisible(false);
            }
            
            this.buttonTable.Validate();

            this.Window.GetStage().SetGamepadFocusElement(null);
            this.ShowWindow();
        }

        private void CloseWindow(Element result)
        {
            base.CloseWindow();
            this.done?.Invoke(result?.UserData as string);
        }
        
        public override void CloseWindow()
        {
            this.CloseWindow(null);
        }

        public override void DoAction()
        {
            if (this.textIndex <= this.textToShow.Length)
            {
                this.textIndex = this.textToShow.Length;
                this.textLabel.SetText(this.textToShow);
            }
            else
            {
                this.CloseWindow(this.ui.Canvas.Stage.GamepadFocusElement as Element);
            }
        }

        public void Update()
        {
            if (!this.IsVisible || !this.IsFocused)
            {
                return;
            }

            if (this.textIndex <= this.textToShow.Length)
            {
                this.textLabel.SetText(this.textToShow.Substring(0, this.textIndex));
                    this.textIndex++;

                
                this.scrollPane.Validate();
                this.scrollPane.SetScrollY(this.scrollPane.GetMaxY());
            }
            else
            {
                if (this.firstButton != null && !this.firstButton.IsVisible())
                {
                    foreach (var child in this.buttonTable.GetChildren().OfType<TextButton>())
                    {
                        child.SetVisible(true);
                    }

                    this.Window.GetStage().SetGamepadFocusElement(this.firstButton);
                    
                    this.buttonTable.Validate();
                }
            }
        } 

        protected void Show(string text, Action<string> doneAction, IEnumerable<string> buttonsText)
        {
            this.done = doneAction;
            this.textToShow = text ?? "";
            this.buttonsText = buttonsText;
        }
    }
}