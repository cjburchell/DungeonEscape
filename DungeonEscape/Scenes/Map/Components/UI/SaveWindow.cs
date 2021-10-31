using DungeonEscape.Scenes.Common.Components.UI;
using Microsoft.Xna.Framework;
using Nez.UI;
using DungeonEscape.State;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class SaveWindow: SelectWindow<GameSave>
    {
        public SaveWindow(UISystem ui) : base(ui, "Save", new Point(20, 20), 300)
        {
        }
        
        protected override Button CreateButton(GameSave item)
        {
            var table = new Table();
            var itemName = new Label(item.Name, Skin).SetAlignment(Align.Left);
            table.Add(itemName).Width(150);
            if (item.Level.HasValue && item.Time.HasValue)
            {
                var level = new Label($"LV: {item.Level.Value}", Skin).SetAlignment(Align.Left);
                var time = new Label(item.Time.Value.ToString("g"), Skin).SetAlignment(Align.Left);
                table.Add(level).Width(30);
                table.Add(time).Width(100);
            }
            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}