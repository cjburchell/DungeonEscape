using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.UI;

namespace DungeonEscape.Components
{
    public class CommandMenu: Component, IUpdatable
    {
        private readonly UICanvas canvas;
        private readonly Player player;

        public CommandMenu(UICanvas canvas, Player player)
        {
            this.canvas = canvas;
            this.player = player;
        }
        
        private VirtualButton showMenuInput;
        private TextButton statusButton;
        private Window commandWindow;
        private TextButton spellButton;
        private TextButton itemButton;
        private TextButton equipButton;

        public override void OnAddedToEntity()
        {
            var windowStyle = Skin.CreateDefaultSkin();

            const int FontScale = 2;    
            commandWindow = new Window("Command", windowStyle);
            canvas.Stage.AddElement(commandWindow);
            commandWindow.SetPosition(30, 30);
            commandWindow.SetWidth(100);
            commandWindow.SetHeight(150);
            commandWindow.SetMovable(false);
            commandWindow.SetResizable(false);
            commandWindow.GetTitleLabel().SetFontScale(FontScale);
            this.commandWindow.SetVisible(false);

            var commandTable = commandWindow.AddElement(new Table());
            
            var buttonStyle = TextButtonStyle.Create(Color.Black, Color.Aqua, Color.Gray);
            statusButton =new TextButton("Status", buttonStyle);
            statusButton.GetLabel().SetFontScale(FontScale);
            statusButton.OnClicked += _ =>
            {
                this.HideWindow();
            };
            spellButton =new TextButton("Spell",buttonStyle);
            spellButton.GetLabel().SetFontScale(FontScale);
            spellButton.OnClicked += _ =>
            {
                this.HideWindow();
            };
            
            itemButton = new TextButton("Item", buttonStyle);
            itemButton.GetLabel().SetFontScale(FontScale);
            itemButton.OnClicked += _ =>
            {
                this.HideWindow();
            };
            equipButton = new TextButton("Equip", buttonStyle);
            equipButton.GetLabel().SetFontScale(FontScale);
            equipButton.OnClicked += _ =>
            {
                this.HideWindow();
            };

            //commandWindow.DebugAll();
            //commandTable.DebugAll();
            
            // layout
            commandTable.SetFillParent(true);
            commandTable.Top().PadLeft(10).PadTop(10).PadRight(10);
            commandTable.Add(statusButton).Height(30).Width(80);
            commandTable.Row().SetPadTop(0);
            commandTable.Add(spellButton).Height(30).Width(80);
            commandTable.Row().SetPadTop(0);
            commandTable.Add(itemButton).Height(30).Width(80);
            commandTable.Row().SetPadTop(0);
            commandTable.Add(equipButton).Height(30).Width(80);

            this.showMenuInput = new VirtualButton();
            this.showMenuInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Enter));
            this.showMenuInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.B));
            
            base.OnAddedToEntity();
        }

        private void HideWindow()
        {
            this.commandWindow.SetVisible(false);
            this.commandWindow.GetStage().SetGamepadFocusElement(null);
            statusButton.GamepadDownElement = null;
            spellButton.GamepadDownElement = null;
            itemButton.GamepadDownElement = null;
            equipButton.GamepadDownElement = null;
            statusButton.GamepadUpElement = null;
            spellButton.GamepadUpElement = null;
            itemButton.GamepadUpElement = null;
            equipButton.GamepadUpElement = null;
            this.player.IsControllable = true;
        }

        private void ShowWindow()
        {
            this.commandWindow.SetVisible(true);
            this.commandWindow.GetStage().SetGamepadFocusElement(this.statusButton);
            this.commandWindow.GetStage().GamepadActionButton = Buttons.A;
            statusButton.GamepadDownElement = spellButton;
            spellButton.GamepadDownElement = itemButton;
            itemButton.GamepadDownElement = equipButton;
            equipButton.GamepadDownElement = statusButton;
            statusButton.GamepadUpElement = equipButton;
            spellButton.GamepadUpElement = statusButton;
            itemButton.GamepadUpElement = spellButton;
            equipButton.GamepadUpElement = itemButton;
            this.player.IsControllable = false;
        }

        public override void OnRemovedFromEntity()
        {
            this.showMenuInput.Deregister();
        }

        public void Update()
        {
            if (!this.showMenuInput.IsPressed)
            {
                return;
            }

            if (!this.commandWindow.IsVisible())
            {
                this.ShowWindow();
            }
            else
            {
                this.HideWindow();
            }
        }
    }
}