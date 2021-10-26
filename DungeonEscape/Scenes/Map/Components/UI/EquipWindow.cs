using System;
using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class EquipWindow : BasicWindow
    {
        private Hero hero;
        private Action done;

        public EquipWindow(UICanvas canvas, WindowInput input) : base(canvas, input, "Equip", new Point(150,30),300,150)
        {
        }

        public override void HideWindow()
        {
            base.HideWindow();
            this.done?.Invoke();
        }

        public void Show(Hero heroToEquip, Action doneAction)
        {
            this.hero = heroToEquip;
            this.done = doneAction;
            this.ShowWindow();
        }
    }
}