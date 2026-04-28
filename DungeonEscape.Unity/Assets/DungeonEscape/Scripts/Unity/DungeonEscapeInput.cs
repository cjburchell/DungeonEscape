using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape;
using UnityEngine;

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

        private static readonly KeyCode[] RebindableCodes = BuildRebindableCodes();
        private static readonly Dictionary<DungeonEscapeInputCommand, InputBinding> DefaultBindings =
            new Dictionary<DungeonEscapeInputCommand, InputBinding>
            {
                { DungeonEscapeInputCommand.MoveLeft, Binding(DungeonEscapeInputCommand.MoveLeft, KeyCode.LeftArrow, KeyCode.A, KeyCode.JoystickButton14) },
                { DungeonEscapeInputCommand.MoveRight, Binding(DungeonEscapeInputCommand.MoveRight, KeyCode.RightArrow, KeyCode.D, KeyCode.JoystickButton15) },
                { DungeonEscapeInputCommand.MoveUp, Binding(DungeonEscapeInputCommand.MoveUp, KeyCode.UpArrow, KeyCode.W, KeyCode.JoystickButton13) },
                { DungeonEscapeInputCommand.MoveDown, Binding(DungeonEscapeInputCommand.MoveDown, KeyCode.DownArrow, KeyCode.S, KeyCode.JoystickButton12) },
                { DungeonEscapeInputCommand.Interact, Binding(DungeonEscapeInputCommand.Interact, KeyCode.Space, KeyCode.Return, KeyCode.JoystickButton0) },
                { DungeonEscapeInputCommand.Cancel, Binding(DungeonEscapeInputCommand.Cancel, KeyCode.Escape, KeyCode.None, KeyCode.JoystickButton1) },
                { DungeonEscapeInputCommand.Sprint, Binding(DungeonEscapeInputCommand.Sprint, KeyCode.LeftShift, KeyCode.RightShift, KeyCode.JoystickButton3) },
                { DungeonEscapeInputCommand.MenuParty, Binding(DungeonEscapeInputCommand.MenuParty, KeyCode.C, KeyCode.None, KeyCode.JoystickButton2) },
                { DungeonEscapeInputCommand.MenuInventory, Binding(DungeonEscapeInputCommand.MenuInventory, KeyCode.I, KeyCode.None, KeyCode.None) },
                { DungeonEscapeInputCommand.MenuQuests, Binding(DungeonEscapeInputCommand.MenuQuests, KeyCode.J, KeyCode.None, KeyCode.None) },
                { DungeonEscapeInputCommand.MenuSettings, Binding(DungeonEscapeInputCommand.MenuSettings, KeyCode.O, KeyCode.None, KeyCode.None) },
                { DungeonEscapeInputCommand.MenuPreviousTab, Binding(DungeonEscapeInputCommand.MenuPreviousTab, KeyCode.LeftBracket, KeyCode.None, KeyCode.JoystickButton4) },
                { DungeonEscapeInputCommand.MenuNextTab, Binding(DungeonEscapeInputCommand.MenuNextTab, KeyCode.RightBracket, KeyCode.None, KeyCode.JoystickButton5) },
                { DungeonEscapeInputCommand.QuickSave, Binding(DungeonEscapeInputCommand.QuickSave, KeyCode.F6, KeyCode.None, KeyCode.None) },
                { DungeonEscapeInputCommand.QuickLoad, Binding(DungeonEscapeInputCommand.QuickLoad, KeyCode.F9, KeyCode.None, KeyCode.None) },
                { DungeonEscapeInputCommand.Restart, Binding(DungeonEscapeInputCommand.Restart, KeyCode.F10, KeyCode.None, KeyCode.None) },
                { DungeonEscapeInputCommand.ReloadMap, Binding(DungeonEscapeInputCommand.ReloadMap, KeyCode.F5, KeyCode.None, KeyCode.JoystickButton8) }
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

            var axis = GetAxisRaw("Horizontal");
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

            var axis = GetAxisRaw("Vertical");
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
            foreach (var code in RebindableCodes)
            {
                if (Input.GetKeyDown(code))
                {
                    keyCode = code.ToString();
                    return true;
                }
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
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.Sprint, KeyCode.JoystickButton5, KeyCode.JoystickButton3);
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuParty, KeyCode.JoystickButton3, KeyCode.JoystickButton2);
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuInventory, KeyCode.JoystickButton2, KeyCode.None);
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuQuests, KeyCode.JoystickButton6, KeyCode.None);
            UpdateLegacyGamepadBinding(settings, DungeonEscapeInputCommand.MenuSettings, KeyCode.JoystickButton7, KeyCode.None);
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
            KeyCode oldValue,
            KeyCode newValue)
        {
            var commandName = command.ToString();
            var binding = settings.InputBindings.FirstOrDefault(
                item => string.Equals(item.Command, commandName, StringComparison.OrdinalIgnoreCase));
            if (binding == null || binding.Gamepad != oldValue.ToString())
            {
                return;
            }

            binding.Gamepad = newValue.ToString();
        }

        private static bool GetKey(string keyCode)
        {
            KeyCode code;
            return TryParseKeyCode(keyCode, out code) && code != KeyCode.None && Input.GetKey(code);
        }

        private static bool GetKeyDown(string keyCode)
        {
            KeyCode code;
            return TryParseKeyCode(keyCode, out code) && code != KeyCode.None && Input.GetKeyDown(code);
        }

        private static bool TryParseKeyCode(string keyCode, out KeyCode code)
        {
            if (string.IsNullOrEmpty(keyCode))
            {
                code = KeyCode.None;
                return false;
            }

            return Enum.TryParse(keyCode, true, out code);
        }

        private static float GetAxisRaw(string axisName)
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

        private static InputBinding Binding(DungeonEscapeInputCommand command, KeyCode primary, KeyCode secondary, KeyCode gamepad)
        {
            return new InputBinding
            {
                Command = command.ToString(),
                Primary = primary.ToString(),
                Secondary = secondary.ToString(),
                Gamepad = gamepad.ToString()
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
            return string.IsNullOrEmpty(code) || code == KeyCode.None.ToString() ? "-" : code;
        }

        private static KeyCode[] BuildRebindableCodes()
        {
            return Enum.GetValues(typeof(KeyCode))
                .Cast<KeyCode>()
                .Where(code => code != KeyCode.None &&
                               code != KeyCode.Mouse0 &&
                               code != KeyCode.Mouse1 &&
                               code != KeyCode.Mouse2 &&
                               code != KeyCode.Mouse3 &&
                               code != KeyCode.Mouse4 &&
                               code != KeyCode.Mouse5 &&
                               code != KeyCode.Mouse6)
                .ToArray();
        }
    }
}
