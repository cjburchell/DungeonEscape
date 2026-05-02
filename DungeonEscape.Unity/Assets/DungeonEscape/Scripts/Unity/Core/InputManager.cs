using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Core
{
    public enum InputCommand
    {
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Interact,
        Cancel,
        Sprint,
        Menu,
        MenuPreviousTab,
        MenuNextTab,
        QuickSave,
        QuickLoad
    }

    public static class InputManager
    {
        private const float AxisDeadZone = 0.35f;
        private const float DpadAxisDeadZone = 0.5f;
        private static readonly KeyCode[] LegacyGamepadButtons =
        {
            KeyCode.JoystickButton0,
            KeyCode.JoystickButton1,
            KeyCode.JoystickButton2,
            KeyCode.JoystickButton3,
            KeyCode.JoystickButton4,
            KeyCode.JoystickButton5,
            KeyCode.JoystickButton6,
            KeyCode.JoystickButton7,
            KeyCode.JoystickButton8,
            KeyCode.JoystickButton9,
            KeyCode.JoystickButton10,
            KeyCode.JoystickButton11,
            KeyCode.JoystickButton12,
            KeyCode.JoystickButton13,
            KeyCode.JoystickButton14,
            KeyCode.JoystickButton15,
            KeyCode.JoystickButton16,
            KeyCode.JoystickButton17,
            KeyCode.JoystickButton18,
            KeyCode.JoystickButton19
        };

        private static readonly Dictionary<InputCommand, InputBinding> DefaultBindings =
            new Dictionary<InputCommand, InputBinding>
            {
                { InputCommand.MoveLeft, Binding(InputCommand.MoveLeft, "A", "None", "DpadLeft") },
                { InputCommand.MoveRight, Binding(InputCommand.MoveRight, "D", "None", "DpadRight") },
                { InputCommand.MoveUp, Binding(InputCommand.MoveUp, "W", "None", "DpadUp") },
                { InputCommand.MoveDown, Binding(InputCommand.MoveDown, "S", "None", "DpadDown") },
                { InputCommand.Interact, Binding(InputCommand.Interact, "Space", "None", "South") },
                { InputCommand.Cancel, Binding(InputCommand.Cancel, "Escape", "None", "East") },
                { InputCommand.Sprint, Binding(InputCommand.Sprint, "LeftShift", "None", "North") },
                { InputCommand.Menu, Binding(InputCommand.Menu, "E", "None", "West") },
                { InputCommand.MenuPreviousTab, Binding(InputCommand.MenuPreviousTab, "LeftBracket", "None", "LeftShoulder") },
                { InputCommand.MenuNextTab, Binding(InputCommand.MenuNextTab, "RightBracket", "None", "RightShoulder") },
                { InputCommand.QuickSave, Binding(InputCommand.QuickSave, "F6", "None", "None") },
                { InputCommand.QuickLoad, Binding(InputCommand.QuickLoad, "F9", "None", "None") }
            };

        public static bool GetCommandDown(InputCommand command)
        {
            var binding = GetBinding(command);
            return GetKeyDown(binding.Primary) || GetKeyDown(binding.Secondary) || GetKeyDown(binding.Gamepad);
        }

        public static bool GetCommand(InputCommand command)
        {
            var binding = GetBinding(command);
            return GetKey(binding.Primary) || GetKey(binding.Secondary) || GetKey(binding.Gamepad);
        }

        public static int GetMoveX()
        {
            if (GetCommand(InputCommand.MoveLeft))
            {
                return -1;
            }

            if (GetCommand(InputCommand.MoveRight))
            {
                return 1;
            }

            var dpad = GetGamepadDpadX();
            if (dpad != 0)
            {
                return dpad;
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
            if (GetCommand(InputCommand.MoveUp))
            {
                return -1;
            }

            if (GetCommand(InputCommand.MoveDown))
            {
                return 1;
            }

            var dpad = GetGamepadDpadY();
            if (dpad != 0)
            {
                return dpad;
            }

            var axis = GetGamepadStickY();
            if (axis > AxisDeadZone)
            {
                return -1;
            }

            return axis < -AxisDeadZone ? 1 : 0;
        }

        public static int GetUiMoveYWithRightStick()
        {
            var moveY = GetMoveY();
            if (moveY != 0)
            {
                return moveY;
            }

            var axis = GetGamepadRightStickY();
            if (axis > AxisDeadZone)
            {
                return -1;
            }

            return axis < -AxisDeadZone ? 1 : 0;
        }

        public static int GetMoveXDown()
        {
            if (GetCommandDown(InputCommand.MoveLeft))
            {
                return -1;
            }

            if (GetCommandDown(InputCommand.MoveRight))
            {
                return 1;
            }

            var dpad = GetGamepadDpadXDown();
            if (dpad != 0)
            {
                return dpad;
            }

            return 0;
        }

        public static int GetMoveYDown()
        {
            if (GetCommandDown(InputCommand.MoveUp))
            {
                return -1;
            }

            if (GetCommandDown(InputCommand.MoveDown))
            {
                return 1;
            }

            var dpad = GetGamepadDpadYDown();
            if (dpad != 0)
            {
                return dpad;
            }

            return 0;
        }

        public static InputBinding[] GetBindings()
        {
            EnsureBindings(SettingsCache.Current);
            return SettingsCache.Current.InputBindings;
        }

        public static string GetBindingText(InputBinding binding)
        {
            if (binding == null)
            {
                return "";
            }

            return FormatCode(binding.Primary) + " / " + FormatCode(binding.Gamepad);
        }

        public static bool TryCaptureBinding(string slot, out string keyCode)
        {
            if (slot == "Gamepad")
            {
                keyCode = TryCaptureGamepadButton();
                return !string.IsNullOrEmpty(keyCode);
            }

            return TryCaptureKeyboardKey(out keyCode);
        }

        private static bool TryCaptureKeyboardKey(out string keyCode)
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

            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (!IsKeyboardKeyCode(code) || !UnityEngine.Input.GetKeyDown(code))
                {
                    continue;
                }

                keyCode = FormatLegacyKeyboardKey(code);
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
                binding.Primary = string.IsNullOrEmpty(keyCode) ? "None" : keyCode;
                binding.Secondary = "None";
            }
            else if (slot == "Gamepad")
            {
                binding.Gamepad = string.IsNullOrEmpty(keyCode) ? "None" : keyCode;
            }
        }

        public static void ResetBindings()
        {
            var settings = SettingsCache.Current;
            if (settings == null)
            {
                return;
            }

            settings.InputBindings = DefaultBindings.Values.Select(CloneBinding).ToArray();
            SettingsCache.Save();
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

            MigrateLegacyMenuBinding(existing);
            existing = existing
                .Where(binding => DefaultBindings.Values.Any(defaultBinding =>
                    string.Equals(binding.Command, defaultBinding.Command, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(binding => binding.Command, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            foreach (var defaultBinding in DefaultBindings.Values)
            {
                if (existing.Any(binding => string.Equals(binding.Command, defaultBinding.Command, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                existing.Add(CloneBinding(defaultBinding));
            }

            foreach (var binding in existing)
            {
                binding.Secondary = "None";
                var defaultBinding = DefaultBindings.Values.FirstOrDefault(item =>
                    string.Equals(item.Command, binding.Command, StringComparison.OrdinalIgnoreCase));
                if (defaultBinding != null &&
                    string.Equals(binding.Primary, "None", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(binding.Gamepad, "None", StringComparison.OrdinalIgnoreCase))
                {
                    binding.Primary = defaultBinding.Primary;
                    binding.Gamepad = defaultBinding.Gamepad;
                }
            }

            settings.InputBindings = existing.ToArray();
            UpdateLegacyGamepadBinding(settings, InputCommand.MoveLeft, "JoystickButton14", "DpadLeft");
            UpdateLegacyGamepadBinding(settings, InputCommand.MoveRight, "JoystickButton15", "DpadRight");
            UpdateLegacyGamepadBinding(settings, InputCommand.MoveUp, "JoystickButton13", "DpadUp");
            UpdateLegacyGamepadBinding(settings, InputCommand.MoveDown, "JoystickButton12", "DpadDown");
            UpdateLegacyGamepadBinding(settings, InputCommand.Interact, "JoystickButton0", "South");
            UpdateLegacyGamepadBinding(settings, InputCommand.Cancel, "JoystickButton1", "East");
            UpdateLegacyGamepadBinding(settings, InputCommand.Sprint, "JoystickButton5", "North");
            UpdateLegacyGamepadBinding(settings, InputCommand.Sprint, "JoystickButton3", "North");
            UpdateLegacyGamepadBinding(settings, InputCommand.Menu, "JoystickButton3", "West");
            UpdateLegacyGamepadBinding(settings, InputCommand.Menu, "JoystickButton2", "West");
            UpdateLegacyGamepadBinding(settings, InputCommand.MenuPreviousTab, "JoystickButton4", "LeftShoulder");
            UpdateLegacyGamepadBinding(settings, InputCommand.MenuNextTab, "JoystickButton5", "RightShoulder");
        }

        private static void MigrateLegacyMenuBinding(List<InputBinding> bindings)
        {
            if (bindings == null)
            {
                return;
            }

            var current = bindings.FirstOrDefault(binding =>
                string.Equals(binding.Command, InputCommand.Menu.ToString(), StringComparison.OrdinalIgnoreCase));
            if (current != null)
            {
                return;
            }

            var legacy = bindings.FirstOrDefault(binding =>
                string.Equals(binding.Command, "MenuParty", StringComparison.OrdinalIgnoreCase));
            if (legacy == null)
            {
                return;
            }

            legacy.Command = InputCommand.Menu.ToString();
            if (string.Equals(legacy.Primary, "C", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(legacy.Primary))
            {
                legacy.Primary = DefaultBindings[InputCommand.Menu].Primary;
            }
        }

        private static InputBinding GetBinding(InputCommand command)
        {
            EnsureBindings(SettingsCache.Current);
            var commandName = command.ToString();
            var binding = SettingsCache.Current.InputBindings.FirstOrDefault(
                item => string.Equals(item.Command, commandName, StringComparison.OrdinalIgnoreCase));
            return binding ?? DefaultBindings[command];
        }

        private static void UpdateLegacyGamepadBinding(
            Settings settings,
            InputCommand command,
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

        private static int GetGamepadDpadX()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.left.isPressed)
                {
                    return -1;
                }

                if (gamepad.dpad.right.isPressed)
                {
                    return 1;
                }
            }

            if (UnityEngine.Input.GetKey(KeyCode.JoystickButton14))
            {
                return -1;
            }

            if (UnityEngine.Input.GetKey(KeyCode.JoystickButton15))
            {
                return 1;
            }

            return GetLegacyDpadAxisX();
        }

        private static int GetGamepadDpadY()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.up.isPressed)
                {
                    return -1;
                }

                if (gamepad.dpad.down.isPressed)
                {
                    return 1;
                }
            }

            if (UnityEngine.Input.GetKey(KeyCode.JoystickButton13))
            {
                return -1;
            }

            if (UnityEngine.Input.GetKey(KeyCode.JoystickButton12))
            {
                return 1;
            }

            return GetLegacyDpadAxisY();
        }

        private static int GetGamepadDpadXDown()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.left.wasPressedThisFrame)
                {
                    return -1;
                }

                if (gamepad.dpad.right.wasPressedThisFrame)
                {
                    return 1;
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton14))
            {
                return -1;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton15))
            {
                return 1;
            }

            return GetLegacyDpadAxisXDown();
        }

        private static int GetGamepadDpadYDown()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.up.wasPressedThisFrame)
                {
                    return -1;
                }

                if (gamepad.dpad.down.wasPressedThisFrame)
                {
                    return 1;
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton13))
            {
                return -1;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton12))
            {
                return 1;
            }

            return GetLegacyDpadAxisYDown();
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

        private static float GetGamepadRightStickY()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                return gamepad.rightStick.ReadValue().y;
            }

            return GetFirstActiveLegacyAxis(
                "Right Stick Y",
                "Right Y",
                "RightVertical",
                "Right Vertical",
                "4th Axis",
                "5th Axis",
                "RZ");
        }

        private static float GetGamepadRightStickX()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                return gamepad.rightStick.ReadValue().x;
            }

            return GetFirstActiveLegacyAxis(
                "Right Stick X",
                "Right X",
                "RightHorizontal",
                "Right Horizontal",
                "3rd Axis",
                "4th Axis");
        }

        private static string TryCaptureGamepadButton()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
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
                if (gamepad.dpad.down.wasPressedThisFrame) return "DpadDown";
            }

            foreach (var code in LegacyGamepadButtons)
            {
                if (UnityEngine.Input.GetKeyDown(code))
                {
                    return NormalizeGamepadButton(code.ToString());
                }
            }

            return null;
        }

        private static bool GetLegacyKey(string keyCode)
        {
            KeyCode code;
            return TryParseLegacyKeyCode(keyCode, out code) && code != KeyCode.None && UnityEngine.Input.GetKey(code);
        }

        private static bool GetLegacyKeyDown(string keyCode)
        {
            KeyCode code;
            return TryParseLegacyKeyCode(keyCode, out code) && code != KeyCode.None && UnityEngine.Input.GetKeyDown(code);
        }

        private static float GetLegacyAxis(string axisName)
        {
            try
            {
                return UnityEngine.Input.GetAxisRaw(axisName);
            }
            catch (ArgumentException)
            {
                return 0f;
            }
        }

        private static int GetLegacyDpadAxisX()
        {
            var axis = GetFirstActiveLegacyAxis(
                "DPad X",
                "Dpad X",
                "D-Pad X",
                "POV X",
                "Pov X",
                "Hat X",
                "6th Axis");
            if (axis < -DpadAxisDeadZone)
            {
                return -1;
            }

            return axis > DpadAxisDeadZone ? 1 : 0;
        }

        private static int GetLegacyDpadAxisY()
        {
            var axis = GetFirstActiveLegacyAxis(
                "DPad Y",
                "Dpad Y",
                "D-Pad Y",
                "POV Y",
                "Pov Y",
                "Hat Y",
                "7th Axis");
            if (axis > DpadAxisDeadZone)
            {
                return -1;
            }

            return axis < -DpadAxisDeadZone ? 1 : 0;
        }

        private static int GetLegacyDpadAxisXDown()
        {
            UpdateLegacyDpadAxisState();
            var current = currentLegacyDpadAxisX;
            if (current == 0)
            {
                return 0;
            }

            return previousLegacyDpadAxisX == current ? 0 : current;
        }

        private static int GetLegacyDpadAxisYDown()
        {
            UpdateLegacyDpadAxisState();
            var current = currentLegacyDpadAxisY;
            if (current == 0)
            {
                return 0;
            }

            return previousLegacyDpadAxisY == current ? 0 : current;
        }

        private static int previousLegacyDpadAxisX;
        private static int previousLegacyDpadAxisY;
        private static int currentLegacyDpadAxisX;
        private static int currentLegacyDpadAxisY;
        private static int legacyDpadAxisFrame = -1;

        private static void UpdateLegacyDpadAxisState()
        {
            if (legacyDpadAxisFrame == Time.frameCount)
            {
                return;
            }

            legacyDpadAxisFrame = Time.frameCount;
            previousLegacyDpadAxisX = currentLegacyDpadAxisX;
            previousLegacyDpadAxisY = currentLegacyDpadAxisY;
            currentLegacyDpadAxisX = GetLegacyDpadAxisX();
            currentLegacyDpadAxisY = GetLegacyDpadAxisY();
        }

        private static float GetFirstActiveLegacyAxis(params string[] axisNames)
        {
            foreach (var axisName in axisNames)
            {
                var value = GetLegacyAxis(axisName);
                if (Mathf.Abs(value) > DpadAxisDeadZone)
                {
                    return value;
                }
            }

            return 0f;
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

        private static InputBinding Binding(InputCommand command, string primary, string secondary, string gamepad)
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

        private static string FormatLegacyKeyboardKey(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.Return:
                    return "Enter";
                case KeyCode.Alpha0:
                    return "Digit0";
                case KeyCode.Alpha1:
                    return "Digit1";
                case KeyCode.Alpha2:
                    return "Digit2";
                case KeyCode.Alpha3:
                    return "Digit3";
                case KeyCode.Alpha4:
                    return "Digit4";
                case KeyCode.Alpha5:
                    return "Digit5";
                case KeyCode.Alpha6:
                    return "Digit6";
                case KeyCode.Alpha7:
                    return "Digit7";
                case KeyCode.Alpha8:
                    return "Digit8";
                case KeyCode.Alpha9:
                    return "Digit9";
                default:
                    return code.ToString();
            }
        }

        private static bool IsKeyboardKeyCode(KeyCode code)
        {
            if (code == KeyCode.None)
            {
                return false;
            }

            var name = code.ToString();
            return !name.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase) &&
                   !name.StartsWith("Joystick", StringComparison.OrdinalIgnoreCase);
        }
    }
}
