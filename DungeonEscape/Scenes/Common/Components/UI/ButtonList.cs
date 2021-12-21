namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
	using System;
	using Nez.UI;

	public class ButtonList : Table
	{
		public event Action<Button> OnClicked;
		private readonly ISounds _sounds;
		private Button _firstButton;
		private Button _lastButton;


		public ButtonList(ISounds sounds, Button firstButton = null, Button lastButton = null)
		{
			this._sounds = sounds;
			this._firstButton = firstButton;
			this._lastButton = lastButton;
		}

		public override void ClearChildren()
		{
			base.ClearChildren();
			this._firstButton = null;
			this._lastButton = null;
		}

		public Cell Add(Button button, int topPadding = 0)
		{
			button.OnClicked += _ =>
			{
				this._sounds.PlaySoundEffect("confirm");
				this.OnClicked?.Invoke(button);
			};

			this.Row().SetPadTop(topPadding);
			var cell = base.Add(button);
			
			button.ShouldUseExplicitFocusableControl = true;
			if (this._firstButton == null)
			{
				this.GetStage().SetGamepadFocusElement(button);
				this._firstButton = button;
				this._lastButton = button;
			}
			else
			{
				this._firstButton.GamepadUpElement = button;
				this._lastButton.GamepadDownElement = button;
			}
			
			button.GamepadDownElement = this._firstButton;
			button.GamepadUpElement = this._lastButton;
			this._lastButton = button;

			return cell;
		}

		public Button GetSelected()
		{
			return this.GetStage().GamepadFocusElement as Button;
		}
	}
}	