namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Nez.UI;

    public class SelectWindow<T> : BasicWindow where T : class
    {
        private readonly int _buttonHeight;
        private Action<T> _done;
        private readonly ButtonList _list;
        private readonly ScrollPane _scrollPane;
        private IEnumerable<T> _items;


        public SelectWindow(UiSystem ui, string title, Point position, int width = 180, int buttonHeight = ButtonHeight)
            : base(ui, title, position, width, 150)
        {
            this._buttonHeight = buttonHeight;
            this._list = new ButtonList(ui.Sounds);
            this._scrollPane = new ScrollPane(this._list, Skin) { FillParent = true };
        }

        public override void CloseWindow(bool remove = true)
        {
            this.CloseWindow(null, remove);
        }

        private void CloseWindow(T result, bool remove = true)
        {
            base.CloseWindow(remove);
            this._done?.Invoke(result);
        }
        
        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.Window.AddElement(this._scrollPane);
            this._list.OnClicked += button =>
            {
                this.Ui.Sounds.PlaySoundEffect("confirm");
                this.CloseWindow(button?.UserData as T);
            };
            
            this._list.SetFillParent(true);
            const int margin = 10;
            this._list.Top().PadLeft(margin).PadTop(margin).PadRight(margin);
            var itemWidth = this.Window.GetWidth();
            var itemList = this._items.ToList();
            foreach (var item in itemList)
            {
                var button = this.CreateButton(item);
                button.UserData = item;
                this._list.Add(button).Width(itemWidth - margin * 2).Height(this._buttonHeight);
            }

            this.Window.SetHeight(Math.Min( margin * 2 + itemList.Count * this._buttonHeight, 400));
            this._scrollPane.Validate();
        }

        public override void DoAction()
        {
            this.CloseWindow(this._list.GetSelected()?.UserData as T);
        }

        protected virtual Button CreateButton(T item)
        {
            var  button = new TextButton(item.ToString(), Skin, "no_border");
            button.GetLabel().SetAlignment(Align.Left);
            return button;
        }

        public void Show(IEnumerable<T> itemsList, Action<T> doneAction)
        {
            this._done = doneAction;
            this._items = itemsList;
            this.ShowWindow();
        }
    }
}