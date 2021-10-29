using System;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
	public class ButtonList : Table
	{
		public event Action<Button> OnClicked;
		private Button firstButton;
		private Button lastButton;


		public ButtonList(Button firstButton = null, Button lastButton = null)
		{
			this.firstButton = firstButton;
			this.lastButton = lastButton;
		}

		public override void ClearChildren()
		{
			base.ClearChildren();
			this.firstButton = null;
			this.lastButton = null;
		}

		public Cell Add(Button button, int topPadding = 0)
		{
			button.OnClicked += _ =>
			{
				this.OnClicked?.Invoke(button);
			};

			this.Row().SetPadTop(topPadding);
			var cell = base.Add(button);
			
			button.ShouldUseExplicitFocusableControl = true;
			if (this.firstButton == null)
			{
				this.GetStage().SetGamepadFocusElement(button);
				this.firstButton = button;
				this.lastButton = button;
			}
			else
			{
				this.firstButton.GamepadUpElement = button;
				this.lastButton.GamepadDownElement = button;
			}
			
			button.GamepadDownElement = this.firstButton;
			button.GamepadUpElement = this.lastButton;
			this.lastButton = button;

			return cell;
		}

		public Button GetSelected()
		{
			return this.GetStage().GamepadFocusElement as Button;
		}
	}
}	