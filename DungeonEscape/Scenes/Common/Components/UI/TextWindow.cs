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

        protected TextWindow(UISystem ui,string title, Point position, int width = MapScene.ScreenWidth - 20, int height = (MapScene.ScreenHeight) / 3 - 10) : base(ui, title, position, width, height)
        {
        }
        
        private Button firstButton;
        private Button lastButton;
        private Table buttonTable;
        private ScrollPane scrollPane;
        private IEnumerable<string> buttonText;

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
            this.textLabel = new Label(this.textToShow, Skin) {FillParent = true};
            this.textLabel.SetAlignment(Align.TopLeft);
            this.textLabel.SetWrap(false);
            this.textIndex = 0;
            this.textLabel.SetText("");
            this.scrollPane = new ScrollPane(this.textLabel, Skin);

            var table = this.Window.AddElement(new Table());
            table.SetFillParent(true);
            table.Top().PadLeft(10).PadTop(10).PadRight(10).Row();
            table.Row();
            table.Add(this.scrollPane).Height(105).Width(452);
            
            this.buttonTable = new Table();
            // layout
            this.firstButton = null;
            this.lastButton = null;
            foreach (var text in this.buttonText.ToList())
            {
                var buttonControl = new TextButton(text, Skin)
                {
                    UserData = buttonText,
                };
                buttonControl.UserData = text;
                this.AddButton(buttonControl);
                this.buttonTable.Add(buttonControl).Height(ButtonHeight).Width(ButtonWidth).SetPadRight(3).SetPadLeft(3);
            }
            
            table.Row();
            table.Add(this.buttonTable).Width(452).Height(30);
            this.buttonTable.SetVisible(false);
            this.buttonTable.Validate();
            this.ui.Canvas.Stage.SetGamepadFocusElement(null);
        }

        private void CloseWindow(Element result, bool remove = true)
        {
            base.CloseWindow(remove);
            this.done?.Invoke(result?.UserData as string);
        }
        
        public override void CloseWindow(bool remove = true)
        {
            this.CloseWindow(null, remove);
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
                var text = this.textToShow.Substring(0, this.textIndex);
                this.textLabel.SetText(text);
                this.textIndex++;
                this.textLabel.Validate();
                this.scrollPane.Validate();
                this.scrollPane.SetScrollY(this.scrollPane.GetMaxY());
            }
            else
            {
                if (!this.buttonTable.IsVisible())
                {
                    this.Window.GetStage().SetGamepadFocusElement(this.firstButton);
                    this.buttonTable.SetVisible(true);
                    this.buttonTable.Validate();
                    
                }
            }
        } 

        protected void Show(string text, Action<string> doneAction, IEnumerable<string> buttonText)
        {
            this.done = doneAction;
            this.textToShow = text ?? "";
            this.buttonText = buttonText;
            this.buttonTable?.SetVisible(false);
            this.buttonTable?.Validate();
            
            this.ShowWindow();
        }

        public void AppendText(string text)
        {
            this.textToShow += text;
            this.buttonTable?.SetVisible(false);
            this.buttonTable?.Validate();
        }
    }
}