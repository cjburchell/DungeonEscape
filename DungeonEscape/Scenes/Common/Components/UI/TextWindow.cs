﻿namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;

    public class TextWindow : BasicWindow, IUpdatable
    {

        private string _textToShow = "";
        private Label _textLabel;
        private Action<string> _done;
        private int _textIndex;

        protected TextWindow(UiSystem ui, Point position, int width = MapScene.ScreenWidth - 20,
            int height = MapScene.ScreenHeight / 3 - 10) : base(ui, position, width, height)
        {
        }

        private Button _firstButton;
        private Button _lastButton;
        private Table _buttonTable;
        private ScrollPane _scrollPane;
        private IEnumerable<string> _buttonText;

        private void AddButton(Button button)
        {
            button.OnClicked += _ =>
            {
                this.Ui.Sounds.PlaySoundEffect("confirm");
                this.CloseWindow(button);
            };

            button.ShouldUseExplicitFocusableControl = true;
            if (this._firstButton == null)
            {
                this._firstButton = button;
                this._lastButton = button;
            }
            else
            {
                this._firstButton.GamepadLeftElement = button;
                this._lastButton.GamepadRightElement = button;
            }
			
            button.GamepadRightElement = this._firstButton;
            button.GamepadLeftElement = this._lastButton;
            this._lastButton = button;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this._textLabel = new Label(this._textToShow, Skin) {FillParent = true};
            this._textLabel.SetAlignment(Align.TopLeft);
            this._textLabel.SetWrap(false);
            this._textIndex = 0;
            this._textLabel.SetText("");
            this._scrollPane = new ScrollPane(this._textLabel, Skin);
            var table =  new Table();
            this.Window.Add(table);
            table.SetFillParent(true);
            table.Add(this._scrollPane).Width(this.Width-20).Height(this.Height - 50);
            
            this._buttonTable = new Table();
            // layout
            this._firstButton = null;
            this._lastButton = null;
            foreach (var text in this._buttonText.ToList())
            {
                var buttonControl = new TextButton(text, Skin)
                {
                    UserData = this._buttonText
                };
                buttonControl.UserData = text;
                this.AddButton(buttonControl);
                this._buttonTable.Add(buttonControl).Height(ButtonHeight).Width(ButtonWidth).SetPadRight(3).SetPadLeft(3);
            }
            
            table.Row();
            table.Add(this._buttonTable).Width(this.Width).Height(30);
            this._buttonTable.SetVisible(false);
            this._buttonTable.Validate();
            this.Ui.Canvas.Stage.SetGamepadFocusElement(null);
        }

        private void CloseWindow(Element result, bool remove = true)
        {
            base.CloseWindow(remove);
            this._done?.Invoke(result?.UserData as string);
        }
        
        public override void CloseWindow(bool remove = true)
        {
            this.CloseWindow(null, remove);
        }

        public override void DoAction()
        {
            if (this._textIndex <= this._textToShow.Length)
            {
                this._textIndex = this._textToShow.Length;
                this._textLabel.SetText(this._textToShow);
            }
            else
            {
                this.CloseWindow(this.Ui.Canvas.Stage.GamepadFocusElement as Element);
            }
        }

        public void Update()
        {
            if (!this.IsVisible || !this.IsFocused)
            {
                return;
            }

            if (this._textIndex <= this._textToShow.Length)
            {
                var text = this._textToShow.Substring(0, this._textIndex);
                this._textLabel.SetText(text);
                this._textIndex++;
                this._textLabel.Validate();
                this._scrollPane.Validate();
                this._scrollPane.SetScrollY(this._scrollPane.GetMaxY());
            }
            else
            {
                if (this._buttonTable.IsVisible())
                {
                    return;
                }

                this.Window.GetStage().SetGamepadFocusElement(this._firstButton);
                this._buttonTable.SetVisible(true);
                this._buttonTable.Validate();
            }
        } 

        protected void Show(string text, Action<string> doneAction, IEnumerable<string> buttonTextList)
        {
            this._done = doneAction;
            this._textToShow = text ?? "";
            this._buttonText = buttonTextList;
            this._buttonTable?.SetVisible(false);
            this._buttonTable?.Validate();
            
            this.ShowWindow();
        }
    }
}