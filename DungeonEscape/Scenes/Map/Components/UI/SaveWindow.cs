namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class SaveWindow: SelectWindow<GameSave>
    {
        public SaveWindow(UiSystem ui) : base(ui, "Save", new Point(20, 20), 570)
        {
        }
        
        protected override Button CreateButton(GameSave save)
        {
            var table = new Table();
            var itemName = new Label(save.Name, Skin).SetAlignment(Align.Left);
            table.Add(itemName).Width(200);
            if (!save.IsEmpty)
            {
                if (save.Level != null)
                {
                    var level = new Label($"LV: {save.Level.Value}", Skin).SetAlignment(Align.Left);
                    table.Add(level).Width(100);
                }

                if (save.Time != null)
                {
                    var time = new Label(save.Time.Value.ToString("g"), Skin).SetAlignment(Align.Left);
                    table.Add(time).Width(250);
                }
            }
            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}