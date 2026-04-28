using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeGameMenu : MonoBehaviour
    {
        private enum MenuTab
        {
            Party,
            Inventory,
            Quests,
            Settings
        }

        private enum SettingsTab
        {
            General,
            Ui,
            Input,
            Debug
        }

        private const float MinUiScale = 0.5f;
        private const float MaxUiScale = 3f;
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private static bool isOpen;

        private DungeonEscapeGameState gameState;
        private DungeonEscapeUiSettings uiSettings;
        private TiledMapView mapView;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;
        private DungeonEscapeUiTheme uiTheme;
        private float lastPixelScale;
        private string lastThemeSignature;
        private MenuTab currentTab = MenuTab.Party;
        private SettingsTab currentSettingsTab = SettingsTab.General;
        private Vector2 scrollPosition;
        private int selectedHeroIndex;
        private int selectedRowIndex;
        private int selectedBindingSlotIndex;
        private int drawingRowIndex;
        private InputBinding rebindingInput;
        private string rebindingSlot;
        private int repeatingMenuMoveX;
        private float nextMenuMoveXTime;
        private int repeatingMenuMoveY;
        private float nextMenuMoveYTime;
        private int heldSettingsTabMoveX;

        public static bool IsOpen
        {
            get { return isOpen; }
        }

        private void Update()
        {
            if (rebindingInput != null)
            {
                string keyCode;
                if (DungeonEscapeInput.TryCaptureBinding(out keyCode))
                {
                    DungeonEscapeInput.SetBinding(rebindingInput, rebindingSlot, keyCode);
                    DungeonEscapeSettingsCache.Save();
                    rebindingInput = null;
                    rebindingSlot = null;
                }

                return;
            }

            if (isOpen && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuPreviousTab))
            {
                CycleTab(-1);
            }
            else if (isOpen && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuNextTab))
            {
                CycleTab(1);
            }
            else if (isOpen && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
            {
                isOpen = false;
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuParty))
            {
                Toggle(MenuTab.Party);
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuInventory))
            {
                Toggle(MenuTab.Inventory);
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuQuests))
            {
                Toggle(MenuTab.Quests);
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.MenuSettings))
            {
                Toggle(MenuTab.Settings);
            }
            else if (isOpen && DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact))
            {
                ActivateSelectedRow();
            }

            UpdateGamepadMenuNavigation();
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureReferences();
            EnsureStyles();

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 980f * scale);
            var height = Mathf.Min(Screen.height - 32f * scale, 680f * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

            GUI.Box(rect, GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(rect.x + 16f * scale, rect.y + 14f * scale, rect.width - 32f * scale, rect.height - 28f * scale));
            DrawHeader();
            DrawTabs();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            DrawCurrentTab();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Toggle(MenuTab tab)
        {
            if (isOpen && currentTab == tab)
            {
                isOpen = false;
                return;
            }

            currentTab = tab;
            scrollPosition = Vector2.zero;
            isOpen = true;
            selectedRowIndex = 0;
            ResetMenuNavigationRepeat();
        }

        private void CycleTab(int delta)
        {
            var tabCount = Enum.GetValues(typeof(MenuTab)).Length;
            var next = ((int)currentTab + delta + tabCount) % tabCount;
            currentTab = (MenuTab)next;
            scrollPosition = Vector2.zero;
            selectedRowIndex = 0;
            ResetMenuNavigationRepeat();
        }

        private void CycleSettingsTab(int delta)
        {
            var tabCount = Enum.GetValues(typeof(SettingsTab)).Length;
            var next = ((int)currentSettingsTab + delta + tabCount) % tabCount;
            currentSettingsTab = (SettingsTab)next;
            scrollPosition = Vector2.zero;
            selectedRowIndex = 0;
        }

        private void UpdateGamepadMenuNavigation()
        {
            if (!isOpen)
            {
                ResetMenuNavigationRepeat();
                return;
            }

            var moveY = GetMenuMoveY();
            if (moveY != 0)
            {
                MoveSelectedRow(moveY);
            }

            var moveX = currentTab == MenuTab.Settings && selectedRowIndex == 0
                ? GetSettingsTabMoveX()
                : GetMenuMoveX();
            if (moveX == 0)
            {
                return;
            }

            AdjustSelectedRow(moveX);
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Dungeon Escape", titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(96f * GetPixelScale())))
            {
                isOpen = false;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            DrawTab(MenuTab.Party, "Party");
            DrawTab(MenuTab.Inventory, "Inventory");
            DrawTab(MenuTab.Quests, "Quests");
            DrawTab(MenuTab.Settings, "Settings");
            GUILayout.EndHorizontal();
            GUILayout.Space(10f * GetPixelScale());
        }

        private void DrawTab(MenuTab tab, string label)
        {
            if (DungeonEscapeUiControls.TabButton(label, currentTab == tab, uiTheme, 34f * GetPixelScale()))
            {
                currentTab = tab;
                scrollPosition = Vector2.zero;
                selectedRowIndex = 0;
            }
        }

        private void DrawCurrentTab()
        {
            drawingRowIndex = 0;
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Mathf.Max(GetSelectableRowCount() - 1, 0));
            switch (currentTab)
            {
                case MenuTab.Party:
                    DrawParty();
                    break;
                case MenuTab.Inventory:
                    DrawInventory();
                    break;
                case MenuTab.Quests:
                    DrawQuests();
                    break;
                case MenuTab.Settings:
                    DrawSettings();
                    break;
            }
        }

        private void DrawParty()
        {
            var party = GetParty();
            if (party == null)
            {
                GUILayout.Label("No party loaded.", labelStyle);
                return;
            }

            GUILayout.Label("Gold: " + party.Gold + "    Map: " + party.CurrentMapId + "    Steps: " + party.StepCount, labelStyle);
            GUILayout.Space(8f * GetPixelScale());
            var activeMembers = party.ActiveMembers.ToList();
            GUILayout.Label("Active Party (" + activeMembers.Count + "/" + GetMaxPartyMembers() + ")", titleStyle);
            for (var i = 0; i < activeMembers.Count; i++)
            {
                BeginSelectableRow();
                DrawHeroStatus(activeMembers[i], true);
                DrawActivePartyControls(activeMembers[i], i, activeMembers.Count);
                EndSelectableRow();
            }

            var inactive = party.InactiveMembers.ToList();
            if (inactive.Count > 0)
            {
                GUILayout.Space(10f * GetPixelScale());
                GUILayout.Label("Reserve", titleStyle);
                foreach (var hero in inactive)
                {
                    BeginSelectableRow();
                    DrawHeroStatus(hero, false);
                    DrawReservePartyControls(hero, activeMembers.Count);
                    EndSelectableRow();
                }
            }
        }

        private void DrawActivePartyControls(Hero hero, int index, int activeCount)
        {
            GUILayout.BeginHorizontal();
            GUI.enabled = index > 0;
            if (GUILayout.Button("Move Up", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.MovePartyMemberUp(hero));
            }

            GUI.enabled = index < activeCount - 1;
            if (GUILayout.Button("Move Down", buttonStyle, GUILayout.Width(128f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.MovePartyMemberDown(hero));
            }

            GUI.enabled = activeCount > 1;
            if (GUILayout.Button("Reserve", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.DeactivatePartyMember(hero));
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(6f * GetPixelScale());
        }

        private void DrawReservePartyControls(Hero hero, int activeCount)
        {
            GUI.enabled = activeCount < GetMaxPartyMembers();
            if (GUILayout.Button("Add to Party", buttonStyle, GUILayout.Width(144f * GetPixelScale())))
            {
                ApplyPartyChange(() => gameState.ActivatePartyMember(hero));
            }

            GUI.enabled = true;
            GUILayout.Space(6f * GetPixelScale());
        }

        private void DrawHeroStatus(Hero hero, bool active)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(hero.Name + "  L" + hero.Level + " " + hero.Class + (active ? "" : "  Reserve"), labelStyle);
            GUILayout.Label(
                "HP " + hero.Health + "/" + hero.MaxHealth +
                "   MP " + hero.Magic + "/" + hero.MaxMagic +
                "   XP " + hero.Xp + "/" + hero.NextLevel,
                smallStyle);
            GUILayout.Label(
                "ATK " + hero.Attack +
                "   DEF " + hero.Defence +
                "   MDEF " + hero.MagicDefence +
                "   AGI " + hero.Agility,
                smallStyle);
            GUILayout.EndVertical();
        }

        private void ApplyPartyChange(Func<bool> action)
        {
            EnsureReferences();
            if (action == null || !action())
            {
                return;
            }

            var player = FindFirstObjectByType<PlayerGridController>();
            if (player != null)
            {
                player.RefreshPartyFollowers();
            }
        }

        private void DrawInventory()
        {
            var party = GetParty();
            if (party == null)
            {
                GUILayout.Label("No party loaded.", labelStyle);
                return;
            }

            var members = party.Members.OrderBy(member => member.IsActive ? 0 : 1).ThenBy(member => member.Order).ToList();
            if (members.Count == 0)
            {
                GUILayout.Label("No party members.", labelStyle);
                return;
            }

            selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1);
            GUILayout.BeginHorizontal();
            for (var i = 0; i < members.Count; i++)
            {
                if (DungeonEscapeUiControls.Button(members[i].Name, selectedHeroIndex == i, uiTheme))
                {
                    selectedHeroIndex = i;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f * GetPixelScale());

            var hero = members[selectedHeroIndex];
            GUILayout.Label(hero.Name + "'s Inventory (" + hero.Items.Count + "/" + Party.MaxItems + ")", titleStyle);
            if (hero.Items.Count == 0)
            {
                GUILayout.Label("No items.", labelStyle);
                return;
            }

            foreach (var item in hero.Items)
            {
                var equipped = item.IsEquipped ? " [E]" : "";
                BeginSelectableRow();
                GUILayout.BeginHorizontal();
                GUILayout.Label(item.NameWithStats + equipped + "    " + item.Type + "    " + item.Gold + "g", labelStyle);
                if (item.IsEquipped)
                {
                    if (GUILayout.Button("Unequip", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
                    {
                        ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
                    }
                }
                else
                {
                    GUI.enabled = hero.CanEquipItem(item);
                    if (GUILayout.Button("Equip", buttonStyle, GUILayout.Width(96f * GetPixelScale())))
                    {
                        ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
                    }

                    GUI.enabled = true;
                }

                GUILayout.EndHorizontal();
                EndSelectableRow();
            }
        }

        private void ApplyInventoryChange(Func<bool> action)
        {
            EnsureReferences();
            if (action != null)
            {
                action();
            }
        }

        private void DrawQuests()
        {
            var party = GetParty();
            if (party == null)
            {
                GUILayout.Label("No party loaded.", labelStyle);
                return;
            }

            if (party.ActiveQuests == null || party.ActiveQuests.Count == 0)
            {
                GUILayout.Label("No active quests.", labelStyle);
                return;
            }

            foreach (var activeQuest in party.ActiveQuests)
            {
                Quest quest;
                if (DungeonEscapeGameDataCache.Current == null ||
                    !DungeonEscapeGameDataCache.Current.TryGetQuest(activeQuest.Id, out quest))
                {
                    BeginSelectableRow();
                    GUILayout.Label(activeQuest.Id + (activeQuest.Completed ? " (Finished)" : ""), labelStyle);
                    EndSelectableRow();
                    continue;
                }

                BeginSelectableRow();
                GUILayout.BeginVertical();
                GUILayout.Label(quest.Name + (activeQuest.Completed ? " (Finished)" : ""), labelStyle);
                if (!string.IsNullOrEmpty(quest.Description))
                {
                    GUILayout.Label(quest.Description, smallStyle);
                }

                var currentStage = quest.Stages == null
                    ? null
                    : quest.Stages.FirstOrDefault(stage => stage.Number == activeQuest.CurrentStage);
                if (currentStage != null && !string.IsNullOrEmpty(currentStage.Description))
                {
                    GUILayout.Label(currentStage.Description, smallStyle);
                }

                GUILayout.EndVertical();
                EndSelectableRow();
            }
        }

        private void DrawSettings()
        {
            var settings = DungeonEscapeSettingsCache.Current;
            if (settings == null)
            {
                GUILayout.Label("Settings are not loaded.", labelStyle);
                return;
            }

            DrawSettingsTabs();

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
        }

        private void DrawSettingsTabs()
        {
            BeginSelectableRow();
            GUILayout.BeginHorizontal();
            DrawSettingsTab(SettingsTab.General, "General");
            DrawSettingsTab(SettingsTab.Ui, "UI");
            DrawSettingsTab(SettingsTab.Input, "Input");
            DrawSettingsTab(SettingsTab.Debug, "Debug");
            GUILayout.EndHorizontal();
            EndSelectableRow();
            GUILayout.Space(10f * GetPixelScale());
        }

        private void DrawSettingsTab(SettingsTab tab, string label)
        {
            if (DungeonEscapeUiControls.TabButton(label, currentSettingsTab == tab, uiTheme, 32f * GetPixelScale()))
            {
                currentSettingsTab = tab;
                scrollPosition = Vector2.zero;
                selectedRowIndex = 0;
            }
        }

        private void DrawGeneralSettings(Settings settings)
        {
            GUI.changed = false;
            BeginSelectableRow();
            settings.UiScale = DrawSliderRow("UI Scale: " + settings.UiScale.ToString("0.00"), settings.UiScale <= 0f ? 1f : settings.UiScale, MinUiScale, MaxUiScale);
            EndSelectableRow();
            GUILayout.Space(8f * GetPixelScale());
            BeginSelectableRow();
            settings.SprintBoost = DrawSliderRow("Sprint Boost: " + settings.SprintBoost.ToString("0.00"), settings.SprintBoost <= 0f ? 1.5f : settings.SprintBoost, 1f, 3f);
            EndSelectableRow();
            GUILayout.Space(8f * GetPixelScale());
            BeginSelectableRow();
            settings.AutoSaveEnabled = DrawCheckboxRow(settings.AutoSaveEnabled, "Autosave enabled");
            EndSelectableRow();
            BeginSelectableRow();
            GUI.enabled = settings.AutoSaveEnabled;
            settings.AutoSaveIntervalSeconds = DrawSliderRow("Autosave Period: " + GetAutoSaveInterval(settings).ToString("0") + " seconds", GetAutoSaveInterval(settings), 5f, 300f);
            GUI.enabled = true;
            EndSelectableRow();

            if (GUI.changed)
            {
                ApplySettings(settings);
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
            GUI.changed = false;
            BeginSelectableRow();
            settings.MapDebugInfo = DrawCheckboxRow(settings.MapDebugInfo, "Map debug info");
            EndSelectableRow();
            BeginSelectableRow();
            settings.ShowHiddenObjects = DrawCheckboxRow(settings.ShowHiddenObjects, "Show hidden map objects");
            EndSelectableRow();

            if (GUI.changed)
            {
                ApplySettings(settings);
            }
        }

        private static float GetAutoSaveInterval(Settings settings)
        {
            return settings.AutoSaveIntervalSeconds <= 0f ? 5f : settings.AutoSaveIntervalSeconds;
        }

        private void DrawInputBindings()
        {
            var bindings = DungeonEscapeInput.GetBindings();
            GUILayout.Label("Input Bindings", titleStyle);
            if (rebindingInput != null)
            {
                GUILayout.Label("Press a key or gamepad button for " + rebindingInput.Command + " " + rebindingSlot + ".", labelStyle);
                if (GUILayout.Button("Cancel Rebind", buttonStyle, GUILayout.Width(160f * GetPixelScale())))
                {
                    rebindingInput = null;
                    rebindingSlot = null;
                }
            }

            foreach (var binding in bindings.OrderBy(item => item.Command))
            {
                BeginSelectableRow();
                GUILayout.BeginVertical();
                GUILayout.Label(binding.Command + ": " + DungeonEscapeInput.GetBindingText(binding), labelStyle);
                GUILayout.BeginHorizontal();
                DrawBindingButton(binding, "Primary", binding.Primary, 0);
                DrawBindingButton(binding, "Secondary", binding.Secondary, 1);
                DrawBindingButton(binding, "Gamepad", binding.Gamepad, 2);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                EndSelectableRow();
            }

            BeginSelectableRow();
            if (GUILayout.Button("Reset Input Bindings", buttonStyle, GUILayout.Width(220f * GetPixelScale())))
            {
                DungeonEscapeInput.ResetBindings();
                rebindingInput = null;
                rebindingSlot = null;
            }
            EndSelectableRow();
        }

        private void DrawBindingButton(InputBinding binding, string slot, string currentValue, int slotIndex)
        {
            var label = slot + ": " + (string.IsNullOrEmpty(currentValue) || currentValue == "None" ? "-" : currentValue);
            var selected = currentSettingsTab == SettingsTab.Input &&
                           drawingRowIndex - 1 == selectedRowIndex &&
                           selectedRowIndex > 0 &&
                           selectedBindingSlotIndex == slotIndex;
            if (DungeonEscapeUiControls.Button(label, selected, uiTheme, GUILayout.Width(190f * GetPixelScale())))
            {
                rebindingInput = binding;
                rebindingSlot = slot;
                selectedBindingSlotIndex = slotIndex;
            }
        }

        private void BeginSelectableRow()
        {
            var rowIndex = drawingRowIndex++;
            DungeonEscapeUiControls.BeginSelectableRow(rowIndex, selectedRowIndex, uiTheme);
        }

        private static void EndSelectableRow()
        {
            DungeonEscapeUiControls.EndSelectableRow();
        }

        private bool DrawCheckboxRow(bool value, string label)
        {
            return DungeonEscapeUiControls.CheckboxRow(value, label, uiTheme, GetPixelScale());
        }

        private float DrawSliderRow(string label, float value, float leftValue, float rightValue)
        {
            return DungeonEscapeUiControls.SliderRow(label, value, leftValue, rightValue, uiTheme);
        }

        private string DrawTextFieldRow(string label, string value, string fallback)
        {
            return DungeonEscapeUiControls.TextFieldRow(label, value, fallback, uiTheme);
        }

        private int GetSelectableRowCount()
        {
            var party = GetParty();
            switch (currentTab)
            {
                case MenuTab.Party:
                    return party == null ? 0 : party.Members.Count;
                case MenuTab.Inventory:
                    return GetSelectedInventoryHero() == null ? 0 : GetSelectedInventoryHero().Items.Count;
                case MenuTab.Quests:
                    return party == null || party.ActiveQuests == null ? 0 : party.ActiveQuests.Count;
                case MenuTab.Settings:
                    return GetSettingsSelectableRowCount();
                default:
                    return 0;
            }
        }

        private int GetSettingsSelectableRowCount()
        {
            switch (currentSettingsTab)
            {
                case SettingsTab.General:
                    return 5;
                case SettingsTab.Ui:
                    return 9;
                case SettingsTab.Input:
                    return DungeonEscapeInput.GetBindings().Length + 2;
                case SettingsTab.Debug:
                    return 3;
                default:
                    return 0;
            }
        }

        private void MoveSelectedRow(int delta)
        {
            var count = GetSelectableRowCount();
            if (count <= 0)
            {
                selectedRowIndex = 0;
                return;
            }

            selectedRowIndex = Mathf.Clamp(selectedRowIndex + delta, 0, count - 1);
            scrollPosition.y = Mathf.Max(0f, selectedRowIndex * 54f * GetPixelScale());
        }

        private void AdjustSelectedRow(int delta)
        {
            if (currentTab == MenuTab.Party)
            {
                AdjustSelectedPartyMember(delta);
                return;
            }

            if (currentTab == MenuTab.Inventory)
            {
                AdjustSelectedInventoryHero(delta);
                return;
            }

            if (currentTab == MenuTab.Settings)
            {
                AdjustSelectedSetting(delta);
            }
        }

        private void ActivateSelectedRow()
        {
            if (currentTab == MenuTab.Party)
            {
                ActivateSelectedPartyMember();
                return;
            }

            if (currentTab == MenuTab.Inventory)
            {
                ActivateSelectedInventoryItem();
                return;
            }

            if (currentTab == MenuTab.Settings)
            {
                ActivateSelectedSetting();
            }
        }

        private void AdjustSelectedPartyMember(int delta)
        {
            var party = GetParty();
            if (party == null)
            {
                return;
            }

            var activeMembers = party.ActiveMembers.ToList();
            if (selectedRowIndex < 0 || selectedRowIndex >= activeMembers.Count)
            {
                return;
            }

            if (delta < 0)
            {
                ApplyPartyChange(() => gameState.MovePartyMemberUp(activeMembers[selectedRowIndex]));
                selectedRowIndex = Mathf.Max(0, selectedRowIndex - 1);
            }
            else if (delta > 0)
            {
                ApplyPartyChange(() => gameState.MovePartyMemberDown(activeMembers[selectedRowIndex]));
                selectedRowIndex = Mathf.Min(activeMembers.Count - 1, selectedRowIndex + 1);
            }
        }

        private void ActivateSelectedPartyMember()
        {
            var party = GetParty();
            if (party == null)
            {
                return;
            }

            var activeMembers = party.ActiveMembers.ToList();
            if (selectedRowIndex < activeMembers.Count)
            {
                ApplyPartyChange(() => gameState.DeactivatePartyMember(activeMembers[selectedRowIndex]));
                selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(GetSelectableRowCount() - 1, 0));
                return;
            }

            var inactiveMembers = party.InactiveMembers.ToList();
            var inactiveIndex = selectedRowIndex - activeMembers.Count;
            if (inactiveIndex >= 0 && inactiveIndex < inactiveMembers.Count)
            {
                ApplyPartyChange(() => gameState.ActivatePartyMember(inactiveMembers[inactiveIndex]));
            }
        }

        private Hero GetSelectedInventoryHero()
        {
            var party = GetParty();
            if (party == null)
            {
                return null;
            }

            var members = party.Members.OrderBy(member => member.IsActive ? 0 : 1).ThenBy(member => member.Order).ToList();
            if (members.Count == 0)
            {
                return null;
            }

            selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1);
            return members[selectedHeroIndex];
        }

        private void AdjustSelectedInventoryHero(int delta)
        {
            var party = GetParty();
            if (party == null || party.Members.Count <= 1)
            {
                return;
            }

            selectedHeroIndex = (selectedHeroIndex + delta + party.Members.Count) % party.Members.Count;
            selectedRowIndex = 0;
        }

        private void ActivateSelectedInventoryItem()
        {
            var hero = GetSelectedInventoryHero();
            if (hero == null || selectedRowIndex < 0 || selectedRowIndex >= hero.Items.Count)
            {
                return;
            }

            var item = hero.Items[selectedRowIndex];
            if (item.IsEquipped)
            {
                ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
            }
            else
            {
                ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
            }
        }

        private void AdjustSelectedSetting(int delta)
        {
            var settings = DungeonEscapeSettingsCache.Current;
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
                        settings.SprintBoost = Mathf.Clamp((settings.SprintBoost <= 0f ? 1.5f : settings.SprintBoost) + 0.05f * delta, 1f, 3f);
                        ApplySettings(settings);
                        break;
                    case 3:
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
                selectedBindingSlotIndex = (selectedBindingSlotIndex + delta + 3) % 3;
            }
        }

        private void ActivateSelectedSetting()
        {
            var settings = DungeonEscapeSettingsCache.Current;
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

                ApplySettings(settings);
            }
            else if (currentSettingsTab == SettingsTab.Input)
            {
                ActivateSelectedInputBinding();
            }
        }

        private void ActivateSelectedInputBinding()
        {
            var bindings = DungeonEscapeInput.GetBindings().OrderBy(item => item.Command).ToList();
            var bindingIndex = selectedRowIndex - 1;
            if (bindingIndex >= bindings.Count)
            {
                DungeonEscapeInput.ResetBindings();
                rebindingInput = null;
                rebindingSlot = null;
                return;
            }

            if (bindingIndex < 0)
            {
                return;
            }

            rebindingInput = bindings[bindingIndex];
            rebindingSlot = selectedBindingSlotIndex == 0
                ? "Primary"
                : selectedBindingSlotIndex == 1 ? "Secondary" : "Gamepad";
        }

        private int GetMenuMoveX()
        {
            var pressed = DungeonEscapeInput.GetMoveXDown();
            if (pressed != 0)
            {
                repeatingMenuMoveX = pressed;
                nextMenuMoveXTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = DungeonEscapeInput.GetMoveX();
            if (held == 0)
            {
                repeatingMenuMoveX = 0;
                nextMenuMoveXTime = 0f;
                return 0;
            }

            if (held != repeatingMenuMoveX)
            {
                repeatingMenuMoveX = held;
                nextMenuMoveXTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMenuMoveXTime)
            {
                return 0;
            }

            nextMenuMoveXTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        private int GetSettingsTabMoveX()
        {
            var moveX = DungeonEscapeInput.GetMoveX();
            if (moveX == 0)
            {
                heldSettingsTabMoveX = 0;
                return 0;
            }

            if (heldSettingsTabMoveX == moveX)
            {
                return 0;
            }

            heldSettingsTabMoveX = moveX;
            return moveX;
        }

        private int GetMenuMoveY()
        {
            var pressed = DungeonEscapeInput.GetMoveYDown();
            if (pressed != 0)
            {
                repeatingMenuMoveY = pressed;
                nextMenuMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = DungeonEscapeInput.GetMoveY();
            if (held == 0)
            {
                repeatingMenuMoveY = 0;
                nextMenuMoveYTime = 0f;
                return 0;
            }

            if (held != repeatingMenuMoveY)
            {
                repeatingMenuMoveY = held;
                nextMenuMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMenuMoveYTime)
            {
                return 0;
            }

            nextMenuMoveYTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        private void ResetMenuNavigationRepeat()
        {
            repeatingMenuMoveX = 0;
            nextMenuMoveXTime = 0f;
            repeatingMenuMoveY = 0;
            nextMenuMoveYTime = 0f;
            heldSettingsTabMoveX = 0;
        }

        private void ApplySettings(Settings settings)
        {
            DungeonEscapeSettingsCache.Set(settings);
            DungeonEscapeSettingsCache.Save();
            DungeonEscapeUiSettings.GetOrCreate().ApplySettings(settings);
            lastThemeSignature = null;
            if (mapView == null)
            {
                mapView = FindFirstObjectByType<TiledMapView>();
            }

            if (mapView != null)
            {
                mapView.RefreshRender();
            }
        }

        private static string GetThemeValue(string value, string fallback)
        {
            return DungeonEscapeUiTheme.GetThemeValue(value, fallback);
        }

        private static int GetBorderThickness(Settings settings)
        {
            return DungeonEscapeUiTheme.GetBorderThickness(settings);
        }

        private static int GetMaxPartyMembers()
        {
            return DungeonEscapeSettingsCache.Current == null || DungeonEscapeSettingsCache.Current.MaxPartyMembers <= 0
                ? 4
                : DungeonEscapeSettingsCache.Current.MaxPartyMembers;
        }

        private Party GetParty()
        {
            EnsureReferences();
            return gameState == null ? null : gameState.Party;
        }

        private void EnsureReferences()
        {
            if (gameState == null)
            {
                gameState = DungeonEscapeGameState.GetOrCreate();
            }

            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            if (mapView == null)
            {
                mapView = FindFirstObjectByType<TiledMapView>();
            }
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = DungeonEscapeSettingsCache.Current;
            var themeSignature = DungeonEscapeUiTheme.GetSignature(settings);
            if (titleStyle != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = DungeonEscapeUiTheme.Create(settings, scale);
            panelStyle = uiTheme.PanelStyle;
            titleStyle = uiTheme.TitleStyle;
            labelStyle = uiTheme.LabelStyle;
            smallStyle = uiTheme.SmallStyle;
            buttonStyle = uiTheme.ButtonStyle;
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }
    }
}
