namespace DungeonEscape.Scenes.Common.Components
{
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;

    public class BorderPrimitiveDrawable : PrimitiveDrawable
    {
        private readonly Color? borderColor;
        private readonly int borderWidth;

        public BorderPrimitiveDrawable(Color? color = null, Color? borderColor = null, int borderWidth = 0) : base(color)
        {
            this.borderColor = borderColor;
            this.borderWidth = borderWidth;
        }

        public override void Draw(Batcher batcher, float x, float y, float width, float height, Color color)
        {
            base.Draw(batcher, x, y, width, height, color);

            if (this.borderColor.HasValue && this.borderWidth != 0)
            {
                batcher.DrawHollowRect(x, y, width, height, this.borderColor.Value, this.borderWidth);
            }
        }
    }
}