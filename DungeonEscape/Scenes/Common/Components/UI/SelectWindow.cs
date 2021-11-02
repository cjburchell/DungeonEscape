using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Nez.UI;

namespace DungeonEscape.Scenes.Common.Components.UI
{
    public class SelectWindow<T> : BasicWindow where T : class
    {
        private Action<T> done;
        private ButtonList list;
        private ScrollPane scrollPane;
        private IEnumerable<T> items;


        public SelectWindow(UISystem ui, string title, Point position, int width = 180,
            int height = 150) : base(ui, title, position, width, height)
        {
            this.list = new ButtonList();
            this.scrollPane = new ScrollPane(this.list, Skin) {FillParent = true};
        }

        public override void CloseWindow(bool remove = true)
        {
            this.CloseWindow(null, remove);
        }

        private void CloseWindow(T result, bool remove = true)
        {
            base.CloseWindow(remove);
            this.done?.Invoke(result);
        }
        
        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.Window.AddElement(this.scrollPane);
            this.list.OnClicked += button =>
            {
                this.CloseWindow(button?.UserData as T);
            };
            
            this.list.SetFillParent(true);
            const int margin = 10;
            this.list.Top().PadLeft(margin).PadTop(margin).PadRight(margin);
            var itemWidth = this.Window.GetWidth();
            var itemList = this.items.ToList();
            foreach (var item in itemList)
            {
                var button = this.CreateButton(item);
                button.UserData = item;
                this.list.Add(button).Width(itemWidth - margin * 2).Height(ButtonHeight);
            }

            this.Window.SetHeight(Math.Min( margin * 2 + itemList.Count * ButtonHeight, 400));
            this.scrollPane.Validate();
        }

        public override void DoAction()
        {
            this.CloseWindow(this.list.GetSelected()?.UserData as T);
        }

        protected virtual Button CreateButton(T item)
        {
            var  button = new TextButton(item.ToString(), Skin, "no_border");
            button.GetLabel().SetAlignment(Align.Left);
            return button;
        }

        public void Show(IEnumerable<T> items, Action<T> doneAction)
        {
            this.done = doneAction;
            this.items = items;
            this.ShowWindow();
        }
    }
}