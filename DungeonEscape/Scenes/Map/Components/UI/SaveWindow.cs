namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class SaveWindow: SelectWindow<GameSave>
    {
        public SaveWindow(UiSystem ui) : base(ui, "Select Save Slot", new Point(20, 20), 570)
        {
        }
        
        protected override Button CreateButton(GameSave save)
        {
            var table = new Table();
            var itemName = new Label(save.Name, Skin);
            if (!save.IsEmpty)
            {
                table.Add(itemName).Width(200).Left();
                if (save.Level != null)
                {
                    table.Add(new Label($"LV: {save.Level.Value}", Skin)).Width(100).Left();
                }

                if (save.Time != null)
                {
                    table.Add(new Label(save.Time.Value.ToString("g"), Skin)).Width(250).Left();
                }
            }
            else
            {
                table.Add(itemName).Width(550).Left();
            }
            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}