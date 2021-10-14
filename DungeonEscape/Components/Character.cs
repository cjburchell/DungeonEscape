using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class Character : Sprite
    {
        private string text;

        public Character(TmxObject tmxObject, TmxMap map) : base(tmxObject, map)
        {
            if (tmxObject.Properties.ContainsKey("Text"))
            {
                this.text = tmxObject.Properties["Text"];
            }
        }
    }
}