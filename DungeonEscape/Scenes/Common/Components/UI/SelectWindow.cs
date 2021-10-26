using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class SelectWindow<T> : BasicWindow where T : class
    {
        private Action<T> done;
        private ButtonList list;
        private ScrollPane scrollPane;

        public SelectWindow(UICanvas canvas, WindowInput input, string title, Point position, int width = 180,
            int height = 150) : base(canvas, input, title, position, width, height)
        {
        }

        public override void HideWindow()
        {
            this.HideWindow(null);
        }

        private void HideWindow(T selected)
        {
            base.HideWindow();
            this.Window.GetStage().SetGamepadFocusElement(null);
            this.done?.Invoke(selected);
        }
        
        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            
            this.list = new ButtonList();
            this.scrollPane = new ScrollPane(this.list, Skin);
            this.scrollPane.FillParent = true;
            this.Window.AddElement(this.scrollPane);
            this.list.OnClicked += button =>
            {
                this.HideWindow(button?.UserData as T);
            };
        }

        public override void DoAction()
        {
            this.HideWindow(this.list.GetSelected().UserData as T);
        }

        protected virtual Button CreateButton(T item)
        {
            return new TextButton(item.ToString(), Skin);
        }

        public void Show(IEnumerable<T> items, Action<T> doneAction)
        {
            this.done = doneAction;
            this.list.ClearChildren();
            this.list.SetFillParent(true);
            const int margin = 10;
            this.list.Top().PadLeft(margin).PadTop(margin).PadRight(margin);
            const int itemHeight = 30;
            var itemWidth = this.Window.GetWidth();
            var itemList = items.ToList();
            foreach (var item in itemList)
            {
                var button = this.CreateButton(item);
                button.UserData = item;
                this.list.Add(button).Width(itemWidth - margin * 2).Height(itemHeight);
            }

            this.Window.SetHeight(Math.Min( margin * 2 + itemList.Count * itemHeight, 400));
            this.scrollPane.Validate();
            this.ShowWindow();
        }
    }
}