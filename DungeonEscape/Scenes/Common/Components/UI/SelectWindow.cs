﻿namespace Redpoint.DungeonEscape.Scenes.Common.Components.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Nez.UI;

    public class SelectWindow<T> : BasicWindow where T : class
    {
        private readonly int _buttonHeight;
        private readonly int _maxHeight;
        private Action<T> _done;
        private readonly ButtonList _list;
        private IEnumerable<T> _items;
        private readonly string _title;


        public SelectWindow(UiSystem ui, string title, Point position, int width = 180, int buttonHeight = ButtonHeight, int maxHeight = 500)
            : base(ui, position, width, 150)
        {
            this._title = title;
            this._buttonHeight = buttonHeight;
            _maxHeight = maxHeight;
            this._list = new ButtonList(ui.Sounds);
            
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
            var itemWidth = this.Window.GetWidth();
            const int margin = 10;
            var itemList = this._items.ToList();
            this._list.OnClicked += button =>
            {
                this.Ui.Sounds.PlaySoundEffect("confirm");
                this.CloseWindow(button?.UserData as T);
            };
            
            var scrollPane = new ScrollPane(this._list, Skin);
            var table = new Table();

            if (!string.IsNullOrEmpty(this._title))
            {
                table.Add(new Label(_title, Skin)).Left();
                table.Row();
            }
            
            table.Add(scrollPane);
            table.SetFillParent(true);
            this.Window.AddElement(table);
            
            //this._list.Top().Right();
            //this._list.SetFillParent(true);
            foreach (var item in itemList)
            {
                var button = this.CreateButton(item);
                button.UserData = item;
                this._list.Add(button).Width(itemWidth - margin * 2).Height(this._buttonHeight);
            }
            

            
            //this.Window.SetHeight(Math.Min( margin * 2 + itemList.Count * this._buttonHeight + (this._isTitleSet?this._buttonHeight:0), _maxHeight));
            this.Window.SetHeight(Math.Min( margin * 2 + itemList.Count*this._buttonHeight + ( !string.IsNullOrEmpty(this._title)? 12 : 0 ), _maxHeight));
            
            scrollPane.Validate();
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