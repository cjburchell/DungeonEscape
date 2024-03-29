﻿using System;
using Nez.BitmapFonts;
using Microsoft.Xna.Framework;


namespace Nez.UI
{
	public class TextButton : Button
	{
		Label label;
		TextButtonStyle style;


		public TextButton(string text, TextButtonStyle style) : base(style)
		{
			SetStyle(style);
			label = new Label(text, style.Font, style.FontColor, style.FontScaleX, style.FontScaleY);
			label.SetAlignment(UI.Align.Center);

			Add(label).Expand().Fill();
			SetSize(PreferredWidth, PreferredHeight);
		}


		public TextButton(string text, Skin skin, string styleName = null) : this(text,
			skin.Get<TextButtonStyle>(styleName))
		{
		}


		public override void SetStyle(ButtonStyle style)
		{
			Insist.IsTrue(style is TextButtonStyle, "style must be a TextButtonStyle");

			base.SetStyle(style);
			this.style = (TextButtonStyle) style;

			if (label != null)
			{
				var textButtonStyle = (TextButtonStyle) style;
				var labelStyle = label.GetStyle();
				labelStyle.Font = textButtonStyle.Font;
				labelStyle.FontColor = textButtonStyle.FontColor;
				labelStyle.FontScaleX = textButtonStyle.FontScaleX;
				labelStyle.FontScaleY = textButtonStyle.FontScaleY;
				label.SetStyle(labelStyle);
			}
		}


		public new TextButtonStyle GetStyle()
		{
			return style;
		}


		public override void Draw(Batcher batcher, float parentAlpha)
		{
			Color? fontColor = null;
			if (_isDisabled && style.DisabledFontColor.HasValue)
				fontColor = style.DisabledFontColor;
			else if (_mouseDown && style.DownFontColor.HasValue)
				fontColor = style.DownFontColor;
			else if (IsChecked &&
			         (!_mouseOver && style.CheckedFontColor.HasValue ||
			          _mouseOver && style.CheckedOverFontColor.HasValue))
				fontColor = (_mouseOver && style.CheckedOverFontColor.HasValue)
					? style.CheckedOverFontColor
					: style.CheckedFontColor;
			else if (_mouseOver && style.OverFontColor.HasValue)
				fontColor = style.OverFontColor;
			else
				fontColor = style.FontColor;

			if (fontColor != null)
				label.GetStyle().FontColor = fontColor.Value;

			base.Draw(batcher, parentAlpha);
		}


		public Label GetLabel()
		{
			return label;
		}


		public Cell GetLabelCell()
		{
			return GetCell(label);
		}


		public TextButton SetText(String text)
		{
			label.SetText(text);
			return this;
		}


		public string GetText()
		{
			return label.GetText();
		}


		public override string ToString()
		{
			return string.Format("[TextButton] text: {0}", GetText());
		}
	}


	/// <summary>
	/// The style for a text button
	/// </summary>
	public class TextButtonStyle : ButtonStyle
	{
		public IFont Font;

		/** Optional. */
		public Color FontColor = Color.White;
		public Color? DownFontColor, OverFontColor, CheckedFontColor, CheckedOverFontColor, DisabledFontColor;
		public float FontScaleX = 1;
		public float FontScaleY = 1;
		public float FontScale { set { FontScaleX = value; FontScaleY = value; } }


		public TextButtonStyle()
		{
			Font = Graphics.Instance.BitmapFont;
		}


		public TextButtonStyle(IDrawable up, IDrawable down, IDrawable over, IFont font) : base(up, down, over)
		{
			Font = font ?? Graphics.Instance.BitmapFont;
		}


		public TextButtonStyle(IDrawable up, IDrawable down, IDrawable over) : this(up, down, over,
			Graphics.Instance.BitmapFont)
		{
		}


		public new static TextButtonStyle Create(Color upColor, Color downColor, Color overColor)
		{
			return new TextButtonStyle
			{
				Up = new PrimitiveDrawable(upColor),
				Down = new PrimitiveDrawable(downColor),
				Over = new PrimitiveDrawable(overColor)
			};
		}


		public new TextButtonStyle Clone()
		{
			return new TextButtonStyle
			{
				Up = Up,
				Down = Down,
				Over = Over,
				Checked = Checked,
				CheckedOver = CheckedOver,
				Disabled = Disabled,

				Font = Font,
				FontColor = FontColor,
				DownFontColor = DownFontColor,
				OverFontColor = OverFontColor,
				CheckedFontColor = CheckedFontColor,
				CheckedOverFontColor = CheckedOverFontColor,
				DisabledFontColor = DisabledFontColor,
				FontScaleX = FontScaleX,
				FontScaleY = FontScaleY,
			};
		}
	}
}