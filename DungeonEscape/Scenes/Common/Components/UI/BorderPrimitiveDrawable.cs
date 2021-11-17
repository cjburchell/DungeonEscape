namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;

    public class BorderPrimitiveDrawable : PrimitiveDrawable
    {
        private readonly Color? _borderColor;
        private readonly int _borderWidth;

        public BorderPrimitiveDrawable(Color? color = null, Color? borderColor = null, int borderWidth = 0) : base(color)
        {
            this._borderColor = borderColor;
            this._borderWidth = borderWidth;
        }

        public override void Draw(Batcher batcher, float x, float y, float width, float height, Color color)
        {
            base.Draw(batcher, x, y, width, height, color);

            if (this._borderColor.HasValue && this._borderWidth != 0)
            {
                batcher.DrawHollowRect(x, y, width, height, this._borderColor.Value, this._borderWidth);
            }
        }
    }
}