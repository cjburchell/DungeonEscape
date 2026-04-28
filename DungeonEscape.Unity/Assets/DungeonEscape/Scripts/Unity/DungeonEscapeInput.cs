using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Redpoint.DungeonEscape.Unity
{
    public enum DungeonEscapeInputCommand
    {
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Interact,
        Cancel,
        Sprint,
        MenuParty,
        MenuInventory,
        MenuQuests,
        MenuSettings,
        MenuPreviousTab,
        MenuNextTab,
        QuickSave,
        QuickLoad,
        Restart,
        ReloadMap
    }

    public static class DungeonEscapeInput
    {
        private const float AxisDeadZone = 0.35f;

        private static readonly Dictionary<DungeonEscapeInputCommand, InputBinding> DefaultBindings =
            new Dictionary<DungeonEscapeInputCommand, InputBinding>
            {
                { DungeonEscapeInputCommand.MoveLeft, Binding(DungeonEscapeInputCommand.MoveLeft, "LeftArrow", "A", "DpadLeft") },
                { DungeonEscapeInputCommand.MoveRight, Binding(DungeonEscapeInputCommand.MoveRight, "RightArrow", "D", "DpadRight") },
                { DungeonEscapeInputCommand.MoveUp, Binding(DungeonEscapeInputCommand.MoveUp, "UpArrow", "W", "DpadUp") },
                { DungeonEscapeInputCommand.MoveDown, Binding(DungeonEscapeInputCommand.MoveDown, "DownArrow", "S", "DpadDown") },
                { DungeonEscapeInputCommand.Interact, Binding(DungeonEscapeInputCommand.Interact, "Space", "Enter", "South") },
                { DungeonEscapeInputCommand.Cancel, Binding(DungeonEscapeInputCommand.Cancel, "Escape", "None", "East") },
                { DungeonEscapeInputCommand.Sprint, Binding(DungeonEscapeInputCommand.Sprint, "LeftShift", "RightShift", "North") },
                { DungeonEscapeInputCommand.MenuParty, Binding(DungeonEscapeInputCommand.MenuParty, "C", "None", "West") },
                { DungeonEscapeInputCommand.MenuInventory, Binding(DungeonEscapeInputCommand.MenuInventory, "I", "None", "None") },
                { DungeonEscapeInputCommand.MenuQuests, Binding(DungeonEscapeInputCommand.MenuQuests, "J", "None", "None") },
                { DungeonEscapeInputCommand.MenuSettings, Binding(DungeonEscapeInputCommand.MenuSettings, "O", "None", "None") },
                { DungeonEscapeInputCommand.MenuPreviousTab, Binding(DungeonEscapeInputCommand.MenuPreviousTab, "LeftBracket", "None", "LeftShoulder") },
                { DungeonEscapeInputCommand.MenuNextTab, Binding(DungeonEscapeInputCommand.MenuNextTab, "RightBracket", "None", "RightShoulder") },
                { DungeonEscapeInputCommand.QuickSave, Binding(DungeonEscapeInputCommand.QuickSave, "F6", "None", "None") },
                { DungeonEscapeInputCommand.QuickLoad, Binding(DungeonEscapeInputCommand.QuickLoad, "F9", "None", "None") },
                { DungeonEscapeInputCommand.Restart, Binding(DungeonEscapeInputCommand.Restart, "F10", "None", "None") },
                { DungeonEscapeInputCommand.ReloadMap, Binding(DungeonEscapeInputCommand.ReloadMap, "F5", "None", "Start") }
            };

        public static bool GetCommandDown(DungeonEscapeInputCommand command)
        {
            var binding = GetBinding(command);
            return GetKeyDown(binding.Primary) || GetKeyDown(binding.Secondary) || GetKeyDown(binding.Gamepad);
        }

        public static bool GetCommand(DungeonEscapeInputCommand command)
        {
            var binding = GetBinding(command);
            return GetKey(binding.Primary) || GetKey(binding.Secondary) || GetKey(binding.Gamepad);
        }

        public static int GetMoveX()
        {
            if (GetCommand(DungeonEscapeInputCommand.MoveLeft))
            {
                return -1;
            }

            if (GetCommand(DungeonEscapeInputCommand.MoveRight))
            {
                return 1;
            }

            var axis = GetGamepadStickX();
            if (axis < -AxisDeadZone)
            {
                return -1;
            }

            return axis > AxisDeadZone ? 1 : 0;
        }

        public static int GetMoveY()
        {
            if (GetCommand(DungeonEscapeInputCommand.MoveUp))
            {
                return -1;
            }

            if (GetCommand(DungeonEscapeInputCommand.MoveDown))
            {
                return 1;
            }

            var axis = GetGamepadStickY();
            if (axis > AxisDeadZone)
            {
                return -1;
            }

            return axis < -AxisDeadZone ? 1 : 0;
        }

        public static int GetMoveXDown()
        {
            if (GetCommandDown(DungeonEscapeInputCommand.MoveLeft))
            {
                return -1;
            }

            if (GetCommandDown(DungeonEscapeInputCommand.MoveRight))
            {
                return 1;
            }

            return 0;
        }

        public static int GetMoveYDown()
        {
            if (GetCommandDown(DungeonEscapeInputCommand.MoveUp))
            {
                return -1;
            }

            if (GetCommandDown(DungeonEscapeInputCommand.MoveDown))
            {
                return 1;
            }

            return 0;
        }

        public static InputBinding[] GetBindings()
        {
            EnsureBindings(DungeonEscapeSettingsCache.Current);
            return DungeonEscapeSettingsCache.Current.InputBindings;
        }

        public static string GetBindingText(InputBinding binding)
        {
            if (binding == null)
            {
                return "";
            }

            return FormatCode(binding.Primary) + " / " + FormatCode(binding.Secondary) + " / " + FormatCode(binding.Gamepad);
        }

        public static bool TryCaptureBinding(out string keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                foreach (var key in keyboard.allKeys)
                {
                    if (key.wasPressedThisFrame)
                    {
                        keyCode = FormatKeyboardKey(key.keyCode);
                        return true;
                    }
                }
            }

            var gamepadCode = TryCaptureGamepadButton();
            if (!string.IsNullOrEmpty(gamepadCode))
            {
                keyCode = gamepadCode;
                return true;
            }

            keyCode = null;
            return false;
        }

        public static void SetBinding(InputBinding binding, string slot, string keyCode)
        {
            if (binding == null || string.IsNullOrEmpty(slot))
            {
                return;
            }

            if (slot == "Primary")
            {
                binding.Primary = keyCode;
            }
            else if (slot == "Secondary")
            {
                binding.Secondary = keyCode;
            }
            else if (slot == "Gamepad")
            {
                binding.Gamepad = keyCode;
            }
        }

        public static void ResetBindings()
        {
            var settings = DungeonEscapeSettingsCache.Current;
            if (settings == null)
            {
                return;
            }

            settings.InputBindings = DefaultBindings.Values.Select(CloneBinding).ToArray();
            DungeonEscapeSettingsCache.Save();
        }

        public static void EnsureBindings(Settings settings)
        {
            if (settings == null)
            {
                return;
            }

            var existing = settings.InputBindings == null
                ? new List<InputBinding>()
                : settings.InputBindings.Where(binding => binding != null && !string.IsNullOrEmpty(binding.Command)).ToList();

            foreach (var defaultBinding in DefaultBindings.Values)
            {
                if (existing.Any(binding => string.Equals(binding.Command, defaultBinding.Command, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                existing.Add(CloneBinding(defaultBinding));
            }

            settings.InputBindings = existing.ToArray();
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MoveLeft, "JoystickButton14", "DpadLeft");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MoveRight, "JoystickButton15", "DpadRight");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MoveUp, "JoystickButton13", "DpadUp");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MoveDown, "JoystickButton12", "DpadDown");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.Interact, "JoystickButton0", "South");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.Cancel, "JoystickButton1", "East");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.Sprint, "JoystickButton5", "North");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.Sprint, "JoystickButton3", "North");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuParty, "JoystickButton3", "West");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuParty, "JoystickButton2", "West");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuInventory, "JoystickButton2", "None");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuQuests, "JoystickButton6", "None");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuSettings, "JoystickButton7", "None");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuPreviousTab, "JoystickButton4", "LeftShoulder");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuNextTab, "JoystickButton5", "RightShoulder");
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.ReloadMap, "JoystickButton8", "Start");
        }

        private static InputBinding GetBinding(DungeonEscapeInputCommand command)
        {
            EnsureBindings(DungeonEscapeSettingsCache.Current);
            var commandName = command.ToString();
            var binding = DungeonEscapeSettingsCache.Current.InputBindings.FirstOrDefault(
                item => string.Equals(item.Command, commandName, StringComparison.OrdinalIgnoreCase));
            return binding ?? DefaultBindings[command];
        }

        private static void UpdateLegacyGamepadBinding(
            Settings settings,
            DungeonEscapeInputCommand command,
            string oldValue,
            string newValue)
        {
            var commandName = command.ToString();
            var binding = settings.InputBindings.FirstOrDefault(
                item => string.Equals(item.Command, commandName, StringComparison.OrdinalIgnoreCase));
            if (binding == null || binding.Gamepad != oldValue)
            {
                return;
            }

            binding.Gamepad = newValue;
        }

        private static bool GetKey(string keyCode)
        {
            var keyboardKey = GetKeyboardKey(keyCode);
            if (keyboardKey != null)
            {
                return keyboardKey.isPressed;
            }

            var gamepadButton = GetGamepadButton(keyCode);
            if (gamepadButton != null && gamepadButton.isPressed)
            {
                return true;
            }

            return GetLegacyKey(keyCode);
        }

        private static bool GetKeyDown(string keyCode)
        {
            var keyboardKey = GetKeyboardKey(keyCode);
            if (keyboardKey != null)
            {
                return keyboardKey.wasPressedThisFrame;
            }

            var gamepadButton = GetGamepadButton(keyCode);
            if (gamepadButton != null && gamepadButton.wasPressedThisFrame)
            {
                return true;
            }

            return GetLegacyKeyDown(keyCode);
        }

        private static KeyControl GetKeyboardKey(string keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || string.IsNullOrEmpty(keyCode) || string.Equals(keyCode, "None", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Key key;
            if (!Enum.TryParse(NormalizeKeyboardKey(keyCode), true, out key))
            {
                return null;
            }

            return keyboard[key];
        }

        private static ButtonControl GetGamepadButton(string keyCode)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null || string.IsNullOrEmpty(keyCode) || string.Equals(keyCode, "None", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            switch (NormalizeGamepadButton(keyCode))
            {
                case "South":
                    return gamepad.buttonSouth;
                case "East":
                    return gamepad.buttonEast;
                case "West":
                    return gamepad.buttonWest;
                case "North":
                    return gamepad.buttonNorth;
                case "LeftShoulder":
                    return gamepad.leftShoulder;
                case "RightShoulder":
                    return gamepad.rightShoulder;
                case "LeftStickPress":
                    return gamepad.leftStickButton;
                case "RightStickPress":
                    return gamepad.rightStickButton;
                case "Start":
                    return gamepad.startButton;
                case "Select":
                    return gamepad.selectButton;
                case "DpadLeft":
                    return gamepad.dpad.left;
                case "DpadRight":
                    return gamepad.dpad.right;
                case "DpadUp":
                    return gamepad.dpad.up;
                case "DpadDown":
                    return gamepad.dpad.down;
                default:
                    return null;
            }
        }

        private static float GetGamepadStickX()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                return gamepad.leftStick.ReadValue().x;
            }

            return GetLegacyAxis("Horizontal");
        }

        private static float GetGamepadStickY()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                return gamepad.leftStick.ReadValue().y;
            }

            return GetLegacyAxis("Vertical");
        }

        private static string TryCaptureGamepadButton()
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return null;
            }

            if (gamepad.buttonSouth.wasPressedThisFrame) return "South";
            if (gamepad.buttonEast.wasPressedThisFrame) return "East";
            if (gamepad.buttonWest.wasPressedThisFrame) return "West";
            if (gamepad.buttonNorth.wasPressedThisFrame) return "North";
            if (gamepad.leftShoulder.wasPressedThisFrame) return "LeftShoulder";
            if (gamepad.rightShoulder.wasPressedThisFrame) return "RightShoulder";
            if (gamepad.leftStickButton.wasPressedThisFrame) return "LeftStickPress";
            if (gamepad.rightStickButton.wasPressedThisFrame) return "RightStickPress";
            if (gamepad.startButton.wasPressedThisFrame) return "Start";
            if (gamepad.selectButton.wasPressedThisFrame) return "Select";
            if (gamepad.dpad.left.wasPressedThisFrame) return "DpadLeft";
            if (gamepad.dpad.right.wasPressedThisFrame) return "DpadRight";
            if (gamepad.dpad.up.wasPressedThisFrame) return "DpadUp";
            return gamepad.dpad.down.wasPressedThisFrame ? "DpadDown" : null;
        }

        private static bool GetLegacyKey(string keyCode)
        {
            KeyCode code;
            return TryParseLegacyKeyCode(keyCode, out code) && code != KeyCode.None && Input.GetKey(code);
        }

        private static bool GetLegacyKeyDown(string keyCode)
        {
            KeyCode code;
            return TryParseLegacyKeyCode(keyCode, out code) && code != KeyCode.None && Input.GetKeyDown(code);
        }

        private static float GetLegacyAxis(string axisName)
        {
            try
            {
                return Input.GetAxisRaw(axisName);
            }
            catch (ArgumentException)
            {
                return 0f;
            }
        }

        private static bool TryParseLegacyKeyCode(string keyCode, out KeyCode code)
        {
            if (string.IsNullOrEmpty(keyCode) || string.Equals(keyCode, "None", StringComparison.OrdinalIgnoreCase))
            {
                code = KeyCode.None;
                return false;
            }

            var normalized = NormalizeLegacyKeyCode(keyCode);
            return Enum.TryParse(normalized, true, out code);
        }

        private static InputBinding Binding(DungeonEscapeInputCommand command, string primary, string secondary, string gamepad)
        {
            return new InputBinding
            {
                Command = command.ToString(),
                Primary = primary,
                Secondary = secondary,
                Gamepad = gamepad
            };
        }

        private static InputBinding CloneBinding(InputBinding binding)
        {
            return new InputBinding
            {
                Command = binding.Command,
                Primary = binding.Primary,
                Secondary = binding.Secondary,
                Gamepad = binding.Gamepad
            };
        }

        private static string FormatCode(string code)
        {
            return string.IsNullOrEmpty(code) || string.Equals(code, "None", StringComparison.OrdinalIgnoreCase) ? "-" : code;
        }

        private static string NormalizeKeyboardKey(string keyCode)
        {
            switch (keyCode)
            {
                case "Return":
                    return "Enter";
                case "Alpha0":
                    return "Digit0";
                case "Alpha1":
                    return "Digit1";
                case "Alpha2":
                    return "Digit2";
                case "Alpha3":
                    return "Digit3";
                case "Alpha4":
                    return "Digit4";
                case "Alpha5":
                    return "Digit5";
                case "Alpha6":
                    return "Digit6";
                case "Alpha7":
                    return "Digit7";
                case "Alpha8":
                    return "Digit8";
                case "Alpha9":
                    return "Digit9";
                default:
                    return keyCode;
            }
        }

        private static string NormalizeGamepadButton(string keyCode)
        {
            switch (keyCode)
            {
                case "JoystickButton0":
                    return "South";
                case "JoystickButton1":
                    return "East";
                case "JoystickButton2":
                    return "West";
                case "JoystickButton3":
                    return "North";
                case "JoystickButton4":
                    return "LeftShoulder";
                case "JoystickButton5":
                    return "RightShoulder";
                case "JoystickButton6":
                    return "Select";
                case "JoystickButton7":
                    return "Start";
                case "JoystickButton8":
                    return "Start";
                case "JoystickButton12":
                    return "DpadDown";
                case "JoystickButton13":
                    return "DpadUp";
                case "JoystickButton14":
                    return "DpadLeft";
                case "JoystickButton15":
                    return "DpadRight";
                default:
                    return keyCode;
            }
        }

        private static string NormalizeLegacyKeyCode(string keyCode)
        {
            switch (keyCode)
            {
                case "Enter":
                    return "Return";
                case "South":
                    return "JoystickButton0";
                case "East":
                    return "JoystickButton1";
                case "West":
                    return "JoystickButton2";
                case "North":
                    return "JoystickButton3";
                case "LeftShoulder":
                    return "JoystickButton4";
                case "RightShoulder":
                    return "JoystickButton5";
                case "Select":
                    return "JoystickButton6";
                case "Start":
                    return "JoystickButton7";
                case "DpadDown":
                    return "JoystickButton12";
                case "DpadUp":
                    return "JoystickButton13";
                case "DpadLeft":
                    return "JoystickButton14";
                case "DpadRight":
                    return "JoystickButton15";
                default:
                    return keyCode;
            }
        }

        private static string FormatKeyboardKey(Key key)
        {
            return key == Key.Enter ? "Enter" : key.ToString();
        }
    }
}
