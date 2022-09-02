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
        
        public BorderPrimitiveDrawable(float minSize, Color color, Color? borderColor = null, int borderWidth = 0) : base(minSize, color)
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

    public class SelectDrawable : BorderPrimitiveDrawable
    {
        private readonly Color? _selectColor;
        private readonly int _selectOffset;

        public SelectDrawable(int selectOffset, Color? color = null, Color? selectColor=null, Color? borderColor = null, int borderWidth = 0) : base(color, borderColor, borderWidth)
        {
            _selectOffset = selectOffset;
            _selectColor = selectColor;
        }
        
        public SelectDrawable(int selectOffset, float minSize, Color color, Color selectColor, Color? borderColor = null, int borderWidth = 0) : base(minSize, color, borderColor, borderWidth)
        {
            _selectOffset = selectOffset;
            _selectColor = selectColor;
        }

        public override void Draw(Batcher batcher, float x, float y, float width, float height, Color color)
        {
            base.Draw(batcher, x, y, width, height, color);

            if (this._selectColor.HasValue && this._selectOffset != 0)
            {
                batcher.DrawRect(x+this._selectOffset, y+this._selectOffset, width - (this._selectOffset*2), height- (this._selectOffset*2), this._selectColor.Value);
            }
        }
    }
}