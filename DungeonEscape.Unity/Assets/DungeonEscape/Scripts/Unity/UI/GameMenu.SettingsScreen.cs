using Redpoint.DungeonEscape.Data;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private sealed class SettingsMenuScreen : MenuScreenController
        {
            public SettingsMenuScreen(GameMenu menu)
                : base(menu)
            {
            }

            public override int GetSelectableRowCount()
            {
                return Menu.GetSettingsSelectableRowCount();
            }

            public override void Draw()
            {
                Menu.DrawSettingsContent();
            }

            public override void ActivateSelectedRow()
            {
                Menu.ActivateSelectedSetting();
            }

            public override void AdjustSelection(int delta)
            {
                Menu.AdjustSelectedSetting(delta);
            }
        }

        private void DrawSettingsContent()
        {
            var settings = SettingsCache.Current;
            if (settings == null)
            {
                GUILayout.Label("Settings are not loaded.", labelStyle);
                return;
            }

            EnsureVisibleSettingsTab(settings);
            DrawSettingsTabs();

            var scale = GetPixelScale();
            var settingsScrollHeight = Mathf.Max(120f * scale, GetMenuContentHeight() - 42f * scale);
            settingsScrollPosition = BeginThemedScroll(settingsScrollPosition, settingsScrollHeight);
            switch (currentSettingsTab)
            {
                case SettingsTab.General:
                    DrawGeneralSettings(settings);
                    break;
                case SettingsTab.Ui:
                    DrawUiSettings(settings);
                    break;
                case SettingsTab.Input:
                    DrawInputBindings();
                    break;
                case SettingsTab.Debug:
                    DrawDebugSettings(settings);
                    break;
            }
            EndThemedScroll();
        }

        private void DrawSettingsTabs()
        {
            BeginSelectableRow();
            GUILayout.BeginHorizontal();
            foreach (var tab in GetVisibleSettingsTabs(SettingsCache.Current))
            {
                DrawSettingsTab(tab, GetSettingsTabLabel(tab));
            }

            GUILayout.EndHorizontal();
            EndSelectableRow();
            GUILayout.Space(10f * GetPixelScale());
        }

        private void EnsureVisibleSettingsTab(Settings settings)
        {
            var tabs = GetVisibleSettingsTabs(settings);
            if (!tabs.Contains(currentSettingsTab))
            {
                currentSettingsTab = SettingsTab.General;
                selectedRowIndex = 0;
                settingsScrollPosition = Vector2.zero;
            }
        }

        private static List<SettingsTab> GetVisibleSettingsTabs(Settings settings)
        {
            var tabs = new List<SettingsTab> { SettingsTab.General };
            if (settings == null || settings.ShowUiSettingsTab)
            {
                tabs.Add(SettingsTab.Ui);
            }

            tabs.Add(SettingsTab.Input);
            if (settings == null || settings.ShowDebugSettingsTab)
            {
                tabs.Add(SettingsTab.Debug);
            }

            return tabs;
        }

        private static string GetSettingsTabLabel(SettingsTab tab)
        {
            return tab == SettingsTab.Ui ? "UI" : tab == SettingsTab.Input ? "Input Bindings" : tab.ToString();
        }

        private void DrawSettingsTab(SettingsTab tab, string label)
        {
            if (UiControls.TabButton(label, currentSettingsTab == tab, uiTheme, 32f * GetPixelScale()))
            {
                currentSettingsTab = tab;
                settingsScrollPosition = Vector2.zero;
                selectedRowIndex = 0;
            }
        }

        private void DrawGeneralSettings(Settings settings)
        {
            var oldUiScale = settings.UiScale;
            var oldFullScreen = settings.IsFullScreen;
            var oldMusicVolume = settings.MusicVolume;
            var oldSoundEffectsVolume = settings.SoundEffectsVolume;
            var oldAutoSaveEnabled = settings.AutoSaveEnabled;
            var oldAutoSaveInterval = settings.AutoSaveIntervalSeconds;
            var oldDialogTextSpeed = settings.DialogTextCharactersPerSecond;

            BeginSelectableRow();
            settings.UiScale = DrawSliderRow("UI Scale: " + settings.UiScale.ToString("0.00"), settings.UiScale <= 0f ? 1f : settings.UiScale, MinUiScale, MaxUiScale);
            EndSelectableRow();
            BeginSelectableRow();
            settings.DialogTextCharactersPerSecond = DrawSliderRow(
                "Dialog Text Speed: " + GetDialogTextSpeedLabel(settings.DialogTextCharactersPerSecond),
                Mathf.Clamp(settings.DialogTextCharactersPerSecond, 0f, 120f),
                0f,
                120f);
            EndSelectableRow();
            GUILayout.Space(8f * GetPixelScale());
            BeginSelectableRow();
            settings.IsFullScreen = DrawCheckboxRow(settings.IsFullScreen, "Fullscreen");
            EndSelectableRow();
            BeginSelectableRow();
            settings.MusicVolume = DrawSliderRow("Music Volume: " + Mathf.Clamp01(settings.MusicVolume).ToString("0.00"), Mathf.Clamp01(settings.MusicVolume), 0f, 1f);
            EndSelectableRow();
            BeginSelectableRow();
            settings.SoundEffectsVolume = DrawSliderRow("Sound Effects Volume: " + Mathf.Clamp01(settings.SoundEffectsVolume).ToString("0.00"), Mathf.Clamp01(settings.SoundEffectsVolume), 0f, 1f);
            EndSelectableRow();
            GUILayout.Space(8f * GetPixelScale());
            BeginSelectableRow();
            settings.AutoSaveEnabled = DrawCheckboxRow(settings.AutoSaveEnabled, "Autosave enabled");
            EndSelectableRow();
            BeginSelectableRow();
            SetMenuGuiEnabled(settings.AutoSaveEnabled);
            settings.AutoSaveIntervalSeconds = DrawSliderRow("Autosave Period: " + GetAutoSaveInterval(settings).ToString("0") + " seconds", GetAutoSaveInterval(settings), 5f, 300f);
            SetMenuGuiEnabled(true);
            EndSelectableRow();

            var volumeChanged =
                !Mathf.Approximately(oldMusicVolume, settings.MusicVolume) ||
                !Mathf.Approximately(oldSoundEffectsVolume, settings.SoundEffectsVolume);
            var otherChanged =
                !Mathf.Approximately(oldUiScale, settings.UiScale) ||
                oldFullScreen != settings.IsFullScreen ||
                oldAutoSaveEnabled != settings.AutoSaveEnabled ||
                !Mathf.Approximately(oldAutoSaveInterval, settings.AutoSaveIntervalSeconds) ||
                !Mathf.Approximately(oldDialogTextSpeed, settings.DialogTextCharactersPerSecond);

            if (otherChanged)
            {
                ApplySettings(settings);
            }
            else if (volumeChanged)
            {
                ApplyAudioSettings(settings);
            }
        }

        private void DrawUiSettings(Settings settings)
        {
            GUI.changed = false;
            BeginSelectableRow();
            settings.UiBackgroundColor = DrawTextFieldRow("Background Colour", settings.UiBackgroundColor, "#000000");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiBackgroundAlpha = DrawSliderRow("Background Transparency: " + settings.UiBackgroundAlpha.ToString("0.00"), Mathf.Clamp01(settings.UiBackgroundAlpha), 0f, 1f);
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiHoverColor = DrawTextFieldRow("Hover Colour", settings.UiHoverColor, "#808080");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiActiveColor = DrawTextFieldRow("Pressed Colour", settings.UiActiveColor, "#D3D3D3");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiBorderColor = DrawTextFieldRow("Border Colour", settings.UiBorderColor, "#FFFFFF");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiBorderThickness = Mathf.RoundToInt(DrawSliderRow("Border Thickness: " + GetBorderThickness(settings), GetBorderThickness(settings), 2f, 12f));
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiTextColor = DrawTextFieldRow("Text Colour", settings.UiTextColor, "#FFFFFF");
            EndSelectableRow();
            BeginSelectableRow();
            settings.UiHighlightColor = DrawTextFieldRow("Highlighted Text/Border Colour", settings.UiHighlightColor, "#FFFF00");
            EndSelectableRow();

            if (GUI.changed)
            {
                ApplySettings(settings);
            }
        }

        private void DrawDebugSettings(Settings settings)
        {
            var oldMapDebugInfo = settings.MapDebugInfo;
            var oldShowHiddenObjects = settings.ShowHiddenObjects;
            var oldNoMonsters = settings.NoMonsters;
            var oldSprintBoost = settings.SprintBoost;
            var oldTurnDelay = settings.TurnMoveDelaySeconds;
            BeginSelectableRow();
            settings.MapDebugInfo = DrawCheckboxRow(settings.MapDebugInfo, "Map debug info");
            EndSelectableRow();
            BeginSelectableRow();
            settings.ShowHiddenObjects = DrawCheckboxRow(settings.ShowHiddenObjects, "Show hidden map objects");
            EndSelectableRow();
            BeginSelectableRow();
            settings.NoMonsters = !DrawCheckboxRow(!settings.NoMonsters, "Monster encounters");
            EndSelectableRow();
            GUILayout.Space(8f * GetPixelScale());
            BeginSelectableRow();
            settings.SprintBoost = DrawSliderRow("Sprint Boost: " + settings.SprintBoost.ToString("0.00"), settings.SprintBoost <= 0f ? 1.5f : settings.SprintBoost, 1f, 3f);
            EndSelectableRow();
            BeginSelectableRow();
            settings.TurnMoveDelaySeconds = DrawSliderRow("Turn Delay: " + GetTurnMoveDelay(settings).ToString("0.00") + " seconds", GetTurnMoveDelay(settings), 0f, 0.3f);
            EndSelectableRow();

            if (oldMapDebugInfo != settings.MapDebugInfo ||
                oldShowHiddenObjects != settings.ShowHiddenObjects ||
                oldNoMonsters != settings.NoMonsters ||
                !Mathf.Approximately(oldSprintBoost, settings.SprintBoost) ||
                !Mathf.Approximately(oldTurnDelay, settings.TurnMoveDelaySeconds))
            {
                ApplySettings(settings, oldShowHiddenObjects != settings.ShowHiddenObjects);
            }
        }

        private static float GetAutoSaveInterval(Settings settings)
        {
            return settings.AutoSaveIntervalSeconds <= 0f ? 5f : settings.AutoSaveIntervalSeconds;
        }

        private static string GetDialogTextSpeedLabel(float speed)
        {
            return speed <= 0f ? "Instant" : Mathf.RoundToInt(speed) + " chars/sec";
        }

        private static float GetTurnMoveDelay(Settings settings)
        {
            return Mathf.Clamp(settings.TurnMoveDelaySeconds, 0f, 0.3f);
        }

        private void DrawInputBindings()
        {
            var bindings = InputManager.GetBindings();
            var headerStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUILayout.BeginHorizontal();
            GUILayout.Label("Action", headerStyle, GUILayout.Width(220f * GetPixelScale()));
            GUILayout.Label("Key", headerStyle, GUILayout.Width(190f * GetPixelScale()));
            GUILayout.Label("Gamepad", headerStyle, GUILayout.Width(190f * GetPixelScale()));
            GUILayout.EndHorizontal();

            foreach (var binding in bindings.OrderBy(item => item.Command))
            {
                BeginSelectableRow();
                GUILayout.BeginHorizontal();
                GUILayout.Label(binding.Command, labelStyle, GUILayout.Width(220f * GetPixelScale()));
                DrawBindingCell(binding, "Primary", binding.Primary, 0);
                DrawBindingCell(binding, "Gamepad", binding.Gamepad, 1);
                GUILayout.EndHorizontal();
                EndSelectableRow();
            }

            BeginSelectableRow();
            if (UiControls.Button("Reset", buttonStyle, GUILayout.Width(120f * GetPixelScale())))
            {
                InputManager.ResetBindings();
                rebindingInput = null;
                rebindingSlot = null;
            }
            EndSelectableRow();
        }

        private void DrawBindingCell(InputBinding binding, string slot, string currentValue, int slotIndex)
        {
            var label = string.IsNullOrEmpty(currentValue) || currentValue == "None" ? "-" : currentValue;
            var selected = currentSettingsTab == SettingsTab.Input &&
                           drawingRowIndex - 1 == selectedRowIndex &&
                           selectedRowIndex > 0 &&
                           selectedBindingSlotIndex == slotIndex;
            if (UiControls.Button(label, selected, uiTheme, GUILayout.Width(190f * GetPixelScale())))
            {
                StartRebinding(binding, slot);
                selectedBindingSlotIndex = slotIndex;
            }
        }

        private string GetRebindingPrompt()
        {
            if (rebindingInput == null)
            {
                return "";
            }

            return rebindingSlot == "Gamepad"
                ? "Press a gamepad button to change the binding for " + rebindingInput.Command + "."
                : "Press a key to change the binding for " + rebindingInput.Command + ".";
        }

        private void StartRebinding(InputBinding binding, string slot)
        {
            EnsureReferences();
            rebindingInput = binding;
            rebindingSlot = slot;
            rebindingStartFrame = Time.frameCount;
        }

        private void AdjustSelectedSetting(int delta)
        {
            var settings = SettingsCache.Current;
            if (settings == null)
            {
                return;
            }

            if (selectedRowIndex == 0)
            {
                CycleSettingsTab(delta);
                return;
            }

            if (currentSettingsTab == SettingsTab.General)
            {
                switch (selectedRowIndex - 1)
                {
                    case 0:
                        settings.UiScale = Mathf.Clamp((settings.UiScale <= 0f ? 1f : settings.UiScale) + 0.05f * delta, MinUiScale, MaxUiScale);
                        ApplySettings(settings);
                        break;
                    case 1:
                        settings.DialogTextCharactersPerSecond = Mathf.Clamp(settings.DialogTextCharactersPerSecond + 5f * delta, 0f, 120f);
                        ApplySettings(settings);
                        break;
                    case 3:
                        settings.MusicVolume = Mathf.Clamp01(settings.MusicVolume + 0.01f * delta);
                        ApplyAudioSettings(settings);
                        break;
                    case 4:
                        settings.SoundEffectsVolume = Mathf.Clamp01(settings.SoundEffectsVolume + 0.01f * delta);
                        ApplyAudioSettings(settings);
                        break;
                    case 6:
                        settings.AutoSaveIntervalSeconds = Mathf.Clamp(GetAutoSaveInterval(settings) + 5f * delta, 5f, 300f);
                        ApplySettings(settings);
                        break;
                }
            }
            else if (currentSettingsTab == SettingsTab.Ui)
            {
                switch (selectedRowIndex - 1)
                {
                    case 1:
                        settings.UiBackgroundAlpha = Mathf.Clamp01(settings.UiBackgroundAlpha + 0.05f * delta);
                        ApplySettings(settings);
                        break;
                    case 5:
                        settings.UiBorderThickness = Mathf.Clamp(settings.UiBorderThickness + delta, 2, 12);
                        ApplySettings(settings);
                        break;
                }
            }
            else if (currentSettingsTab == SettingsTab.Input)
            {
                selectedBindingSlotIndex = (selectedBindingSlotIndex + delta + 2) % 2;
            }
            else if (currentSettingsTab == SettingsTab.Debug)
            {
                switch (selectedRowIndex - 1)
                {
                    case 3:
                        settings.SprintBoost = Mathf.Clamp((settings.SprintBoost <= 0f ? 1.5f : settings.SprintBoost) + 0.05f * delta, 1f, 3f);
                        ApplySettings(settings);
                        break;
                    case 4:
                        settings.TurnMoveDelaySeconds = Mathf.Clamp(GetTurnMoveDelay(settings) + 0.01f * delta, 0f, 0.3f);
                        ApplySettings(settings);
                        break;
                }
            }
        }

        private void ActivateSelectedSetting()
        {
            var settings = SettingsCache.Current;
            if (settings == null)
            {
                return;
            }

            if (selectedRowIndex == 0)
            {
                return;
            }

            var settingsRowIndex = selectedRowIndex - 1;
            if (currentSettingsTab == SettingsTab.General && settingsRowIndex == 2)
            {
                settings.IsFullScreen = !settings.IsFullScreen;
                ApplySettings(settings);
            }
            else if (currentSettingsTab == SettingsTab.General && settingsRowIndex == 5)
            {
                settings.AutoSaveEnabled = !settings.AutoSaveEnabled;
                ApplySettings(settings);
            }
            else if (currentSettingsTab == SettingsTab.Debug)
            {
                if (settingsRowIndex == 0)
                {
                    settings.MapDebugInfo = !settings.MapDebugInfo;
                }
                else if (settingsRowIndex == 1)
                {
                    settings.ShowHiddenObjects = !settings.ShowHiddenObjects;
                }
                else if (settingsRowIndex == 2)
                {
                    settings.NoMonsters = !settings.NoMonsters;
                }

                ApplySettings(settings, settingsRowIndex == 1);
            }
            else if (currentSettingsTab == SettingsTab.Input)
            {
                ActivateSelectedInputBinding();
            }
        }

        private void ActivateSelectedInputBinding()
        {
            var bindings = InputManager.GetBindings().OrderBy(item => item.Command).ToList();
            var bindingIndex = selectedRowIndex - 1;
            if (bindingIndex >= bindings.Count)
            {
                InputManager.ResetBindings();
                rebindingInput = null;
                rebindingSlot = null;
                return;
            }

            if (bindingIndex < 0)
            {
                return;
            }

            StartRebinding(bindings[bindingIndex], selectedBindingSlotIndex == 0 ? "Primary" : "Gamepad");
        }
    }
}
