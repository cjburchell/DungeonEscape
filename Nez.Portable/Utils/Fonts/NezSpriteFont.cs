using System;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Text;


namespace Nez
{
	using BitmapFonts;

#if !FNA
	public class NezSpriteFont : IFont
	{
		public float LineSpacing => _font.LineSpacing;
		public Padding Padding { get; } = new Padding();

		SpriteFont _font;
		readonly Dictionary<char, SpriteFont.Glyph> _glyphs;

		/// <summary>
		/// this sucker gets used a lot so we cache it to avoid having to create it every frame
		/// </summary>
		Matrix2D _transformationMatrix = Matrix2D.Identity;

		private readonly float _spaceWidth;


		public NezSpriteFont(SpriteFont font)
		{
			_font = font;
			_glyphs = font.GetGlyphs();
			_spaceWidth = this.MeasureString(this._font.DefaultCharacter.ToString()).X;
		}


		/// <summary>
		/// Returns the size of a string when rendered in this font.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <returns>The size, in pixels, of 'text' when rendered in
		/// this font.</returns>
		public Vector2 MeasureString(string text)
		{
			var source = new FontCharacterSource(text);
			Vector2 size;
			MeasureString(ref source, out size);
			return size;
		}


		/// <summary>
		/// Returns the size of the contents of a StringBuilder when
		/// rendered in this font.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <returns>The size, in pixels, of 'text' when rendered in
		/// this font.</returns>
		public Vector2 MeasureString(StringBuilder text)
		{
			var source = new FontCharacterSource(text);
			Vector2 size;
			MeasureString(ref source, out size);
			return size;
		}


		void MeasureString(ref FontCharacterSource text, out Vector2 size)
		{
			if (text.Length == 0)
			{
				size = Vector2.Zero;
				return;
			}

			// Get the default glyph here once.
			SpriteFont.Glyph? defaultGlyph = null;
			if (_font.DefaultCharacter.HasValue)
				defaultGlyph = _glyphs[_font.DefaultCharacter.Value];

			var width = 0.0f;
			var finalLineHeight = (float) _font.LineSpacing;

			
			var offset = Vector2.Zero;
			var firstGlyphOfLine = true;

			for (var i = 0; i < text.Length; ++i)
			{
				var c = text[i];

				if (c == '\r')
					continue;

				if (c == '\n')
				{
					finalLineHeight = _font.LineSpacing;

					offset.X = 0;
					offset.Y += _font.LineSpacing;
					firstGlyphOfLine = true;
					continue;
				}

				if (!_glyphs.TryGetValue(c, out var currentGlyph))
				{
					if (defaultGlyph.HasValue)
					{
						currentGlyph = defaultGlyph.Value;
					}
				}

				// The first character on a line might have a negative left side bearing.
				// In this scenario, SpriteBatch/SpriteFont normally offset the text to the right,
				//  so that text does not hang off the left side of its rectangle.
				if (firstGlyphOfLine)
				{
					offset.X = Math.Max(currentGlyph.LeftSideBearing, 0);
					firstGlyphOfLine = false;
				}
				else
				{
					offset.X += _font.Spacing + currentGlyph.LeftSideBearing;
				}

				offset.X += currentGlyph.Width;

				var proposedWidth = offset.X + Math.Max(currentGlyph.RightSideBearing, 0);
				if (proposedWidth > width)
					width = proposedWidth;

				offset.X += currentGlyph.RightSideBearing;

				if (currentGlyph.Cropping.Height > finalLineHeight)
					finalLineHeight = currentGlyph.Cropping.Height;
			}

			size.X = width;
			size.Y = offset.Y + finalLineHeight;
		}


		/// <summary>
		/// gets the BitmapFontRegion for the given char optionally substituting the default region if it isnt present.
		/// </summary>
		/// <returns><c>true</c>, if get font region for char was tryed, <c>false</c> otherwise.</returns>
		/// <param name="c">C.</param>
		/// <param name="fontRegion">Font region.</param>
		/// <param name="useDefaultRegionIfNotPresent">If set to <c>true</c> use default region if not present.</param>
		public bool TryGetFontRegionForChar(char c, out SpriteFont.Glyph fontGlyph,
		                                    bool useDefaultRegionIfNotPresent = false)
		{
			if (!_glyphs.TryGetValue(c, out fontGlyph))
			{
				if (useDefaultRegionIfNotPresent)
				{
					fontGlyph = _glyphs[_font.DefaultCharacter.Value];
					return true;
				}

				return false;
			}

			return true;
		}


		/// <summary>
		/// checks to see if a BitmapFontRegion exists for the char
		/// </summary>
		/// <returns><c>true</c>, if region exists for char was fonted, <c>false</c> otherwise.</returns>
		/// <param name="c">C.</param>
		public bool HasCharacter(char c)
		{
			SpriteFont.Glyph fontGlyph;
			return TryGetFontRegionForChar(c, out fontGlyph);
		}


		#region drawing

		void IFont.DrawInto(Batcher batcher, string text, Vector2 position, Color color,
		                    float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
		{
			var source = new FontCharacterSource(text);
			DrawInto(batcher, ref source, position, color, rotation, origin, scale, effect, depth);
		}


		void IFont.DrawInto(Batcher batcher, StringBuilder text, Vector2 position, Color color,
		                    float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
		{
			var source = new FontCharacterSource(text);
			DrawInto(batcher, ref source, position, color, rotation, origin, scale, effect, depth);
		}

		public float DefaultCharacterXAdvance => this.GetXAdvance(this._font.DefaultCharacter ?? 'A');

		public float GetXAdvance(char c)
		{
			return 0;
		}

		public float DefaultCharacterWidth => this.MeasureString(this._font.DefaultCharacter.ToString()).X;

		public string TruncateText(string text, string ellipsis, float maxLineWidth)
		{
			if (maxLineWidth < _spaceWidth)
				return string.Empty;

			var size = MeasureString(text);

			// do we even need to truncate?
			var ellipsisWidth = MeasureString(ellipsis).X;
			if (size.X > maxLineWidth)
			{
				var sb = new StringBuilder();

				var width = 0.0f;
				char? currentChar = null;
				var offsetX = 0.0f;

				// determine how many chars we can fit in maxLineWidth - ellipsisWidth
				for (var i = 0; i < text.Length; i++)
				{
					var c = text[i];

					// we don't deal with line breaks or tabs
					if (c == '\r' || c == '\n')
						continue;

					if (currentChar.HasValue)
						offsetX += this._font.Spacing + this.GetXAdvance(currentChar.Value);

					if (this._font.Characters.Contains(c))
						currentChar = c;
					else
						currentChar = this._font.DefaultCharacter;

					var proposedWidth = offsetX + this.GetXAdvance(currentChar.Value) + this._font.Spacing;
					if (proposedWidth > width)
						width = proposedWidth;

					if (width < maxLineWidth - ellipsisWidth)
					{
						sb.Append(c);
					}
					else
					{
						// no more room. append our ellipsis and get out of here
						sb.Append(ellipsis);
						break;
					}
				}

				return sb.ToString();
			}

			return text;
		}


		public string WrapText(string text, float maxLineWidth)
		{
			var words = text.Split(' ');
			var sb = new StringBuilder();
			var lineWidth = 0f;

			if (maxLineWidth < _spaceWidth)
				return string.Empty;

			foreach (var word in words)
			{
				var size = MeasureString(word);
				if (lineWidth + size.X < maxLineWidth)
				{
					sb.Append(word + " ");
					lineWidth += size.X + _spaceWidth;
				}
				else
				{
					if (size.X > maxLineWidth)
					{
						if (sb.ToString() == "")
							sb.Append(WrapText(word.Insert(word.Length / 2, " ") + " ", maxLineWidth));
						else
							sb.Append("\n" + WrapText(word.Insert(word.Length / 2, " ") + " ", maxLineWidth));
					}
					else
					{
						sb.Append("\n" + word + " ");
						lineWidth = size.X + _spaceWidth;
					}
				}
			}

			return sb.ToString();
		}


		public void DrawInto(Batcher batcher, ref FontCharacterSource text, Vector2 position, Color color,
		                     float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
		{
			var flipAdjustment = Vector2.Zero;

			var flippedVert = (effect & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically;
			var flippedHorz = (effect & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally;

			if (flippedVert || flippedHorz)
			{
				Vector2 size;
				MeasureString(ref text, out size);

				if (flippedHorz)
				{
					origin.X *= -1;
					flipAdjustment.X = -size.X;
				}

				if (flippedVert)
				{
					origin.Y *= -1;
					flipAdjustment.Y = _font.LineSpacing - size.Y;
				}
			}

			// TODO: This looks excessive... i suspect we could do most of this with simple vector math and avoid this much matrix work.
			var requiresTransformation = flippedHorz || flippedVert || rotation != 0f || scale != Vector2.One;
			if (requiresTransformation)
			{
				Matrix2D temp;
				Matrix2D.CreateTranslation(-origin.X, -origin.Y, out _transformationMatrix);
				Matrix2D.CreateScale((flippedHorz ? -scale.X : scale.X), (flippedVert ? -scale.Y : scale.Y), out temp);
				Matrix2D.Multiply(ref _transformationMatrix, ref temp, out _transformationMatrix);
				Matrix2D.CreateTranslation(flipAdjustment.X, flipAdjustment.Y, out temp);
				Matrix2D.Multiply(ref temp, ref _transformationMatrix, out _transformationMatrix);
				Matrix2D.CreateRotation(rotation, out temp);
				Matrix2D.Multiply(ref _transformationMatrix, ref temp, out _transformationMatrix);
				Matrix2D.CreateTranslation(position.X, position.Y, out temp);
				Matrix2D.Multiply(ref _transformationMatrix, ref temp, out _transformationMatrix);
			}

			// Get the default glyph here once.
			SpriteFont.Glyph? defaultGlyph = null;
			if (_font.DefaultCharacter.HasValue)
				defaultGlyph = _glyphs[_font.DefaultCharacter.Value];

			var offset = requiresTransformation ? Vector2.Zero : position - origin;
			var firstGlyphOfLine = true;

			for (var i = 0; i < text.Length; ++i)
			{
				var c = text[i];

				if (c == '\r')
					continue;

				if (c == '\n')
				{
					offset.X = requiresTransformation ? 0f : position.X - origin.X;
					offset.Y += _font.LineSpacing;
					firstGlyphOfLine = true;
					continue;
				}

				if (!_glyphs.TryGetValue(c, out var currentGlyph))
				{
					if (defaultGlyph.HasValue)
					{
						currentGlyph = defaultGlyph.Value;
					}
				}

				// The first character on a line might have a negative left side bearing.
				// In this scenario, SpriteBatch/SpriteFont normally offset the text to the right,
				// so that text does not hang off the left side of its rectangle.
				if (firstGlyphOfLine)
				{
					offset.X += Math.Max(currentGlyph.LeftSideBearing, 0);
					firstGlyphOfLine = false;
				}
				else
				{
					offset.X += _font.Spacing + currentGlyph.LeftSideBearing;
				}

				var p = offset;

				if (flippedHorz)
					p.X += currentGlyph.BoundsInTexture.Width;
				p.X += currentGlyph.Cropping.X;

				if (flippedVert)
					p.Y += currentGlyph.BoundsInTexture.Height - _font.LineSpacing;
				p.Y += currentGlyph.Cropping.Y;

				// transform our point if we need to
				if (requiresTransformation)
					Vector2Ext.Transform(ref p, ref _transformationMatrix, out p);

				var destRect = RectangleExt.FromFloats(p.X, p.Y,
					currentGlyph.BoundsInTexture.Width * scale.X,
					currentGlyph.BoundsInTexture.Height * scale.Y);

				batcher.Draw(_font.Texture, destRect, currentGlyph.BoundsInTexture, color, rotation, Vector2.Zero,
					effect, depth);

				offset.X += currentGlyph.Width + currentGlyph.RightSideBearing;
			}
		}

		#endregion
	}

#else
	public class NezSpriteFont : IFont
	{
		public float LineSpacing { get { return _font.LineSpacing; } }

		SpriteFont _font;

		/// <summary>
		/// this sucker gets used a lot so we cache it to avoid having to create it every frame
		/// </summary>
		#pragma warning disable 0414
		Matrix _transformationMatrix = Matrix.Identity;
		#pragma warning restore 0414


		public NezSpriteFont( SpriteFont font )
		{
			_font = font;
		}


		public void DrawInto( Batcher batcher, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth )
		{
			var source = new FontCharacterSource( text );
			DrawInto( batcher, ref source, position, color, rotation, origin, scale, effect, depth );
		}


		public void DrawInto( Batcher batcher, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth )
		{
			var source = new FontCharacterSource( text );
			DrawInto( batcher, ref source, position, color, rotation, origin, scale, effect, depth );
		}


		public void DrawInto( Batcher batcher, ref FontCharacterSource text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth )
		{
			throw new NotImplementedException();
		}


		bool IFont.HasCharacter( char c )
		{
			throw new NotImplementedException();
		}


		public Vector2 MeasureString( StringBuilder text )
		{
			return _font.MeasureString( text );
		}


		public Vector2 MeasureString( string text )
		{
			return _font.MeasureString( text );
		}

	}

#endif
}