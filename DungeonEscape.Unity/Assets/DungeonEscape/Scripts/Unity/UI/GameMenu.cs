using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape;
using Redpoint.DungeonEscape.State;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class GameMenu : MonoBehaviour
    {
        private enum MenuTab
        {
            Party,
            Inventory,
            Quests,
            Save,
            Settings
        }

        private enum SettingsTab
        {
            General,
            Ui,
            Input,
            Debug
        }

        private enum PartyDetailTab
        {
            Status,
            Equipment,
            Items,
            Skills,
            Spells
        }

        private enum MenuScreen
        {
            Main,
            Items,
            Spells,
            Equipment,
            Abilities,
            Status,
            Party,
            Misc,
            Save,
            Load,
            Settings
        }

        private enum MenuFocus
        {
            Primary,
            Detail,
            SubDetail
        }

        private const float MinUiScale = 0.5f;
        private const float MaxUiScale = 3f;
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private static bool isOpen;

        private GameState gameState;
        private MessageBox messageBox;
        private UiSettings uiSettings;
        private View mapView;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;
        private UiTheme uiTheme;
        private float lastPixelScale;
        private string lastThemeSignature;
        private MenuScreen currentScreen = MenuScreen.Main;
        private MenuScreen previousScreen = MenuScreen.Main;
        private MenuFocus currentFocus = MenuFocus.Primary;
        private MenuTab currentTab = MenuTab.Party;
        private SettingsTab currentSettingsTab = SettingsTab.General;
        private PartyDetailTab currentPartyDetailTab = PartyDetailTab.Status;
        private Vector2 partyScrollPosition;
        private Vector2 inventoryScrollPosition;
        private Vector2 partyDetailScrollPosition;
        private Vector2 questScrollPosition;
        private Vector2 saveScrollPosition;
        private Vector2 settingsScrollPosition;
        private float menuBodyHeight;
        private int selectedHeroIndex;
        private int selectedPartyItemIndex;
        private int selectedDetailIndex;
        private int selectedEquipmentItemIndex;
        private int selectedMainActionIndex;
        private int selectedPreviousScreenRowIndex;
        private int detailPageIndex;
        private int selectedRowIndex;
        private int selectedBindingSlotIndex;
        private int drawingRowIndex;
        private InputBinding rebindingInput;
        private string rebindingSlot;
        private int rebindingStartFrame;
        private string menuModalTitle;
        private string menuModalMessage;
        private List<string> menuModalChoices;
        private List<Hero> menuModalChoiceHeroes;
        private Action<int> menuModalSelected;
        private int menuModalSelectedIndex;
        private bool menuModalWaitingForConfirmRelease;
        private int repeatingMenuMoveX;
        private float nextMenuMoveXTime;
        private int repeatingMenuMoveY;
        private float nextMenuMoveYTime;
        private int heldSettingsTabMoveX;
        private bool waitForMenuInteractRelease;
        private int acceptMenuInteractAfterFrame;
        private bool menuControlsBlocked;
        private bool uiAssetsPrewarmed;
        private bool pendingSettingsSave;
        private float pendingSettingsSaveTime;

        public static bool IsOpen
        {
            get { return isOpen; }
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            PrewarmUiAssets();
        }

        private void Update()
        {
            UpdateDeferredSettingsSave();

            if (CombatWindow.IsOpen)
            {
                if (isOpen)
                {
                    isOpen = false;
                }

                return;
            }

            if (StoreWindow.IsOpen || HealerWindow.IsOpen)
            {
                return;
            }

            if (TitleMenu.IsOpen)
            {
                return;
            }

            if (rebindingInput != null)
            {
                if (Time.frameCount <= rebindingStartFrame)
                {
                    return;
                }

                string keyCode;
                if (InputManager.TryCaptureBinding(rebindingSlot, out keyCode))
                {
                    InputManager.SetBinding(rebindingInput, rebindingSlot, keyCode);
                    SettingsCache.Save();
                    rebindingInput = null;
                    rebindingSlot = null;
                }

                return;
            }

            if (IsMenuModalVisible())
            {
                UpdateMenuModal();
                return;
            }

            UpdateMenuInteractRelease();

            if (isOpen && InputManager.GetCommandDown(InputCommand.MenuPreviousTab))
            {
                UiControls.PlaySelectSound();
                HandleMenuPageCommand(-1);
            }
            else if (isOpen && InputManager.GetCommandDown(InputCommand.MenuNextTab))
            {
                UiControls.PlaySelectSound();
                HandleMenuPageCommand(1);
            }
            else if (isOpen && InputManager.GetCommandDown(InputCommand.Cancel))
            {
                UiControls.PlayConfirmSound();
                CancelCurrentScreen();
            }
            else if (!isOpen && InputManager.GetCommandDown(InputCommand.Menu))
            {
                UiControls.PlayConfirmSound();
                Toggle(MenuTab.Party);
            }
            else if (isOpen && InputManager.GetCommandDown(InputCommand.Interact))
            {
                if (CanAcceptMenuInteract())
                {
                    UiControls.PlayConfirmSound();
                    ActivateSelectedRow();
                }
            }

            UpdateGamepadMenuNavigation();
        }

        private void OnGUI()
        {
            if (!isOpen || CombatWindow.IsOpen)
            {
                return;
            }

            EnsureReferences();
            EnsureStyles();

            if (MessageBox.IsAnyVisible)
            {
                return;
            }

            var previousDepth = GUI.depth;
            GUI.depth = 1000;
            var scale = GetPixelScale();
            if (currentScreen == MenuScreen.Main || currentScreen == MenuScreen.Misc)
            {
                DrawLeftActionMenuOverlay(scale);
                DrawMenuModalOverlay();
                DrawRebindingOverlay();
                GUI.depth = previousDepth;
                return;
            }

            var width = Mathf.Min(Screen.width - 32f * scale, 980f * scale);
            var menuTop = GetMenuTop(scale);
            var maxMenuHeight = GetMenuBottomLimit(scale) - menuTop;
            var height = Mathf.Max(180f * scale, Mathf.Min(maxMenuHeight, 720f * scale));
            var rect = new Rect(10f * scale, menuTop, width, height);

            GUI.Box(rect, GUIContent.none, panelStyle);
            var areaHeight = rect.height - 28f * scale;
            menuBodyHeight = Mathf.Max(160f * scale, areaHeight - 170f * scale);
            GUILayout.BeginArea(new Rect(rect.x + 16f * scale, rect.y + 14f * scale, rect.width - 32f * scale, areaHeight));
            var previousEnabled = GUI.enabled;
            var previousMenuControlsBlocked = menuControlsBlocked;
            menuControlsBlocked = rebindingInput != null || IsMenuModalVisible();
            SetMenuGuiEnabled(previousEnabled);
            var previousVerticalThumb = GUI.skin.verticalScrollbarThumb;
            if (uiTheme != null)
            {
                GUI.skin.verticalScrollbarThumb = uiTheme.VerticalScrollbarThumbStyle;
            }

            DrawCurrentTab();
            GUI.skin.verticalScrollbarThumb = previousVerticalThumb;
            GUI.enabled = previousEnabled;
            menuControlsBlocked = previousMenuControlsBlocked;
            GUILayout.EndArea();
            DrawMenuModalOverlay();
            DrawRebindingOverlay();
            GUI.depth = previousDepth;
        }

        private void DrawLeftActionMenuOverlay(float scale)
        {
            menuBodyHeight = Screen.height;
            var previousEnabled = GUI.enabled;
            var previousMenuControlsBlocked = menuControlsBlocked;
            menuControlsBlocked = rebindingInput != null || IsMenuModalVisible();
            SetMenuGuiEnabled(previousEnabled);
            var actions = currentScreen == MenuScreen.Misc ? GetMiscMenuActions() : GetMainMenuActions();
            if (currentScreen == MenuScreen.Main)
            {
                selectedMainActionIndex = Mathf.Clamp(selectedMainActionIndex, 0, Math.Max(actions.Count - 1, 0));
            }

            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(actions.Count - 1, 0));
            var rowHeight = GetLeftActionRowHeight(scale);
            var height = actions.Count * rowHeight + 8f * scale;
            var padding = 12f * scale;
            var rect = new Rect(10f * scale, GetMenuTop(scale), 200f * scale, height + padding * 2f + 14f * scale);
            GUI.Box(rect, GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2f, rect.height - padding * 2f));
            DrawActionList(actions, selectedRowIndex, false);
            GUILayout.EndArea();
            GUI.enabled = previousEnabled;
            menuControlsBlocked = previousMenuControlsBlocked;
        }

        private void SetMenuGuiEnabled(bool enabled)
        {
            GUI.enabled = enabled && !menuControlsBlocked;
        }

        private static float GetMenuTop(float scale)
        {
            return 96f * scale;
        }

        private static float GetMenuBottomLimit(float scale)
        {
            return Screen.height - 64f * scale;
        }

        private static float GetLeftActionRowHeight(float scale)
        {
            return 40f * scale;
        }

        private void Toggle(MenuTab tab)
        {
            PrewarmUiAssets();
            if (isOpen)
            {
                isOpen = false;
                return;
            }

            currentTab = tab;
            currentScreen = MenuScreen.Main;
            previousScreen = MenuScreen.Main;
            currentFocus = MenuFocus.Primary;
            isOpen = true;
            selectedRowIndex = 0;
            selectedMainActionIndex = 0;
            selectedPreviousScreenRowIndex = 0;
            selectedDetailIndex = 0;
            selectedEquipmentItemIndex = 0;
            detailPageIndex = 0;
            ResetMenuNavigationRepeat();
        }

        private void CycleTab(int delta)
        {
            var tabs = GetVisibleTabs();
            var currentIndex = tabs.IndexOf(currentTab);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            currentTab = tabs[(currentIndex + delta + tabs.Count) % tabs.Count];
            selectedRowIndex = 0;
            ResetMenuNavigationRepeat();
        }

        private static List<MenuTab> GetVisibleTabs()
        {
            return new List<MenuTab>
            {
                MenuTab.Party,
                MenuTab.Quests,
                MenuTab.Save,
                MenuTab.Settings
            };
        }

        private void CycleSettingsTab(int delta)
        {
            var tabs = GetVisibleSettingsTabs(SettingsCache.Current);
            var currentIndex = tabs.IndexOf(currentSettingsTab);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            currentSettingsTab = tabs[(currentIndex + delta + tabs.Count) % tabs.Count];
            settingsScrollPosition = Vector2.zero;
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
                MoveMenuSelection(moveY);
            }

            var moveX = currentScreen == MenuScreen.Settings && selectedRowIndex == 0
                ? GetSettingsTabMoveX()
                : GetMenuMoveX();
            if (moveX == 0)
            {
                return;
            }

            AdjustMenuSelection(moveX);
        }

        private void HandleMenuPageCommand(int delta)
        {
            if (currentScreen == MenuScreen.Settings)
            {
                CycleSettingsTab(delta);
                return;
            }

            if (IsPagedDetailScreen(currentScreen))
            {
                ChangeDetailPage(delta);
            }
        }

        private void CancelCurrentScreen()
        {
            if (currentFocus == MenuFocus.Detail)
            {
                currentFocus = MenuFocus.Primary;
                selectedDetailIndex = 0;
                selectedEquipmentItemIndex = 0;
                return;
            }

            if (currentFocus == MenuFocus.SubDetail)
            {
                currentFocus = MenuFocus.Detail;
                selectedEquipmentItemIndex = 0;
                return;
            }

            if (currentScreen != MenuScreen.Main)
            {
                if (previousScreen != MenuScreen.Main)
                {
                    currentScreen = previousScreen;
                    previousScreen = MenuScreen.Main;
                    selectedRowIndex = selectedPreviousScreenRowIndex;
                }
                else
                {
                    currentScreen = MenuScreen.Main;
                    selectedRowIndex = GetClampedMainActionIndex();
                }

                selectedDetailIndex = 0;
                selectedEquipmentItemIndex = 0;
                detailPageIndex = 0;
                return;
            }

            isOpen = false;
        }

        private void DrawMenuGoldSummary()
        {
            var party = GetParty();
            if (party == null)
            {
                return;
            }

            GUILayout.Space(8f * GetPixelScale());
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(150f * GetPixelScale()), GUILayout.Height(42f * GetPixelScale()));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Gold: " + party.Gold, labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawMenuStatusSummary()
        {
            var party = GetParty();
            if (party == null)
            {
                return;
            }

            var members = party.ActiveMembers.ToList();
            if (members.Count == 0)
            {
                return;
            }

            GUILayout.BeginHorizontal(panelStyle, GUILayout.Height(78f * GetPixelScale()));
            foreach (var member in members)
            {
                DrawMenuStatusMember(member);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawMenuStatusMember(Hero member)
        {
            var scale = GetPixelScale();
            GUILayout.BeginHorizontal(GUILayout.Width(155f * scale));
            Sprite sprite;
            UiControls.SpriteIcon(UiAssetResolver.TryGetHeroSprite(member, out sprite) ? sprite : null, 42f * scale, uiTheme);
            GUILayout.BeginVertical(GUILayout.Width(104f * scale));
            GUILayout.Label(member.Name, smallStyle);
            DrawMiniProgress("HP", member.Health, member.MaxHealth, GetHealthColor(member.Health, member.MaxHealth));
            if (member.MaxMagic > 0)
            {
                DrawMiniProgress("MP", member.Magic, member.MaxMagic, Color.blue);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawMiniProgress(string label, int value, int maxValue, Color color)
        {
            var scale = GetPixelScale();
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, smallStyle, GUILayout.Width(22f * scale));
            DrawProgressBar(maxValue <= 0 ? 0f : Mathf.Clamp01((float)value / maxValue), 70f * scale, 10f * scale, color);
            GUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Dungeon Escape", titleStyle);
            GUILayout.FlexibleSpace();
            if (UiControls.Button("Main Menu", buttonStyle, GUILayout.Width(120f * GetPixelScale())))
            {
                ConfirmReturnToMainMenu();
            }

            if (UiControls.Button("Quit", buttonStyle, GUILayout.Width(96f * GetPixelScale())))
            {
                ConfirmQuitGame();
            }

            if (UiControls.Button("Close", buttonStyle, GUILayout.Width(96f * GetPixelScale())))
            {
                isOpen = false;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            DrawTab(MenuTab.Party, "Party");
            DrawTab(MenuTab.Quests, "Quests");
            DrawTab(MenuTab.Save, "Save");
            DrawTab(MenuTab.Settings, "Settings");
            GUILayout.EndHorizontal();
            GUILayout.Space(10f * GetPixelScale());
        }

        private void DrawTab(MenuTab tab, string label)
        {
            if (UiControls.TabButton(label, currentTab == tab, uiTheme, 34f * GetPixelScale()))
            {
                currentTab = tab;
                selectedRowIndex = 0;
            }
        }

        private void DrawCurrentTab()
        {
            drawingRowIndex = 0;
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Mathf.Max(GetSelectableRowCount() - 1, 0));
            switch (currentScreen)
            {
                case MenuScreen.Main:
                    DrawMainActionList();
                    break;
                case MenuScreen.Items:
                    DrawMemberDetailScreen("Items", DrawMenuItemsList, DrawSelectedMenuItemDetail);
                    break;
                case MenuScreen.Spells:
                    DrawMemberDetailScreen("Spells", DrawMenuSpellsList, DrawSelectedMenuSpellDetail);
                    break;
                case MenuScreen.Abilities:
                    DrawMemberDetailScreen("Abilities", DrawMenuAbilitiesList, DrawSelectedMenuAbilityDetail);
                    break;
                case MenuScreen.Equipment:
                    DrawMemberDetailScreen("Equipment", DrawMenuEquipmentList, DrawSelectedMenuEquipmentDetail);
                    break;
                case MenuScreen.Status:
                    DrawMemberDetailScreen("Status", null, DrawSelectedMenuStatusDetail);
                    break;
                case MenuScreen.Party:
                    DrawMemberDetailScreen("Party", null, DrawSelectedMenuStatusDetail);
                    break;
                case MenuScreen.Misc:
                    DrawMiscActionList();
                    break;
                case MenuScreen.Save:
                    DrawSave();
                    break;
                case MenuScreen.Load:
                    DrawLoad();
                    break;
                case MenuScreen.Settings:
                    DrawSettings();
                    break;
            }
        }

        private void DrawMainActionList()
        {
            var actions = GetMainMenuActions();
            selectedMainActionIndex = Mathf.Clamp(selectedMainActionIndex, 0, Math.Max(actions.Count - 1, 0));
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(actions.Count - 1, 0));
            DrawActionList(actions, selectedRowIndex, true);
        }

        private void DrawMiscActionList()
        {
            var actions = GetMiscMenuActions();
            DrawActionList(actions, selectedRowIndex, true);
        }

        private void DrawActionList(IList<string> actions, int selectedIndex, bool centeredPanel)
        {
            var scale = GetPixelScale();
            var width = centeredPanel ? 260f * scale : 176f * scale;
            if (centeredPanel)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical(panelStyle, GUILayout.Width(width));
            }
            else
            {
                GUILayout.BeginVertical(GUILayout.Width(width));
            }

            for (var i = 0; i < actions.Count; i++)
            {
                var selected = i == selectedIndex;
                var rowHeight = centeredPanel ? 34f * scale : GetLeftActionRowHeight(scale);
                GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(rowHeight));
                GUILayout.Label(actions[i], GetMenuListLabelStyle(selected), GUILayout.Height(rowHeight));
                GUILayout.EndHorizontal();
                SelectMenuRowOnMouseClick(i, ActivateSelectedRow);
            }

            GUILayout.EndVertical();
            if (centeredPanel)
            {
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private List<string> GetMainMenuActions()
        {
            var actions = new List<string>
            {
                "Items"
            };

            if (AnyMemberHasUsableMapSpells())
            {
                actions.Add("Spells");
            }

            actions.Add("Equipment");

            if (AnyMemberHasUsableMapAbilities())
            {
                actions.Add("Abilities");
            }

            actions.Add("Status");
            if (CanManagePartyMembers())
            {
                actions.Add("Party");
            }

            actions.Add("Misc.");
            return actions;
        }

        private static List<string> GetMiscMenuActions()
        {
            return new List<string>
            {
                "Save",
                "Load",
                "Settings",
                "Exit to Main",
                "Quit"
            };
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
            var partyPanelHeight = Mathf.Max(120f * GetPixelScale(), menuBodyHeight - 34f * GetPixelScale());
            var activeMembers = party.ActiveMembers.ToList();
            var inactive = party.InactiveMembers.ToList();
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Mathf.Max(activeMembers.Count + inactive.Count - 1, 0));

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(340f * GetPixelScale()), GUILayout.Height(partyPanelHeight));
            partyScrollPosition = BeginThemedScroll(
                partyScrollPosition,
                Mathf.Max(100f * GetPixelScale(), partyPanelHeight - 28f * GetPixelScale()));
            GUILayout.Label("Active Party (" + activeMembers.Count + "/" + GetMaxPartyMembers() + ")", titleStyle);
            for (var i = 0; i < activeMembers.Count; i++)
            {
                BeginSelectableRow();
                DrawHeroStatus(activeMembers[i], true);
                EndSelectableRow();
                SelectRowOnMouseClick(i);
            }

            if (inactive.Count > 0)
            {
                GUILayout.Space(10f * GetPixelScale());
                GUILayout.Label("Reserve", titleStyle);
                for (var i = 0; i < inactive.Count; i++)
                {
                    BeginSelectableRow();
                    DrawHeroStatus(inactive[i], false);
                    EndSelectableRow();
                    SelectRowOnMouseClick(activeMembers.Count + i);
                }
            }
            EndThemedScroll();
            GUILayout.EndVertical();

            var selectedHero = GetSelectedPartyHero(activeMembers, inactive);
            if (selectedHero != null)
            {
                GUILayout.Space(10f * GetPixelScale());
                DrawPartyDetail(selectedHero, partyPanelHeight);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawHeroStatus(Hero hero, bool active)
        {
            GUILayout.Label(hero.Name + (active ? "" : "  Reserve"), labelStyle);
        }

        private void DrawMemberDetailScreen(
            string title,
            Action<Hero> drawDetailList,
            Action<Hero> drawDetail)
        {
            var party = GetParty();
            if (party == null)
            {
                GUILayout.Label("No party loaded.", labelStyle);
                return;
            }

            var members = GetMenuMembers(party);
            if (members.Count == 0)
            {
                GUILayout.Label("No party members.", labelStyle);
                return;
            }

            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, members.Count - 1);
            var hero = members[selectedRowIndex];
            GUILayout.Label(title, titleStyle);
            GUILayout.Space(6f * GetPixelScale());
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(230f * GetPixelScale()), GUILayout.Height(menuBodyHeight));
            for (var i = 0; i < members.Count; i++)
            {
                DrawMenuMemberRow(members[i], i);
                SelectMenuRowOnMouseClick(i, ActivateCurrentMemberDetailScreen);
            }

            GUILayout.EndVertical();
            GUILayout.Space(10f * GetPixelScale());

            if (drawDetailList != null)
            {
                GUILayout.BeginVertical(panelStyle, GUILayout.Width(310f * GetPixelScale()), GUILayout.Height(menuBodyHeight));
                drawDetailList(hero);
                GUILayout.EndVertical();
                GUILayout.Space(10f * GetPixelScale());
            }

            var showDetailPanel =
                !(currentScreen == MenuScreen.Items && currentFocus != MenuFocus.Detail) &&
                !(currentScreen == MenuScreen.Spells && currentFocus != MenuFocus.Detail) &&
                !(currentScreen == MenuScreen.Abilities && currentFocus != MenuFocus.Detail) &&
                !(currentScreen == MenuScreen.Equipment && currentFocus == MenuFocus.Primary);
            if (showDetailPanel)
            {
                GUILayout.BeginVertical(panelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(menuBodyHeight));
                if (drawDetail != null)
                {
                    drawDetail(hero);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawMenuItemsList(Hero hero)
        {
            var items = hero == null || hero.Items == null ? new List<ItemInstance>() : hero.Items.ToList();
            DrawPagedItemList(hero, items);
        }

        private void DrawSelectedMenuItemDetail(Hero hero)
        {
            if (currentFocus != MenuFocus.Detail)
            {
                return;
            }

            var items = hero == null || hero.Items == null ? new List<ItemInstance>() : hero.Items.ToList();
            if (items.Count == 0)
            {
                GUILayout.Label("No items.", labelStyle);
                return;
            }

            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex, 0, items.Count - 1);
            DrawInventoryItemDetail(hero, items[selectedDetailIndex], false, false);
        }

        private void DrawMenuSpellsList(Hero hero)
        {
            var spells = GetKnownSpells(hero);
            DrawPagedSpellList(spells);
        }

        private void DrawSelectedMenuSpellDetail(Hero hero)
        {
            var spells = GetKnownSpells(hero);
            if (spells.Count == 0)
            {
                GUILayout.Label("No spells.", labelStyle);
                return;
            }

            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex, 0, spells.Count - 1);
            var spell = spells[selectedDetailIndex];
            DrawSpellDetailHeader(spell);
        }

        private void DrawMenuAbilitiesList(Hero hero)
        {
            var skills = GetKnownSkills(hero);
            DrawPagedIconList(skills.Count, i => skills[i].Name, i => GetSkillSprite(skills[i]), i =>
            {
                selectedDetailIndex = i;
                currentFocus = MenuFocus.Detail;
            });
        }

        private void DrawSelectedMenuAbilityDetail(Hero hero)
        {
            var skills = GetKnownSkills(hero);
            if (skills.Count == 0)
            {
                GUILayout.Label("No abilities.", labelStyle);
                return;
            }

            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex, 0, skills.Count - 1);
            var skill = skills[selectedDetailIndex];
            DrawSkillDetailHeader(skill);
        }

        private void DrawMenuEquipmentList(Hero hero)
        {
            var slots = GetEquipmentSlots();
            if (slots.Count == 0)
            {
                GUILayout.Label("None.", labelStyle);
                return;
            }

            const int pageSize = 10;
            var maxPage = Math.Max(0, (slots.Count - 1) / pageSize);
            detailPageIndex = Mathf.Clamp(detailPageIndex, 0, maxPage);
            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex, 0, slots.Count - 1);
            var start = detailPageIndex * pageSize;
            var end = Math.Min(slots.Count, start + pageSize);
            for (var i = start; i < end; i++)
            {
                DrawEquipmentSlotSelectionRow(hero, slots[i], i);
            }

            GUILayout.FlexibleSpace();
            DrawPageLabel(maxPage);
        }

        private void DrawSelectedMenuEquipmentDetail(Hero hero)
        {
            if (currentFocus == MenuFocus.Primary)
            {
                return;
            }

            DrawEquipmentCandidateList(hero);
        }

        private void DrawSelectedMenuStatusDetail(Hero hero)
        {
            DrawPartyStatusDetail(hero);
        }

        private void DrawMenuMemberRow(Hero hero, int index)
        {
            if (hero == null)
            {
                return;
            }

            var selected = index == selectedRowIndex;
            var rowHeight = 38f * GetPixelScale();
            GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(rowHeight));
            Sprite sprite;
            DrawSpriteIconNoFrame(UiAssetResolver.TryGetHeroSprite(hero, out sprite) ? sprite : null, 32f * GetPixelScale());

            GUILayout.Label(hero.Name + (hero.IsActive ? string.Empty : "  Reserve"), GetMenuListLabelStyle(selected), GUILayout.Height(rowHeight));
            GUILayout.EndHorizontal();
        }

        private void DrawPagedItemList(Hero hero, IList<ItemInstance> items)
        {
            if (items == null || items.Count == 0)
            {
                GUILayout.Label("None.", labelStyle);
                return;
            }

            const int pageSize = 10;
            var maxPage = Math.Max(0, (items.Count - 1) / pageSize);
            detailPageIndex = Mathf.Clamp(detailPageIndex, 0, maxPage);
            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex, 0, items.Count - 1);
            var start = detailPageIndex * pageSize;
            var end = Math.Min(items.Count, start + pageSize);
            for (var i = start; i < end; i++)
            {
                DrawMenuItemRow(hero, items[i], i);
            }

            GUILayout.FlexibleSpace();
            DrawPageLabel(maxPage);
        }

        private void DrawMenuItemRow(Hero hero, ItemInstance item, int index)
        {
            var selected = index == selectedDetailIndex;
            var rowHeight = 36f * GetPixelScale();
            GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(rowHeight));
            Sprite sprite;
            DrawSpriteIconNoFrame(UiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null, 32f * GetPixelScale());
            GUILayout.Label(item.NameWithStats, GetItemRowLabelStyle(item), GUILayout.Height(rowHeight));
            GUILayout.FlexibleSpace();
            if (item.IsEquipped)
            {
                GUILayout.Label("E", GetEquippedMarkerStyle(selected), GUILayout.Width(18f * GetPixelScale()), GUILayout.Height(rowHeight));
            }

            GUILayout.EndHorizontal();
            HandleDetailRowMouseClick(index, () => ShowPartyItemActionModal(hero, item));
        }

        private void DrawEquipmentSlotSelectionRow(Hero hero, Slot slot, int index)
        {
            var selected = index == selectedDetailIndex;
            var rowHeight = 36f * GetPixelScale();
            var equipped = GetEquippedItem(hero, slot);
            GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(rowHeight));
            Sprite sprite;
            DrawSpriteIconNoFrame(equipped != null && UiAssetResolver.TryGetItemSprite(equipped, out sprite) ? sprite : null, 32f * GetPixelScale());
            GUILayout.Label(slot + ": " + (equipped == null ? "Empty" : equipped.Name), GetEquipmentSlotLabelStyle(equipped, selected), GUILayout.Height(rowHeight));
            GUILayout.EndHorizontal();
            HandleDetailRowMouseClick(index, () => SelectEquipmentSlot(hero));
        }

        private void DrawEquipmentCandidateList(Hero hero)
        {
            var slots = GetEquipmentSlots();
            if (hero == null || selectedDetailIndex < 0 || selectedDetailIndex >= slots.Count)
            {
                return;
            }

            var slot = slots[selectedDetailIndex];
            var candidates = GetEquipmentCandidates(hero, slot);
            GUILayout.Label(slot.ToString(), labelStyle);
            if (candidates.Count == 0)
            {
                GUILayout.Label("No equipment available.", smallStyle);
                return;
            }

            selectedEquipmentItemIndex = Mathf.Clamp(selectedEquipmentItemIndex, 0, candidates.Count - 1);
            for (var i = 0; i < candidates.Count; i++)
            {
                DrawEquipmentCandidateRow(hero, candidates[i], i);
            }
        }

        private void DrawEquipmentCandidateRow(Hero hero, ItemInstance item, int index)
        {
            var selected = index == selectedEquipmentItemIndex;
            var rowHeight = 38f * GetPixelScale();
            GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(rowHeight));
            Sprite sprite;
            DrawSpriteIconNoFrame(UiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null, 32f * GetPixelScale());
            GUILayout.Label(item.NameWithStats, GetRarityStyle(item, GetMenuListLabelStyle(selected)), GUILayout.Height(rowHeight));
            GUILayout.FlexibleSpace();
            if (item.IsEquipped)
            {
                GUILayout.Label("E", GetEquippedMarkerStyle(selected), GUILayout.Width(18f * GetPixelScale()), GUILayout.Height(rowHeight));
            }

            GUILayout.EndHorizontal();
            HandleEquipmentCandidateMouseClick(index, () => EquipSelectedEquipmentCandidate(hero));
        }

        private void DrawPagedList(int count, Func<int, string> getLabel, Action<int> clicked)
        {
            if (count == 0)
            {
                GUILayout.Label("None.", labelStyle);
                return;
            }

            const int pageSize = 10;
            var maxPage = Math.Max(0, (count - 1) / pageSize);
            detailPageIndex = Mathf.Clamp(detailPageIndex, 0, maxPage);
            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex, 0, count - 1);
            var start = detailPageIndex * pageSize;
            var end = Math.Min(count, start + pageSize);
            for (var i = start; i < end; i++)
            {
                var selected = i == selectedDetailIndex;
                GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(30f * GetPixelScale()));
                GUILayout.Label(getLabel(i), GetMenuListLabelStyle(selected));
                GUILayout.EndHorizontal();
                HandleDetailRowMouseClick(i, () => clicked(i));
            }

            GUILayout.FlexibleSpace();
            DrawPageLabel(maxPage);
        }

        private void DrawPagedIconList(int count, Func<int, string> getLabel, Func<int, Sprite> getSprite, Action<int> clicked)
        {
            if (count == 0)
            {
                GUILayout.Label("None.", labelStyle);
                return;
            }

            const int pageSize = 10;
            var maxPage = Math.Max(0, (count - 1) / pageSize);
            detailPageIndex = Mathf.Clamp(detailPageIndex, 0, maxPage);
            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex, 0, count - 1);
            var start = detailPageIndex * pageSize;
            var end = Math.Min(count, start + pageSize);
            var rowHeight = 36f * GetPixelScale();
            for (var i = start; i < end; i++)
            {
                var selected = i == selectedDetailIndex;
                GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(rowHeight));
                DrawSpriteIconNoFrame(getSprite == null ? null : getSprite(i), 32f * GetPixelScale());
                GUILayout.Label(getLabel(i), GetMenuListLabelStyle(selected), GUILayout.Height(rowHeight));
                GUILayout.EndHorizontal();
                HandleDetailRowMouseClick(i, () => clicked(i));
            }

            GUILayout.FlexibleSpace();
            DrawPageLabel(maxPage);
        }

        private void DrawPagedSpellList(IList<Spell> spells)
        {
            if (spells == null || spells.Count == 0)
            {
                GUILayout.Label("None.", labelStyle);
                return;
            }

            const int pageSize = 10;
            var maxPage = Math.Max(0, (spells.Count - 1) / pageSize);
            detailPageIndex = Mathf.Clamp(detailPageIndex, 0, maxPage);
            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex, 0, spells.Count - 1);
            var start = detailPageIndex * pageSize;
            var end = Math.Min(spells.Count, start + pageSize);
            var rowHeight = 36f * GetPixelScale();
            for (var i = start; i < end; i++)
            {
                var spell = spells[i];
                var selected = i == selectedDetailIndex;
                GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(rowHeight));
                Sprite sprite;
                DrawSpriteIconNoFrame(UiAssetResolver.TryGetSpellSprite(spell, out sprite) ? sprite : null, 32f * GetPixelScale());
                GUILayout.Label(spell.Name, GetMenuListLabelStyle(selected), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();
                GUILayout.Label(spell.Cost + " MP", GetMenuListLabelStyle(selected), GUILayout.Width(58f * GetPixelScale()), GUILayout.Height(rowHeight));
                GUILayout.EndHorizontal();
                HandleDetailRowMouseClick(i, () =>
                {
                    selectedDetailIndex = i;
                    currentFocus = MenuFocus.Detail;
                });
            }

            GUILayout.FlexibleSpace();
            DrawPageLabel(maxPage);
        }

        private void DrawPageLabel(int maxPage)
        {
            if (maxPage > 0)
            {
                GUILayout.Label("Page " + (detailPageIndex + 1) + "/" + (maxPage + 1), smallStyle);
            }
        }

        private void DrawSpellDetailHeader(Spell spell)
        {
            if (spell == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            Sprite sprite;
            DrawSpriteIconNoFrame(UiAssetResolver.TryGetSpellSprite(spell, out sprite) ? sprite : null, 48f * GetPixelScale());
            GUILayout.BeginVertical();
            GUILayout.Label(spell.Name, labelStyle);
            GUILayout.Label(spell.Type + "  " + spell.Targets, smallStyle);
            GUILayout.Label("MP: " + spell.Cost, smallStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawSkillDetailHeader(Skill skill)
        {
            if (skill == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            DrawSpriteIconNoFrame(GetSkillSprite(skill), 48f * GetPixelScale());
            GUILayout.BeginVertical();
            GUILayout.Label(skill.Name, labelStyle);
            GUILayout.Label(skill.Type + "  " + skill.Targets, smallStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static Sprite GetSkillSprite(Skill skill)
        {
            if (skill == null || GameDataCache.Current == null || GameDataCache.Current.Spells == null)
            {
                return null;
            }

            var spell = GameDataCache.Current.Spells.FirstOrDefault(item =>
                item != null && string.Equals(item.SkillId, skill.Name, StringComparison.OrdinalIgnoreCase));
            Sprite sprite;
            return UiAssetResolver.TryGetSpellSprite(spell, out sprite) ? sprite : null;
        }

        private void DrawPartyDetail(Hero hero, float height)
        {
            EnsureVisiblePartyDetailTab(hero);
            GUILayout.BeginVertical(panelStyle, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            GUILayout.Label(hero.Name, titleStyle);
            DrawPartyDetailTabs();
            GUILayout.Space(8f * GetPixelScale());
            partyDetailScrollPosition = BeginThemedScroll(
                partyDetailScrollPosition,
                Mathf.Max(100f * GetPixelScale(), height - 86f * GetPixelScale()));
            switch (currentPartyDetailTab)
            {
                case PartyDetailTab.Status:
                    DrawPartyStatusDetail(hero);
                    break;
                case PartyDetailTab.Equipment:
                    DrawPartyEquipmentDetail(hero);
                    break;
                case PartyDetailTab.Items:
                    DrawPartyItemsDetail(hero);
                    break;
                case PartyDetailTab.Skills:
                    DrawPartySkillsDetail(hero);
                    break;
                case PartyDetailTab.Spells:
                    DrawPartySpellsDetail(hero);
                    break;
            }

            EndThemedScroll();
            GUILayout.EndVertical();
        }

        private void DrawPartyDetailTabs()
        {
            GUILayout.BeginHorizontal();
            foreach (var tab in GetVisiblePartyDetailTabs(GetSelectedPartyHero()))
            {
                DrawPartyDetailTab(tab, GetPartyDetailTabLabel(tab));
            }

            GUILayout.EndHorizontal();
        }

        private static List<PartyDetailTab> GetVisiblePartyDetailTabs(Hero hero)
        {
            var tabs = new List<PartyDetailTab>
            {
                PartyDetailTab.Status,
                PartyDetailTab.Equipment,
                PartyDetailTab.Items
            };

            if (CanUseMapSkills(hero))
            {
                tabs.Add(PartyDetailTab.Skills);
            }

            if (CanUseMapSpells(hero))
            {
                tabs.Add(PartyDetailTab.Spells);
            }

            return tabs;
        }

        private static string GetPartyDetailTabLabel(PartyDetailTab tab)
        {
            return tab.ToString();
        }

        private void DrawPartyDetailTab(PartyDetailTab tab, string label)
        {
            if (UiControls.TabButton(label, currentPartyDetailTab == tab, uiTheme, 30f * GetPixelScale()))
            {
                currentPartyDetailTab = tab;
            }
        }

        private void DrawPartyStatusDetail(Hero hero)
        {
            GUILayout.BeginHorizontal();
            Sprite sprite;
            UiControls.SpriteIcon(
                UiAssetResolver.TryGetHeroSprite(hero, out sprite) ? sprite : null,
                72f * GetPixelScale(),
                uiTheme);
            GUILayout.BeginVertical();
            GUILayout.Label("Level " + hero.Level + " " + hero.Class + "  " + hero.Gender, labelStyle);
            GUILayout.Space(4f * GetPixelScale());
            DrawProgressValue("HP", hero.Health, hero.MaxHealth, hero.Health + " / " + hero.MaxHealth, GetHealthColor(hero.Health, hero.MaxHealth));
            if (HeroHasMagic(hero))
            {
                GUILayout.Space(3f * GetPixelScale());
                DrawProgressValue("MP", hero.Magic, hero.MaxMagic, hero.Magic + " / " + hero.MaxMagic, Color.blue);
            }

            GUILayout.Space(3f * GetPixelScale());
            DrawProgressValue("XP", hero.Xp, hero.NextLevel, hero.Xp + " / " + hero.NextLevel + " (" + GetXpToNextLevel(hero) + " to next)");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Attributes", labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            DrawDetailValue("Attack", hero.Attack.ToString());
            DrawDetailValue("Defence", hero.Defence.ToString());
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            if (HeroHasMagic(hero))
            {
                DrawDetailValue("Magic Defence", hero.MagicDefence.ToString());
            }

            DrawDetailValue("Agility", hero.Agility.ToString());
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            DrawStatusEffects(hero);
            DrawStatusSpellAbilityColumns(hero);
        }

        private void DrawPartyEquipmentDetail(Hero hero)
        {
            GUILayout.Label("Equipment", labelStyle);
            foreach (Slot slot in Enum.GetValues(typeof(Slot)))
            {
                DrawEquipmentSlot(hero, slot);
            }
        }

        private void DrawPartyItemsDetail(Hero hero)
        {
            GUILayout.Label("Items (" + hero.Items.Count + "/" + Party.MaxItems + ")", titleStyle);
            if (hero.Items.Count == 0)
            {
                GUILayout.Label("No items.", smallStyle);
                return;
            }

            selectedPartyItemIndex = Mathf.Clamp(selectedPartyItemIndex, 0, hero.Items.Count - 1);
            inventoryScrollPosition = BeginThemedScroll(inventoryScrollPosition, 172f * GetPixelScale());
            for (var i = 0; i < hero.Items.Count; i++)
            {
                var item = hero.Items[i];
                Sprite sprite;
                GUILayout.BeginHorizontal();
                UiControls.SpriteIcon(
                    UiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                    32f * GetPixelScale(),
                    uiTheme);
                if (UiControls.Button(
                        item.NameWithStats + (item.IsEquipped ? " [E]" : ""),
                        GetLeftAlignedButtonStyle(selectedPartyItemIndex == i),
                        GUILayout.Height(32f * GetPixelScale())))
                {
                    selectedPartyItemIndex = i;
                }

                GUILayout.EndHorizontal();
            }

            EndThemedScroll();
            GUILayout.Space(8f * GetPixelScale());
            DrawInventoryItemDetail(hero, hero.Items[selectedPartyItemIndex], false, false);
        }

        private void DrawPartySkillsDetail(Hero hero)
        {
            DrawKnownSkills(hero);
        }

        private void DrawPartySpellsDetail(Hero hero)
        {
            DrawKnownSpells(hero);
        }

        private void DrawDetailValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", smallStyle, GUILayout.Width(112f * GetPixelScale()));
            GUILayout.Label(value, smallStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawProgressValue(string label, ulong value, ulong maxValue, string text)
        {
            DrawProgressValue(label, value, maxValue, text, Color.white);
        }

        private void DrawProgressValue(string label, ulong value, ulong maxValue, string text, Color fillColor)
        {
            var scale = GetPixelScale();
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", smallStyle, GUILayout.Width(36f * scale));
            DrawProgressBar(maxValue == 0 ? 0f : Mathf.Clamp01((float)((double)value / maxValue)), 170f * scale, 20f * scale, fillColor);
            GUILayout.Label(text, smallStyle, GUILayout.Width(190f * scale));
            GUILayout.EndHorizontal();
        }

        private void DrawProgressValue(string label, int value, int maxValue, string text)
        {
            DrawProgressValue(label, (ulong)Math.Max(0, value), (ulong)Math.Max(0, maxValue), text);
        }

        private void DrawProgressValue(string label, int value, int maxValue, string text, Color fillColor)
        {
            DrawProgressValue(label, (ulong)Math.Max(0, value), (ulong)Math.Max(0, maxValue), text, fillColor);
        }

        private void DrawProgressBar(float progress, float width, float height)
        {
            DrawProgressBar(progress, width, height, Color.white);
        }

        private void DrawProgressBar(float progress, float width, float height, Color fillColor)
        {
            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            GUI.Box(rect, GUIContent.none, uiTheme == null ? GUI.skin.box : uiTheme.ButtonStyle);

            var previousColor = GUI.color;
            GUI.color = fillColor;
            var inset = uiTheme == null ? 2f : Mathf.Max(2f, uiTheme.BorderThickness);
            GUI.DrawTexture(
                new Rect(
                    rect.x + inset,
                    rect.y + inset,
                    Mathf.Max(0f, rect.width - inset * 2f) * progress,
                    Mathf.Max(0f, rect.height - inset * 2f)),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static Color GetHealthColor(int currentHealth, int maxHealth)
        {
            if (maxHealth <= 0 || currentHealth <= 0)
            {
                return Color.red;
            }

            var progress = Mathf.Clamp01((float)currentHealth / maxHealth);
            if (progress < 0.1f)
            {
                return new Color(1f, 0.55f, 0f, 1f);
            }

            return progress < 0.5f ? Color.yellow : Color.green;
        }

        private GUIStyle GetLeftAlignedButtonStyle(bool selected)
        {
            var style = new GUIStyle(uiTheme == null
                ? GUI.skin.button
                : selected ? uiTheme.SelectedTabStyle : uiTheme.ButtonStyle)
            {
                alignment = TextAnchor.MiddleLeft
            };
            return style;
        }

        private GUIStyle GetHighlightedMenuLabelStyle()
        {
            return GetMenuListLabelStyle(true);
        }

        private GUIStyle GetMenuListLabelStyle(bool selected)
        {
            var style = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleLeft
            };
            if (selected && uiTheme != null)
            {
                style.normal.textColor = uiTheme.HighlightColor;
            }

            return style;
        }

        private GUIStyle GetItemRowLabelStyle(ItemInstance item)
        {
            var style = new GUIStyle(GetRarityStyle(item, labelStyle))
            {
                alignment = TextAnchor.MiddleLeft
            };
            return style;
        }

        private GUIStyle GetEquipmentSlotLabelStyle(ItemInstance item, bool selected)
        {
            var style = new GUIStyle(GetRarityStyle(item, GetMenuListLabelStyle(selected)))
            {
                alignment = TextAnchor.MiddleLeft
            };
            return style;
        }

        private GUIStyle GetEquippedMarkerStyle(bool selected)
        {
            var style = GetMenuListLabelStyle(selected);
            style.alignment = TextAnchor.MiddleRight;
            return style;
        }

        private GUIStyle GetListRowStyle(bool selected)
        {
            if (!selected)
            {
                return GUIStyle.none;
            }

            return uiTheme == null ? GUI.skin.box : uiTheme.SelectedRowStyle;
        }

        private static void DrawSpriteIconNoFrame(Sprite sprite, float size)
        {
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var texCoords = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);

            var aspect = textureRect.height <= 0f ? 1f : textureRect.width / textureRect.height;
            var drawWidth = rect.width;
            var drawHeight = aspect <= 0f ? rect.height : drawWidth / aspect;
            if (drawHeight > rect.height)
            {
                drawHeight = rect.height;
                drawWidth = drawHeight * aspect;
            }

            var drawRect = new Rect(
                rect.x + (rect.width - drawWidth) / 2f,
                rect.y + (rect.height - drawHeight) / 2f,
                drawWidth,
                drawHeight);
            GUI.DrawTextureWithTexCoords(drawRect, texture, texCoords, true);
        }

        private static ulong GetXpToNextLevel(Hero hero)
        {
            return hero == null || hero.Xp >= hero.NextLevel ? 0 : hero.NextLevel - hero.Xp;
        }

        private void DrawEquipmentSlot(Hero hero, Slot slot)
        {
            var item = GetEquippedItem(hero, slot);
            GUILayout.BeginHorizontal();
            GUILayout.Label(slot + ":", labelStyle, GUILayout.Width(98f * GetPixelScale()));
            if (item != null)
            {
                Sprite sprite;
                UiControls.SpriteIcon(
                    UiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                    32f * GetPixelScale(),
                    uiTheme);
            }

            GUILayout.Label(item == null ? "Empty" : item.NameWithStats, GetRarityStyle(item, labelStyle));
            GUILayout.EndHorizontal();
        }

        private void DrawStatusEffects(Hero hero)
        {
            if (hero.Status == null || hero.Status.Count == 0)
            {
                return;
            }

            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Effects", labelStyle);
            foreach (var effect in hero.Status)
            {
                var duration = GetStatusEffectDurationText(effect);
                var stat = effect.StatType == StatType.None ? "" : "  " + effect.StatType + " " + effect.StatValue;
                GUILayout.Label(effect.Name + stat + duration, smallStyle);
            }
        }

        private string GetStatusEffectDurationText(StatusEffect effect)
        {
            if (effect == null || effect.Duration <= 0)
            {
                return "";
            }

            var elapsed = 0;
            var party = GetParty();
            if (effect.DurationType == DurationType.Distance && party != null)
            {
                elapsed = party.StepCount - effect.StartTime;
            }

            var remaining = Math.Max(0, effect.Duration - elapsed);
            switch (effect.DurationType)
            {
                case DurationType.Distance:
                    return "  " + remaining + " steps left";
                case DurationType.Rounds:
                    return "  " + remaining + " rounds left";
                default:
                    return "  " + remaining + " " + effect.DurationType + " left";
            }
        }

        private void DrawStatusSpellAbilityColumns(Hero hero)
        {
            GUILayout.Space(8f * GetPixelScale());
            GUILayout.BeginHorizontal();
            if (HeroHasMagic(hero))
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.Label("Spells", labelStyle);
                DrawStatusSpellList(GetKnownSpells(hero));
                GUILayout.EndVertical();
                GUILayout.Space(12f * GetPixelScale());
            }

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Abilities", labelStyle);
            DrawStatusSkillList(GetKnownSkills(hero));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawStatusSpellList(IList<Spell> spells)
        {
            if (spells == null || spells.Count == 0)
            {
                GUILayout.Label("None", smallStyle);
                return;
            }

            foreach (var spell in spells)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(28f * GetPixelScale()));
                Sprite sprite;
                DrawSpriteIconNoFrame(UiAssetResolver.TryGetSpellSprite(spell, out sprite) ? sprite : null, 24f * GetPixelScale());
                GUILayout.Label(spell.Name, smallStyle);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawStatusSkillList(IList<Skill> skills)
        {
            if (skills == null || skills.Count == 0)
            {
                GUILayout.Label("None", smallStyle);
                return;
            }

            foreach (var skill in skills)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(28f * GetPixelScale()));
                DrawSpriteIconNoFrame(GetSkillSprite(skill), 24f * GetPixelScale());
                GUILayout.Label(skill.Name, smallStyle);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawKnownSkills(Hero hero)
        {
            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Skills", labelStyle);
            var skills = GameDataCache.Current == null || GameDataCache.Current.Skills == null
                ? new List<Skill>()
                : hero.GetSkills(GameDataCache.Current.Skills).ToList();
            if (skills.Count == 0)
            {
                GUILayout.Label("None", smallStyle);
                return;
            }

            foreach (var skill in skills)
            {
                GUILayout.Label(skill.Name + "  " + skill.Type + "  " + skill.Targets, smallStyle);
            }
        }

        private void DrawKnownSpells(Hero hero)
        {
            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Spells", labelStyle);
            var spells = GameDataCache.Current == null || GameDataCache.Current.Spells == null
                ? new List<Spell>()
                : hero.GetSpells(GameDataCache.Current.Spells).ToList();
            if (spells.Count == 0)
            {
                GUILayout.Label("None", smallStyle);
                return;
            }

            foreach (var spell in spells)
            {
                var canCast = gameState != null && CanCastSpellFromPartyMenu(hero, spell);
                Sprite sprite;
                GUILayout.BeginHorizontal();
                UiControls.SpriteIcon(
                    UiAssetResolver.TryGetSpellSprite(spell, out sprite) ? sprite : null,
                    32f * GetPixelScale(),
                    uiTheme);
                GUILayout.Label(spell.Name, labelStyle);
                if (canCast && UiControls.Button("Cast", buttonStyle, GUILayout.Width(86f * GetPixelScale())))
                {
                    ShowSpellTargetPicker(hero, spell);
                }

                GUILayout.EndHorizontal();
            }
        }

        private bool CanCastSpellFromPartyMenu(Hero caster, Spell spell)
        {
            if (gameState == null || !gameState.CanCastHeroSpell(caster, spell))
            {
                return false;
            }

            if (spell.Type == SkillType.Outside)
            {
                return gameState.CanCastOutside();
            }

            if (spell.Type == SkillType.Return)
            {
                return gameState.CanCastReturn();
            }

            return true;
        }

        private void ShowSpellTargetPicker(Hero caster, Spell spell)
        {
            EnsureReferences();
            if (gameState == null || caster == null || spell == null)
            {
                return;
            }

            if (!CanCastSpellFromPartyMenu(caster, spell))
            {
                ShowPartyMessage("Cannot cast spell.");
                return;
            }

            if (spell.Type == SkillType.Outside)
            {
                ShowPartyMessage(gameState.CastOutsideSpell(caster, spell));
                return;
            }

            if (spell.Type == SkillType.Return)
            {
                ShowReturnLocationPicker(caster, spell);
                return;
            }

            if (spell.Type == SkillType.Open)
            {
                ShowPartyMessage(CastSpellOnFacingObject(caster, spell));
                return;
            }

            switch (spell.Targets)
            {
                case Target.Group:
                    ShowPartyMessage(gameState.CastHeroSpellOnParty(caster, spell));
                    return;
                case Target.None:
                    ShowPartyMessage(gameState.CastHeroSpellWithoutTarget(caster, spell));
                    return;
                case Target.Single:
                    ShowSingleSpellTargetPicker(caster, spell);
                    return;
                case Target.Object:
                    ShowPartyMessage(CastSpellOnFacingObject(caster, spell));
                    return;
                default:
                    ShowPartyMessage("That spell cannot be cast from the party menu.");
                    return;
            }
        }

        private void ShowReturnLocationPicker(Hero caster, Spell spell)
        {
            var locations = gameState.GetReturnLocations().ToList();
            if (locations.Count == 0)
            {
                ShowPartyMessage(gameState.Party != null && gameState.Party.CurrentMapIsOverWorld
                    ? "You have not visited any places to return to."
                    : "Return can only be used outside.");
                return;
            }

            var labels = locations
                .Select(location => string.IsNullOrEmpty(location.DisplayName) ? location.MapId : location.DisplayName)
                .ToList();
            labels.Add("Cancel");
            ShowMenuModal("Return", "Choose a place to return to.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= locations.Count)
                {
                    return;
                }

                ShowPartyMessage(gameState.CastReturnSpell(caster, spell, locations[selectedIndex]));
            });
        }

        private void ShowSingleSpellTargetPicker(Hero caster, Spell spell)
        {
            var party = GetParty();
            if (party == null)
            {
                ShowPartyMessage("No party loaded.");
                return;
            }

            var targets = GetValidSpellTargets(party, spell).ToList();
            if (targets.Count == 0)
            {
                ShowPartyMessage("No party members can be targeted.");
                return;
            }

            var labels = targets.Select(target => target.Name).ToList();
            labels.Add("Cancel");
            var choiceHeroes = targets.Cast<Hero>().Concat(new Hero[] { null }).ToList();
            ShowMenuModal("Cast " + spell.Name, "Choose a target.", labels, choiceHeroes, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= targets.Count)
                {
                    return;
                }

                ShowPartyMessage(gameState.CastHeroSpell(caster, spell, targets[selectedIndex]));
            });
        }

        private static IEnumerable<Hero> GetValidSpellTargets(Party party, Spell spell)
        {
            if (party == null)
            {
                return Enumerable.Empty<Hero>();
            }

            return spell != null && spell.Type == SkillType.Revive
                ? party.DeadMembers
                : party.AliveMembers;
        }

        private Hero GetSelectedPartyHero(IList<Hero> activeMembers, IList<Hero> inactiveMembers)
        {
            if (selectedRowIndex < activeMembers.Count)
            {
                return activeMembers[selectedRowIndex];
            }

            var reserveIndex = selectedRowIndex - activeMembers.Count;
            return reserveIndex >= 0 && reserveIndex < inactiveMembers.Count ? inactiveMembers[reserveIndex] : null;
        }

        private Hero GetSelectedPartyHero()
        {
            var party = GetParty();
            if (party == null)
            {
                return null;
            }

            return GetSelectedPartyHero(party.ActiveMembers.ToList(), party.InactiveMembers.ToList());
        }

        private Hero GetSelectedMenuHero()
        {
            var party = GetParty();
            if (party == null)
            {
                return null;
            }

            var members = GetMenuMembers(party);
            return selectedRowIndex >= 0 && selectedRowIndex < members.Count ? members[selectedRowIndex] : null;
        }

        private static bool IsMemberDetailScreen(MenuScreen screen)
        {
            return screen == MenuScreen.Items ||
                   screen == MenuScreen.Spells ||
                   screen == MenuScreen.Equipment ||
                   screen == MenuScreen.Abilities ||
                   screen == MenuScreen.Status ||
                   screen == MenuScreen.Party;
        }

        private static bool IsPagedDetailScreen(MenuScreen screen)
        {
            return screen == MenuScreen.Items ||
                   screen == MenuScreen.Spells ||
                   screen == MenuScreen.Equipment ||
                   screen == MenuScreen.Abilities;
        }

        private int GetCurrentDetailCount()
        {
            var hero = GetSelectedMenuHero();
            if (hero == null)
            {
                return 0;
            }

            switch (currentScreen)
            {
                case MenuScreen.Items:
                    return hero.Items == null ? 0 : hero.Items.Count;
                case MenuScreen.Spells:
                    return GetKnownSpells(hero).Count;
                case MenuScreen.Abilities:
                    return GetKnownSkills(hero).Count;
                case MenuScreen.Equipment:
                    return GetEquipmentSlots().Count;
                default:
                    return 0;
            }
        }

        private static List<Spell> GetKnownSpells(Hero hero)
        {
            return hero == null || !HeroHasMagic(hero) || GameDataCache.Current == null || GameDataCache.Current.Spells == null
                ? new List<Spell>()
                : hero.GetSpells(GameDataCache.Current.Spells)
                    .Where(spell => spell != null && spell.IsNonEncounterSpell)
                    .ToList();
        }

        private static List<Skill> GetKnownSkills(Hero hero)
        {
            return hero == null || GameDataCache.Current == null || GameDataCache.Current.Skills == null
                ? new List<Skill>()
                : hero.GetSkills(GameDataCache.Current.Skills)
                    .Where(skill => skill != null && skill.IsNonEncounterSkill)
                    .ToList();
        }

        private List<Hero> GetMenuMembers(Party party)
        {
            var members = GetInventoryMembers(party);
            switch (currentScreen)
            {
                case MenuScreen.Spells:
                    return members.Where(CanUseMapSpells).ToList();
                case MenuScreen.Abilities:
                    return members.Where(CanUseMapSkills).ToList();
                default:
                    return members;
            }
        }

        private bool AnyMemberHasUsableMapSpells()
        {
            var party = GetParty();
            return party != null && GetInventoryMembers(party).Any(CanUseMapSpells);
        }

        private bool AnyMemberHasUsableMapAbilities()
        {
            var party = GetParty();
            return party != null && GetInventoryMembers(party).Any(CanUseMapSkills);
        }

        private bool CanManagePartyMembers()
        {
            var party = GetParty();
            return party != null && GetInventoryMembers(party).Count > 1;
        }

        private static List<Slot> GetEquipmentSlots()
        {
            return Enum.GetValues(typeof(Slot)).Cast<Slot>().ToList();
        }

        private List<ItemInstance> GetEquipmentCandidates(Hero hero, Slot slot)
        {
            var candidates = new List<ItemInstance>();
            if (hero == null || hero.Items == null)
            {
                return candidates;
            }

            var equipped = GetEquippedItem(hero, slot);
            if (equipped != null)
            {
                candidates.Add(equipped);
            }

            candidates.AddRange(hero.Items.Where(item =>
                item != null &&
                !item.IsEquipped &&
                item.Slots != null &&
                item.Slots.Contains(slot) &&
                hero.CanEquipItem(item)));
            return candidates;
        }

        private void SelectEquipmentSlot(Hero hero)
        {
            var slots = GetEquipmentSlots();
            if (hero == null || selectedDetailIndex < 0 || selectedDetailIndex >= slots.Count)
            {
                return;
            }

            var candidates = GetEquipmentCandidates(hero, slots[selectedDetailIndex]);
            if (candidates.Count == 0)
            {
                selectedEquipmentItemIndex = 0;
                return;
            }

            var equipped = GetEquippedItem(hero, slots[selectedDetailIndex]);
            selectedEquipmentItemIndex = equipped == null ? 0 : Math.Max(0, candidates.IndexOf(equipped));
            currentFocus = MenuFocus.SubDetail;
            BlockMenuInteractUntilRelease();
        }

        private void EquipSelectedEquipmentCandidate(Hero hero)
        {
            var slots = GetEquipmentSlots();
            if (hero == null || selectedDetailIndex < 0 || selectedDetailIndex >= slots.Count)
            {
                return;
            }

            var candidates = GetEquipmentCandidates(hero, slots[selectedDetailIndex]);
            if (selectedEquipmentItemIndex < 0 || selectedEquipmentItemIndex >= candidates.Count)
            {
                return;
            }

            var item = candidates[selectedEquipmentItemIndex];
            if (item.IsEquipped)
            {
                ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
            }
            else
            {
                ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
            }

            var updatedCandidates = GetEquipmentCandidates(hero, slots[selectedDetailIndex]);
            selectedEquipmentItemIndex = Mathf.Clamp(selectedEquipmentItemIndex, 0, Math.Max(updatedCandidates.Count - 1, 0));
        }

        private void ShowEquipmentActionModal(Hero hero)
        {
            var slots = GetEquipmentSlots();
            if (hero == null || selectedDetailIndex < 0 || selectedDetailIndex >= slots.Count)
            {
                return;
            }

            var slot = slots[selectedDetailIndex];
            var equipped = GetEquippedItem(hero, slot);
            var choices = new List<string>();
            var actions = new List<Action>();
            if (equipped != null)
            {
                choices.Add("Unequip");
                actions.Add(() => ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, equipped)));
            }

            var candidates = hero.Items == null
                ? new List<ItemInstance>()
                : hero.Items
                    .Where(item => item != null && !item.IsEquipped && item.Slots != null && item.Slots.Contains(slot) && hero.CanEquipItem(item))
                    .ToList();
            foreach (var candidate in candidates)
            {
                var item = candidate;
                choices.Add("Equip " + item.NameWithStats);
                actions.Add(() => ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item)));
            }

            choices.Add("Cancel");
            ShowMenuModal(slot.ToString(), "Choose equipment.", choices, index =>
            {
                if (index >= 0 && index < actions.Count)
                {
                    actions[index]();
                }
            });
        }

        private void EnsureVisiblePartyDetailTab(Hero hero)
        {
            if (currentPartyDetailTab == PartyDetailTab.Skills && !CanUseMapSkills(hero) ||
                currentPartyDetailTab == PartyDetailTab.Spells && !CanUseMapSpells(hero))
            {
                currentPartyDetailTab = PartyDetailTab.Status;
            }
        }

        private static bool HasKnownSkills(Hero hero)
        {
            return hero != null &&
                   GameDataCache.Current != null &&
                   GameDataCache.Current.Skills != null &&
                   hero.GetSkills(GameDataCache.Current.Skills).Any(skill => skill != null && skill.IsNonEncounterSkill);
        }

        private static bool HasKnownSpells(Hero hero)
        {
            return hero != null &&
                   HeroHasMagic(hero) &&
                   GameDataCache.Current != null &&
                   GameDataCache.Current.Spells != null &&
                   hero.GetSpells(GameDataCache.Current.Spells).Any(spell => spell != null && spell.IsNonEncounterSpell);
        }

        private static bool CanUseMapSkills(Hero hero)
        {
            return hero != null && !hero.IsDead && HasKnownSkills(hero);
        }

        private static bool CanUseMapSpells(Hero hero)
        {
            return hero != null && !hero.IsDead && HasKnownSpells(hero);
        }

        private static bool HeroHasMagic(Hero hero)
        {
            return hero != null && hero.MaxMagic > 0;
        }

        private static ItemInstance GetEquippedItem(Hero hero, Slot slot)
        {
            if (hero == null || hero.Slots == null || hero.Items == null)
            {
                return null;
            }

            string itemId;
            if (!hero.Slots.TryGetValue(slot, out itemId) || string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            return hero.Items.FirstOrDefault(item => item != null && item.Id == itemId);
        }

        private void ApplyPartyChange(Func<bool> action)
        {
            EnsureReferences();
            if (action == null || !action())
            {
                return;
            }

            var player = FindAnyObjectByType<PlayerGridController>();
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

            var members = GetInventoryMembers(party);
            if (members.Count == 0)
            {
                GUILayout.Label("No party members.", labelStyle);
                return;
            }

            selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, members.Count - 1);
            GUILayout.BeginHorizontal();
            for (var i = 0; i < members.Count; i++)
            {
                if (UiControls.Button(members[i].Name, selectedHeroIndex == i, uiTheme))
                {
                    selectedHeroIndex = i;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f * GetPixelScale());

            var hero = members[selectedHeroIndex];
            GUILayout.BeginHorizontal();
            GUILayout.Label(hero.Name + "'s Inventory (" + hero.Items.Count + "/" + Party.MaxItems + ")", titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Gold: " + party.Gold, labelStyle);
            GUILayout.EndHorizontal();
            if (hero.Items.Count == 0)
            {
                GUILayout.Label("No items.", labelStyle);
                return;
            }

            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, hero.Items.Count - 1);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.MinWidth(420f * GetPixelScale()));
            inventoryScrollPosition = BeginThemedScroll(inventoryScrollPosition, Mathf.Max(120f * GetPixelScale(), menuBodyHeight - 82f * GetPixelScale()));
            for (var i = 0; i < hero.Items.Count; i++)
            {
                var item = hero.Items[i];
                var equipped = item.IsEquipped ? " [E]" : "";
                BeginSelectableRow();
                GUILayout.BeginHorizontal();
                Sprite sprite;
                UiControls.SpriteIcon(
                    UiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                    36f * GetPixelScale(),
                    uiTheme);
                GUILayout.Label(item.NameWithStats + equipped, GetRarityStyle(item, labelStyle));
                if (item.IsEquipped)
                {
                if (UiControls.Button("Unequip", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
                    {
                        ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
                    }
                }
                else if (hero.CanEquipItem(item))
                {
                if (UiControls.Button("Equip", buttonStyle, GUILayout.Width(112f * GetPixelScale())))
                    {
                        ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
                    }
                }

                GUILayout.EndHorizontal();
                EndSelectableRow();
                SelectRowOnMouseClick(i);
            }

            EndThemedScroll();
            GUILayout.EndVertical();
            GUILayout.Space(10f * GetPixelScale());
            DrawInventoryItemDetail(hero, hero.Items[selectedRowIndex]);
            GUILayout.EndHorizontal();
        }

        private void DrawInventoryItemDetail(Hero hero, ItemInstance item, bool framed = true, bool showActions = true)
        {
            if (framed)
            {
                GUILayout.BeginVertical(panelStyle, GUILayout.Width(320f * GetPixelScale()));
            }
            else
            {
                GUILayout.BeginVertical();
            }
            if (item == null)
            {
                GUILayout.Label("No item selected.", labelStyle);
                GUILayout.EndVertical();
                return;
            }

            Sprite sprite;
            GUILayout.BeginHorizontal();
            UiControls.SpriteIcon(
                UiAssetResolver.TryGetItemSprite(item, out sprite) ? sprite : null,
                64f * GetPixelScale(),
                uiTheme);
            GUILayout.BeginVertical();
            GUILayout.Label(item.Name, GetRarityStyle(item, labelStyle));
            GUILayout.Label(GetItemDetailSubtitle(item), smallStyle);
            if (!string.IsNullOrEmpty(item.Item.StatString))
            {
                GUILayout.Label("Stats: " + item.Item.StatString, smallStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f * GetPixelScale());
            if (item.MaxCharges > 0)
            {
                GUILayout.Label("Charges: " + item.Charges + "/" + item.MaxCharges, smallStyle);
            }

            if (!showActions)
            {
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Space(8f * GetPixelScale());
            if (gameState != null && CanUseItemFromInventory(hero, item))
            {
            if (UiControls.Button("Use", buttonStyle))
                {
                    ShowUseItemTargetPicker(hero, item);
                }
            }

            if (item.IsEquipped)
            {
            if (UiControls.Button("Unequip", buttonStyle))
                {
                    ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
                }
            }
            else if (hero.CanEquipItem(item))
            {
            if (UiControls.Button("Equip", buttonStyle))
                {
                    ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
                }
            }

            if (HasTransferTarget(hero))
            {
            if (UiControls.Button("Transfer", buttonStyle))
                {
                    ShowTransferItemTargetPicker(hero, item);
                }
            }

            if (item.Type != ItemType.Quest)
            {
            if (UiControls.Button("Drop", buttonStyle))
                {
                    ShowDropItemConfirmation(hero, item);
                }
            }

            GUILayout.EndVertical();
        }

        private static string GetItemDetailSubtitle(ItemInstance item)
        {
            if (item == null)
            {
                return "";
            }

            var subtitle = item.Type == ItemType.Quest
                ? item.Type.ToString()
                : item.Rarity + " " + item.Type;
            return item.IsEquipped ? subtitle + "  Equipped" : subtitle;
        }

        private bool HasTransferTarget(Hero source)
        {
            var party = GetParty();
            return party != null &&
                   source != null &&
                   GetInventoryMembers(party).Any(member => !ReferenceEquals(member, source) && member.Items.Count < Party.MaxItems);
        }

        private void ShowTransferItemTargetPicker(Hero source, ItemInstance item)
        {
            var party = GetParty();
            if (party == null || source == null || item == null)
            {
                return;
            }

            var targets = GetInventoryMembers(party)
                .Where(member => !ReferenceEquals(member, source) && member.Items.Count < Party.MaxItems)
                .ToList();
            if (targets.Count == 0)
            {
                ShowInventoryMessage("No party member has room for " + item.Name + ".");
                return;
            }

            var labels = targets.Select(target => target.Name).ToList();
            labels.Add("Cancel");
            var choiceHeroes = targets.Cast<Hero>().Concat(new Hero[] { null }).ToList();
            ShowMenuModal("Transfer " + item.Name, "Choose who should carry this item.", labels, choiceHeroes, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= targets.Count)
                {
                    return;
                }

                var target = targets[selectedIndex];
                if (gameState.TransferHeroItem(source, target, item))
                {
                    ClampSelectedItemIndex(source);
                    ShowInventoryMessage(source.Name + " gave " + item.Name + " to " + target.Name + ".");
                }
                else
                {
                    ShowInventoryMessage("Could not transfer " + item.Name + ".");
                }
            });
        }

        private void ShowDropItemConfirmation(Hero hero, ItemInstance item)
        {
            if (hero == null || item == null)
            {
                return;
            }

            if (item.Type == ItemType.Quest)
            {
                ShowInventoryMessage("Quest items cannot be dropped.");
                return;
            }

            ShowMenuModal("Drop " + item.Name, "Discard this item?", new[] { "Drop", "Cancel" }, selectedIndex =>
            {
                if (selectedIndex != 0)
                {
                    return;
                }

                if (gameState.DropHeroItem(hero, item))
                {
                    ClampSelectedItemIndex(hero);
                    ShowInventoryMessage(item.Name + " was dropped.");
                }
                else
                {
                    ShowInventoryMessage("Could not drop " + item.Name + ".");
                }
            });
        }

        private void ShowUseItemTargetPicker(Hero hero, ItemInstance item)
        {
            EnsureReferences();
            if (gameState == null || hero == null || item == null)
            {
                return;
            }

            if (item.Item != null && item.Item.Skill != null && item.Item.Skill.Type == SkillType.Outside)
            {
                ShowInventoryMessage(gameState.UseOutsideItem(hero, item));
                ClampSelectedItemIndex(hero);
                return;
            }

            if (item.Item != null && item.Item.Skill != null && item.Item.Skill.Type == SkillType.Return)
            {
                ShowReturnLocationPicker(hero, item);
                return;
            }

            if (item.Item != null && item.Item.Skill != null && item.Item.Skill.Type == SkillType.Open)
            {
                ShowInventoryMessage(UseItemOnFacingObject(hero, item));
                ClampSelectedItemIndex(hero);
                return;
            }

            switch (item.Target)
            {
                case Target.Group:
                    ShowInventoryMessage(gameState.UseHeroItemOnParty(hero, item));
                    ClampSelectedItemIndex(hero);
                    return;
                case Target.None:
                    ShowInventoryMessage(gameState.UseHeroItem(hero, item, hero));
                    ClampSelectedItemIndex(hero);
                    return;
                case Target.Single:
                    ShowSingleItemTargetPicker(hero, item);
                    return;
                case Target.Object:
                    ShowInventoryMessage(UseItemOnFacingObject(hero, item));
                    ClampSelectedItemIndex(hero);
                    return;
                default:
                    ShowInventoryMessage("That item cannot be used from inventory.");
                    return;
            }
        }

        private static string UseItemOnFacingObject(Hero hero, ItemInstance item)
        {
            var player = FindAnyObjectByType<PlayerGridController>();
            return player == null
                ? "There is no map object target."
                : player.UseItemOnFacingObject(hero, item);
        }

        private static string CastSpellOnFacingObject(Hero hero, Spell spell)
        {
            var player = FindAnyObjectByType<PlayerGridController>();
            return player == null
                ? "There is no map object target."
                : player.CastSpellOnFacingObject(hero, spell);
        }

        private void ShowSingleItemTargetPicker(Hero hero, ItemInstance item)
        {
            var party = GetParty();
            if (party == null)
            {
                ShowInventoryMessage("No party loaded.");
                return;
            }

            var targets = party.ActiveMembers.ToList();
            if (targets.Count == 0)
            {
                ShowInventoryMessage("No party members can be targeted.");
                return;
            }

            var labels = targets.Select(target => target.Name).ToList();
            labels.Add("Cancel");
            var choiceHeroes = targets.Cast<Hero>().Concat(new Hero[] { null }).ToList();
            ShowMenuModal("Use " + item.Name, "Choose a target.", labels, choiceHeroes, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= targets.Count)
                {
                    return;
                }

                ShowInventoryMessage(gameState.UseHeroItem(hero, item, targets[selectedIndex]));
                ClampSelectedItemIndex(hero);
            });
        }

        private bool CanUseItemFromInventory(Hero hero, ItemInstance item)
        {
            if (gameState == null || !gameState.CanUseHeroItem(hero, item))
            {
                return false;
            }

            if (item != null && item.Item != null && item.Item.Skill != null)
            {
                if (item.Item.Skill.Type == SkillType.Outside)
                {
                    return gameState.CanCastOutside();
                }

                if (item.Item.Skill.Type == SkillType.Return)
                {
                    return gameState.CanCastReturn();
                }
            }

            return true;
        }

        private void ShowReturnLocationPicker(Hero hero, ItemInstance item)
        {
            var locations = gameState.GetReturnLocations().ToList();
            if (locations.Count == 0)
            {
                ShowInventoryMessage(gameState.Party != null && gameState.Party.CurrentMapIsOverWorld
                    ? "You have not visited any places to return to."
                    : "Return can only be used outside.");
                return;
            }

            var labels = locations
                .Select(location => string.IsNullOrEmpty(location.DisplayName) ? location.MapId : location.DisplayName)
                .ToList();
            labels.Add("Cancel");
            ShowMenuModal("Return", "Choose a place to return to.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= locations.Count)
                {
                    return;
                }

                ShowInventoryMessage(gameState.UseReturnItem(hero, item, locations[selectedIndex]));
                ClampSelectedItemIndex(hero);
            });
        }

        private void ShowInventoryMessage(string message)
        {
            ShowMenuModal("Inventory", string.IsNullOrEmpty(message) ? "Done." : message, null, null);
        }

        private void ShowPartyMessage(string message)
        {
            ShowMenuModal("Party", string.IsNullOrEmpty(message) ? "Done." : message, null, null);
        }

        private void ShowMenuModal(string title, string message, IEnumerable<string> choices, Action<int> selected)
        {
            ShowMenuModal(title, message, choices, null, selected);
        }

        private void ShowMenuModal(string title, string message, IEnumerable<string> choices, IEnumerable<Hero> choiceHeroes, Action<int> selected)
        {
            menuModalTitle = title;
            menuModalMessage = message;
            menuModalChoices = choices == null ? null : choices.ToList();
            menuModalChoiceHeroes = choiceHeroes == null ? null : choiceHeroes.ToList();
            menuModalSelected = selected;
            menuModalSelectedIndex = 0;
            menuModalWaitingForConfirmRelease =
                InputManager.GetCommand(InputCommand.Interact) || UnityEngine.Input.GetMouseButton(0);
            ResetMenuNavigationRepeat();
        }

        private bool IsMenuModalVisible()
        {
            return !string.IsNullOrEmpty(menuModalMessage);
        }

        private bool MenuModalHasChoices()
        {
            return menuModalChoices != null && menuModalChoices.Count > 0;
        }

        private void HideMenuModal()
        {
            menuModalTitle = null;
            menuModalMessage = null;
            menuModalChoices = null;
            menuModalChoiceHeroes = null;
            menuModalSelected = null;
            menuModalSelectedIndex = 0;
            menuModalWaitingForConfirmRelease = false;
            ResetMenuNavigationRepeat();
        }

        private void UpdateMenuModal()
        {
            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                HideMenuModal();
                return;
            }

            if (menuModalWaitingForConfirmRelease)
            {
                if (InputManager.GetCommand(InputCommand.Interact) || UnityEngine.Input.GetMouseButton(0))
                {
                    return;
                }

                menuModalWaitingForConfirmRelease = false;
            }

            if (!MenuModalHasChoices())
            {
                if (InputManager.GetCommandDown(InputCommand.Interact))
                {
                    HideMenuModal();
                }

                return;
            }

            var moveY = GetMenuMoveY();
            if (moveY < 0)
            {
                menuModalSelectedIndex = Mathf.Max(0, menuModalSelectedIndex - 1);
            }
            else if (moveY > 0)
            {
                menuModalSelectedIndex = Mathf.Min(menuModalChoices.Count - 1, menuModalSelectedIndex + 1);
            }

            if (menuModalChoices.Count <= 2)
            {
                var moveX = InputManager.GetMoveXDown();
                if (moveX < 0)
                {
                    menuModalSelectedIndex = Mathf.Max(0, menuModalSelectedIndex - 1);
                }
                else if (moveX > 0)
                {
                    menuModalSelectedIndex = Mathf.Min(menuModalChoices.Count - 1, menuModalSelectedIndex + 1);
                }
            }

            if (InputManager.GetCommandDown(InputCommand.Interact))
            {
                SelectMenuModalChoice(menuModalSelectedIndex);
            }
        }

        private void SelectMenuModalChoice(int index)
        {
            if (!MenuModalHasChoices() || index < 0 || index >= menuModalChoices.Count)
            {
                return;
            }

            var selected = menuModalSelected;
            HideMenuModal();
            if (selected != null)
            {
                selected(index);
            }
        }

        private void BlockMenuInteractUntilRelease()
        {
            acceptMenuInteractAfterFrame = Time.frameCount + 1;
            waitForMenuInteractRelease = true;
        }

        private bool CanAcceptMenuInteract()
        {
            if (Time.frameCount <= acceptMenuInteractAfterFrame)
            {
                return false;
            }

            return !waitForMenuInteractRelease;
        }

        private void UpdateMenuInteractRelease()
        {
            if (!waitForMenuInteractRelease || Time.frameCount <= acceptMenuInteractAfterFrame)
            {
                return;
            }

            if (!InputManager.GetCommand(InputCommand.Interact))
            {
                waitForMenuInteractRelease = false;
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

        private void ClampSelectedItemIndex(Hero hero)
        {
            var maxIndex = hero == null || hero.Items == null ? 0 : Math.Max(hero.Items.Count - 1, 0);
            if (currentTab == MenuTab.Party)
            {
                selectedPartyItemIndex = Mathf.Clamp(selectedPartyItemIndex, 0, maxIndex);
            }
            else
            {
                selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, maxIndex);
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

            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, party.ActiveQuests.Count - 1);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.MinWidth(420f * GetPixelScale()));
            questScrollPosition = BeginThemedScroll(questScrollPosition, menuBodyHeight);
            for (var i = 0; i < party.ActiveQuests.Count; i++)
            {
                var activeQuest = party.ActiveQuests[i];
                Quest quest;
                if (GameDataCache.Current == null ||
                    !GameDataCache.Current.TryGetQuest(activeQuest.Id, out quest))
                {
                    BeginSelectableRow();
                    GUILayout.Label(activeQuest.Id + (activeQuest.Completed ? " (Finished)" : ""), labelStyle);
                    EndSelectableRow();
                    SelectRowOnMouseClick(i);
                    continue;
                }

                BeginSelectableRow();
                GUILayout.BeginVertical();
                GUILayout.Label(quest.Name + (activeQuest.Completed ? " (Finished)" : ""), labelStyle);
                var currentStage = quest.Stages == null
                    ? null
                    : quest.Stages.FirstOrDefault(stage => stage.Number == activeQuest.CurrentStage);
                if (currentStage != null && !string.IsNullOrEmpty(currentStage.Description))
                {
                    GUILayout.Label(currentStage.Description, smallStyle);
                }

                GUILayout.EndVertical();
                EndSelectableRow();
                SelectRowOnMouseClick(i);
            }
            EndThemedScroll();
            GUILayout.EndVertical();
            GUILayout.Space(10f * GetPixelScale());
            DrawQuestDetail(party.ActiveQuests[selectedRowIndex]);
            GUILayout.EndHorizontal();
        }

        private void DrawQuestDetail(ActiveQuest activeQuest)
        {
            GUILayout.BeginVertical(panelStyle, GUILayout.Width(360f * GetPixelScale()));
            if (activeQuest == null)
            {
                GUILayout.Label("No quest selected.", labelStyle);
                GUILayout.EndVertical();
                return;
            }

            Quest quest = null;
            if (GameDataCache.Current != null)
            {
                GameDataCache.Current.TryGetQuest(activeQuest.Id, out quest);
            }

            GUILayout.Label(quest == null ? activeQuest.Id : quest.Name, titleStyle);
            GUILayout.Label(activeQuest.Completed ? "Completed" : "Active", labelStyle);
            GUILayout.Label("Current Stage: " + activeQuest.CurrentStage, smallStyle);

            if (quest == null)
            {
                GUILayout.Label("Quest data was not found.", smallStyle);
                GUILayout.EndVertical();
                return;
            }

            if (!string.IsNullOrEmpty(quest.Description))
            {
                GUILayout.Space(8f * GetPixelScale());
                GUILayout.Label(quest.Description, smallStyle);
            }

            var currentStage = quest.Stages == null
                ? null
                : quest.Stages.FirstOrDefault(stage => stage.Number == activeQuest.CurrentStage);
            if (currentStage != null && !string.IsNullOrEmpty(currentStage.Description))
            {
                GUILayout.Space(8f * GetPixelScale());
                GUILayout.Label("Stage", labelStyle);
                GUILayout.Label(currentStage.Description, smallStyle);
            }

            GUILayout.Space(8f * GetPixelScale());
            GUILayout.Label("Rewards", labelStyle);
            GUILayout.Label("XP: " + quest.Xp + "    Gold: " + quest.Gold, smallStyle);
            if (quest.Items != null && quest.Items.Count > 0)
            {
                GUILayout.Label("Items: " + string.Join(", ", quest.Items.ToArray()), smallStyle);
            }

            if (quest.Stages != null && quest.Stages.Count > 0)
            {
                GUILayout.Space(8f * GetPixelScale());
                GUILayout.Label("Stages", labelStyle);
                foreach (var stage in quest.Stages.OrderBy(stage => stage.Number))
                {
                    var marker = stage.Number == activeQuest.CurrentStage ? "> " : "  ";
                    GUILayout.Label(marker + stage.Number + ": " + stage.Description, smallStyle);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawSave()
        {
            EnsureReferences();
            if (gameState == null)
            {
                GUILayout.Label("Game state is not loaded.", labelStyle);
                return;
            }

            var saves = gameState.GetManualSaveSlots();
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, saves.Count);

            GUILayout.BeginVertical();
            saveScrollPosition = BeginThemedScroll(saveScrollPosition, Mathf.Max(120f * GetPixelScale(), menuBodyHeight));
            for (var i = 0; i < saves.Count; i++)
            {
                BeginSelectableRow();
                DrawSaveRow(saves[i]);
                EndSelectableRow();
                SelectSaveRowOnMouseClick(i);
            }

            BeginSelectableRow();
            DrawNewSaveRow();
            EndSelectableRow();
            SelectSaveRowOnMouseClick(saves.Count);

            EndThemedScroll();
            GUILayout.EndVertical();
        }

        private void DrawLoad()
        {
            EnsureReferences();
            if (gameState == null)
            {
                GUILayout.Label("Game state is not loaded.", labelStyle);
                return;
            }

            var saves = gameState.GetManualSaveSlots();
            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(saves.Count - 1, 0));
            if (saves.Count == 0)
            {
                GUILayout.Label("No saves.", labelStyle);
                return;
            }

            saveScrollPosition = BeginThemedScroll(saveScrollPosition, Mathf.Max(120f * GetPixelScale(), menuBodyHeight));
            for (var i = 0; i < saves.Count; i++)
            {
                BeginSelectableRow();
                DrawSaveRow(saves[i]);
                EndSelectableRow();
                SelectRowOnMouseClick(i);
            }

            EndThemedScroll();
        }

        private void DrawSaveRow(GameSave save)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(GetSaveTitle(save), labelStyle);
            GUILayout.Label(GetSaveSummary(save), smallStyle);
            GUILayout.EndVertical();
        }

        private void DrawNewSaveRow()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("New Save", labelStyle);
            GUILayout.Label("Save the current quest.", smallStyle);
            GUILayout.EndVertical();
        }

        private void ConfirmSaveManual(int slotIndex, GameSave save)
        {
            if (IsUsableSave(save))
            {
                ShowMenuModal(
                    "Save",
                    "Save over this quest?",
                    new[] { "Save", "Cancel" },
                    index =>
                    {
                        if (index == 0)
                        {
                            SaveManual(slotIndex);
                        }
                    });
                return;
            }

            SaveManual(slotIndex);
        }

        private void SaveManual(int slotIndex)
        {
            ShowSaveMessage(gameState != null && gameState.SaveManual(slotIndex)
                ? "Saved quest."
                : "Could not save quest.");
        }

        private void ConfirmLoadManual(int slotIndex)
        {
            ShowMenuModal(
                "Load",
                "Load this quest? Unsaved progress will be lost.",
                new[] { "Load", "Cancel" },
                index =>
                {
                    if (index == 0)
                    {
                        if (gameState != null && gameState.LoadManual(slotIndex))
                        {
                            isOpen = false;
                        }
                        else
                        {
                            ShowSaveMessage("Could not load quest.");
                        }
                    }
                });
        }

        private void ConfirmDeleteManual(int slotIndex)
        {
            ShowMenuModal(
                "Delete",
                "Delete this quest?",
                new[] { "Delete", "Cancel" },
                index =>
                {
                    if (index == 0)
                    {
                        var deleted = gameState != null && gameState.DeleteManual(slotIndex);
                        if (deleted && gameState != null)
                        {
                            selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, gameState.GetManualSaveSlots().Count);
                        }

                        ShowSaveMessage(deleted ? "Deleted quest." : "Could not delete quest.");
                    }
                });
        }

        private void ShowSaveActionModal(int rowIndex)
        {
            if (gameState == null || rowIndex < 0)
            {
                return;
            }

            var saves = gameState.GetManualSaveSlots();
            if (rowIndex >= saves.Count)
            {
                ShowMenuModal(
                    "New Save",
                    "Create a new manual save?",
                    new[] { "Save", "Cancel" },
                    index =>
                    {
                        if (index == 0)
                        {
                            SaveManual(rowIndex);
                        }
                    });
                return;
            }

            var save = saves[rowIndex];
            ShowMenuModal(
                GetSaveTitle(save),
                "Choose an action for this save.",
                new[] { "Save Over", "Load", "Delete", "Cancel" },
                index =>
                {
                    switch (index)
                    {
                        case 0:
                            ConfirmSaveManual(rowIndex, save);
                            break;
                        case 1:
                            ConfirmLoadManual(rowIndex);
                            break;
                        case 2:
                            ConfirmDeleteManual(rowIndex);
                            break;
                    }
                });
        }

        private void ConfirmRestartNewGame()
        {
            ShowMenuModal(
                "New Game",
                "Start a new game? Unsaved progress will be lost.",
                new[] { "Start", "Cancel" },
                index =>
                {
                    if (index == 0 && gameState != null)
                    {
                        gameState.RestartNewGame();
                        isOpen = false;
                    }
                });
        }

        private void ConfirmReturnToMainMenu()
        {
            ShowMenuModal(
                "Main Menu",
                "Return to the main menu? Unsaved progress will be lost.",
                new[] { "OK", "Back" },
                index =>
                {
                    if (index == 0)
                    {
                        isOpen = false;
                        TitleMenu.OpenMainMenu();
                    }
                });
        }

        private void ConfirmQuitGame()
        {
            ShowMenuModal(
                "Quit",
                "Quit the game? Unsaved progress will be lost.",
                new[] { "Quit", "Back" },
                index =>
                {
                    if (index == 0)
                    {
                        Application.Quit();
                    }
                });
        }

        private void ShowSaveMessage(string message)
        {
            ShowMenuModal("Save", string.IsNullOrEmpty(message) ? "Done." : message, null, null);
        }

        private static string GetSaveTitle(GameSave save)
        {
            return GameState.GetGameSaveTitle(save);
        }

        private static string GetSaveSummary(GameSave save)
        {
            return GameState.GetGameSaveSummary(save);
        }

        private static bool IsUsableSave(GameSave save)
        {
            return GameState.IsUsableGameSave(save);
        }

        private void DrawSettings()
        {
            var settings = SettingsCache.Current;
            if (settings == null)
            {
                GUILayout.Label("Settings are not loaded.", labelStyle);
                return;
            }

            EnsureVisibleSettingsTab(settings);
            DrawSettingsTabs();

            settingsScrollPosition = BeginThemedScroll(settingsScrollPosition, Mathf.Max(120f * GetPixelScale(), menuBodyHeight - 50f * GetPixelScale()));
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

        private static string GetBindingSlotLabel(string slot)
        {
            return slot == "Primary" ? "Key" : slot;
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

        private void DrawMenuModalOverlay()
        {
            if (!IsMenuModalVisible() || rebindingInput != null)
            {
                return;
            }

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 620f * scale);
            var hasChoices = MenuModalHasChoices();
            var choiceCount = hasChoices ? menuModalChoices.Count : 1;
            var compactDialog = !hasChoices || choiceCount <= 2;
            var height = compactDialog
                ? 170f * scale
                : Mathf.Min(Screen.height - 48f * scale, (150f + choiceCount * 42f) * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.enabled = true;
            DrawModalBackdrop();
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 18f * scale, rect.y + 16f * scale, rect.width - 36f * scale, rect.height - 32f * scale));
            if (compactDialog)
            {
                GUILayout.FlexibleSpace();
            }

            GUILayout.Label(menuModalTitle, titleStyle);
            GUILayout.Label(menuModalMessage, labelStyle);
            GUILayout.Space(10f * scale);

            if (hasChoices)
            {
                if (menuModalChoiceHeroes != null)
                {
                    for (var i = 0; i < menuModalChoices.Count; i++)
                    {
                        DrawMenuModalChoiceRow(i, 38f * scale);
                    }
                }
                else if (menuModalChoices.Count <= 2)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    for (var i = 0; i < menuModalChoices.Count; i++)
                    {
                        if (UiControls.Button(menuModalChoices[i], i == menuModalSelectedIndex, uiTheme, GUILayout.Width(120f * scale), GUILayout.Height(34f * scale)) &&
                            !menuModalWaitingForConfirmRelease)
                        {
                            SelectMenuModalChoice(i);
                            break;
                        }

                        if (i < menuModalChoices.Count - 1)
                        {
                            GUILayout.Space(10f * scale);
                        }
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    for (var i = 0; i < menuModalChoices.Count; i++)
                    {
                        if (UiControls.Button(menuModalChoices[i], i == menuModalSelectedIndex, uiTheme) &&
                            !menuModalWaitingForConfirmRelease)
                        {
                            SelectMenuModalChoice(i);
                            break;
                        }
                    }
                }
            }
            else if (UiControls.Button("OK", buttonStyle, GUILayout.Width(120f * scale)) &&
                     !menuModalWaitingForConfirmRelease)
            {
                HideMenuModal();
            }

            if (compactDialog)
            {
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndArea();
        }

        private void DrawMenuModalChoiceRow(int index, float height)
        {
            var selected = index == menuModalSelectedIndex;
            GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(height));
            var hero = index >= 0 && menuModalChoiceHeroes != null && index < menuModalChoiceHeroes.Count
                ? menuModalChoiceHeroes[index]
                : null;
            if (hero != null)
            {
                Sprite sprite;
                DrawSpriteIconNoFrame(UiAssetResolver.TryGetHeroSprite(hero, out sprite) ? sprite : null, 32f * GetPixelScale());
            }
            else
            {
                GUILayout.Space(32f * GetPixelScale());
            }

            GUILayout.Label(menuModalChoices[index], GetMenuListLabelStyle(selected), GUILayout.Height(height));
            GUILayout.EndHorizontal();
            SelectMenuModalChoiceOnMouseClick(index);
        }

        private void SelectMenuModalChoiceOnMouseClick(int index)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                return;
            }

            menuModalSelectedIndex = index;
            if (!menuModalWaitingForConfirmRelease)
            {
                SelectMenuModalChoice(index);
            }

            currentEvent.Use();
        }

        private void DrawRebindingOverlay()
        {
            if (rebindingInput == null)
            {
                return;
            }

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 620f * scale);
            var height = 150f * scale;
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.enabled = true;
            DrawModalBackdrop();
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 18f * scale, rect.y + 16f * scale, rect.width - 36f * scale, rect.height - 32f * scale));
            GUILayout.Label("Input Bindings", titleStyle);
            GUILayout.Label(GetRebindingPrompt(), labelStyle);
            GUILayout.FlexibleSpace();
            if (UiControls.Button("Cancel", buttonStyle, GUILayout.Width(120f * scale)))
            {
                rebindingInput = null;
                rebindingSlot = null;
            }

            GUILayout.EndArea();
        }

        private static void DrawModalBackdrop()
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private Vector2 BeginThemedScroll(Vector2 position, float height)
        {
            return GUILayout.BeginScrollView(
                position,
                false,
                false,
                GUIStyle.none,
                uiTheme == null ? GUI.skin.verticalScrollbar : uiTheme.VerticalScrollbarStyle,
                GUILayout.Height(height));
        }

        private static void EndThemedScroll()
        {
            GUILayout.EndScrollView();
        }

        private void BeginSelectableRow()
        {
            var rowIndex = drawingRowIndex++;
            UiControls.BeginSelectableRow(rowIndex, selectedRowIndex, uiTheme);
        }

        private static void EndSelectableRow()
        {
            UiControls.EndSelectableRow();
        }

        private void SelectRowOnMouseClick(int rowIndex)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                return;
            }

            selectedRowIndex = rowIndex;
        }

        private void SelectMenuRowOnMouseClick(int rowIndex, Action doubleClickAction)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                return;
            }

            selectedRowIndex = rowIndex;
            currentFocus = MenuFocus.Primary;
            if (currentEvent.clickCount >= 2 && doubleClickAction != null)
            {
                UiControls.PlayConfirmSound();
                doubleClickAction();
            }
            else
            {
                UiControls.PlaySelectSound();
            }

            currentEvent.Use();
        }

        private void HandleDetailRowMouseClick(int rowIndex, Action doubleClickAction)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                return;
            }

            selectedDetailIndex = rowIndex;
            detailPageIndex = rowIndex / 10;
            currentFocus = MenuFocus.Detail;
            if (currentEvent.clickCount >= 2 && doubleClickAction != null)
            {
                UiControls.PlayConfirmSound();
                doubleClickAction();
            }
            else
            {
                UiControls.PlaySelectSound();
            }

            currentEvent.Use();
        }

        private void HandleEquipmentCandidateMouseClick(int rowIndex, Action doubleClickAction)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                return;
            }

            selectedEquipmentItemIndex = rowIndex;
            currentFocus = MenuFocus.SubDetail;
            if (currentEvent.clickCount >= 2 && doubleClickAction != null)
            {
                UiControls.PlayConfirmSound();
                doubleClickAction();
            }
            else
            {
                UiControls.PlaySelectSound();
            }

            currentEvent.Use();
        }

        private void SelectSaveRowOnMouseClick(int rowIndex)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                return;
            }

            selectedRowIndex = rowIndex;
            if (currentEvent.clickCount >= 2)
            {
                UiControls.PlayConfirmSound();
                ShowSaveActionModal(rowIndex);
            }
            else
            {
                UiControls.PlaySelectSound();
            }

            currentEvent.Use();
        }

        private bool DrawCheckboxRow(bool value, string label)
        {
            return UiControls.CheckboxRow(value, label, uiTheme, GetPixelScale());
        }

        private float DrawSliderRow(string label, float value, float leftValue, float rightValue)
        {
            return UiControls.SliderRow(label, value, leftValue, rightValue, uiTheme);
        }

        private string DrawTextFieldRow(string label, string value, string fallback)
        {
            return UiControls.TextFieldRow(label, value, fallback, uiTheme);
        }

        private int GetSelectableRowCount()
        {
            var party = GetParty();
            switch (currentScreen)
            {
                case MenuScreen.Main:
                    return GetMainMenuActions().Count;
                case MenuScreen.Misc:
                    return GetMiscMenuActions().Count;
                case MenuScreen.Items:
                case MenuScreen.Spells:
                case MenuScreen.Equipment:
                case MenuScreen.Abilities:
                case MenuScreen.Status:
                case MenuScreen.Party:
                    return party == null ? 0 : GetMenuMembers(party).Count;
                case MenuScreen.Save:
                    return gameState == null ? 0 : gameState.ManualSaveSlotCount;
                case MenuScreen.Load:
                    return gameState == null ? 0 : gameState.GetManualSaveSlots().Count;
                case MenuScreen.Settings:
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
                    return 8;
                case SettingsTab.Ui:
                    return 9;
                case SettingsTab.Input:
                    return InputManager.GetBindings().Length + 2;
                case SettingsTab.Debug:
                    return 6;
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

            var previousIndex = selectedRowIndex;
            selectedRowIndex = Mathf.Clamp(selectedRowIndex + delta, 0, count - 1);
            if (selectedRowIndex != previousIndex)
            {
                UiControls.PlaySelectSound();
            }

            ScrollActiveListToSelectedRow();
        }

        private void MoveMenuSelection(int delta)
        {
            if (currentFocus == MenuFocus.SubDetail)
            {
                MoveEquipmentCandidateSelection(delta);
                return;
            }

            if (currentFocus == MenuFocus.Detail)
            {
                MoveDetailSelection(delta);
                return;
            }

            MoveSelectedRow(delta);
        }

        private void MoveDetailSelection(int delta)
        {
            var count = GetCurrentDetailCount();
            if (count <= 0)
            {
                selectedDetailIndex = 0;
                return;
            }

            var previousIndex = selectedDetailIndex;
            selectedDetailIndex = Mathf.Clamp(selectedDetailIndex + delta, 0, count - 1);
            detailPageIndex = selectedDetailIndex / 10;
            if (selectedDetailIndex != previousIndex)
            {
                UiControls.PlaySelectSound();
            }
        }

        private void MoveEquipmentCandidateSelection(int delta)
        {
            var hero = GetSelectedMenuHero();
            var slots = GetEquipmentSlots();
            if (hero == null || selectedDetailIndex < 0 || selectedDetailIndex >= slots.Count)
            {
                selectedEquipmentItemIndex = 0;
                return;
            }

            var count = GetEquipmentCandidates(hero, slots[selectedDetailIndex]).Count;
            if (count <= 0)
            {
                selectedEquipmentItemIndex = 0;
                return;
            }

            var previousIndex = selectedEquipmentItemIndex;
            selectedEquipmentItemIndex = Mathf.Clamp(selectedEquipmentItemIndex + delta, 0, count - 1);
            if (selectedEquipmentItemIndex != previousIndex)
            {
                UiControls.PlaySelectSound();
            }
        }

        private void ScrollActiveListToSelectedRow()
        {
            var y = Mathf.Max(0f, selectedRowIndex * 54f * GetPixelScale());
            switch (currentTab)
            {
                case MenuTab.Party:
                    partyScrollPosition.y = y;
                    break;
                case MenuTab.Inventory:
                    inventoryScrollPosition.y = y;
                    break;
                case MenuTab.Quests:
                    questScrollPosition.y = y;
                    break;
                case MenuTab.Save:
                    saveScrollPosition.y = y;
                    break;
                case MenuTab.Settings:
                    settingsScrollPosition.y = y;
                    break;
            }
        }

        private void AdjustSelectedRow(int delta)
        {
            if (currentTab == MenuTab.Party)
            {
                CyclePartyDetailTab(delta);
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

        private void CyclePartyDetailTab(int delta)
        {
            var tabs = GetVisiblePartyDetailTabs(GetSelectedPartyHero());
            var currentIndex = tabs.IndexOf(currentPartyDetailTab);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            var previousTab = currentPartyDetailTab;
            currentPartyDetailTab = tabs[(currentIndex + delta + tabs.Count) % tabs.Count];
            if (currentPartyDetailTab != previousTab)
            {
                UiControls.PlaySelectSound();
            }
        }

        private void AdjustMenuSelection(int delta)
        {
            if (currentScreen == MenuScreen.Settings)
            {
                AdjustSelectedSetting(delta);
            }
        }

        private void ChangeDetailPage(int delta)
        {
            var count = GetCurrentDetailCount();
            if (count <= 0)
            {
                detailPageIndex = 0;
                selectedDetailIndex = 0;
                return;
            }

            var maxPage = Math.Max(0, (count - 1) / 10);
            var previousPage = detailPageIndex;
            detailPageIndex = Mathf.Clamp(detailPageIndex + delta, 0, maxPage);
            selectedDetailIndex = Mathf.Clamp(detailPageIndex * 10, 0, count - 1);
            if (detailPageIndex != previousPage)
            {
                UiControls.PlaySelectSound();
            }
        }

        private void ActivateSelectedRow()
        {
            if (currentScreen == MenuScreen.Main)
            {
                ActivateMainAction();
                return;
            }

            if (currentScreen == MenuScreen.Misc)
            {
                ActivateMiscAction();
                return;
            }

            if (currentScreen == MenuScreen.Save)
            {
                ActivateSelectedSaveSlot();
                return;
            }

            if (currentScreen == MenuScreen.Load)
            {
                var saves = gameState == null ? new List<GameSave>() : gameState.GetManualSaveSlots().ToList();
                if (selectedRowIndex >= 0 && selectedRowIndex < saves.Count)
                {
                    ConfirmLoadManual(selectedRowIndex);
                }

                return;
            }

            if (currentScreen == MenuScreen.Settings)
            {
                ActivateSelectedSetting();
                return;
            }

            if (IsMemberDetailScreen(currentScreen))
            {
                ActivateCurrentMemberDetailScreen();
                return;
            }

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
                return;
            }

            if (currentTab == MenuTab.Save)
            {
                ActivateSelectedSaveSlot();
            }
        }

        private void ActivateMainAction()
        {
            var actions = GetMainMenuActions();
            if (selectedRowIndex < 0 || selectedRowIndex >= actions.Count)
            {
                return;
            }

            selectedMainActionIndex = selectedRowIndex;
            switch (actions[selectedRowIndex])
            {
                case "Items":
                    OpenMenuScreen(MenuScreen.Items);
                    break;
                case "Spells":
                    OpenMenuScreen(MenuScreen.Spells);
                    break;
                case "Equipment":
                    OpenMenuScreen(MenuScreen.Equipment);
                    break;
                case "Abilities":
                    OpenMenuScreen(MenuScreen.Abilities);
                    break;
                case "Status":
                    OpenMenuScreen(MenuScreen.Status);
                    break;
                case "Party":
                    OpenMenuScreen(MenuScreen.Party);
                    break;
                case "Misc.":
                    OpenMenuScreen(MenuScreen.Misc);
                    break;
            }
        }

        private void ActivateMiscAction()
        {
            switch (selectedRowIndex)
            {
                case 0:
                    OpenMenuScreen(MenuScreen.Save);
                    break;
                case 1:
                    OpenMenuScreen(MenuScreen.Load);
                    break;
                case 2:
                    OpenMenuScreen(MenuScreen.Settings);
                    break;
                case 3:
                    ConfirmReturnToMainMenu();
                    break;
                case 4:
                    ConfirmQuitGame();
                    break;
            }
        }

        private void OpenMenuScreen(MenuScreen screen)
        {
            previousScreen = currentScreen == MenuScreen.Misc &&
                (screen == MenuScreen.Save || screen == MenuScreen.Load || screen == MenuScreen.Settings)
                ? currentScreen
                : MenuScreen.Main;
            selectedPreviousScreenRowIndex = previousScreen == MenuScreen.Misc ? selectedRowIndex : 0;
            currentScreen = screen;
            currentFocus = MenuFocus.Primary;
            selectedRowIndex = 0;
            selectedDetailIndex = 0;
            selectedEquipmentItemIndex = 0;
            detailPageIndex = 0;
            saveScrollPosition = Vector2.zero;
            settingsScrollPosition = Vector2.zero;
            BlockMenuInteractUntilRelease();
        }

        private int GetClampedMainActionIndex()
        {
            var actions = GetMainMenuActions();
            if (actions.Count == 0)
            {
                selectedMainActionIndex = 0;
                return 0;
            }

            selectedMainActionIndex = Mathf.Clamp(selectedMainActionIndex, 0, actions.Count - 1);
            return selectedMainActionIndex;
        }

        private void ActivateCurrentMemberDetailScreen()
        {
            var hero = GetSelectedMenuHero();
            if (hero == null)
            {
                return;
            }

            if (currentFocus == MenuFocus.Primary)
            {
                if (currentScreen == MenuScreen.Status)
                {
                    return;
                }

                if (currentScreen == MenuScreen.Party)
                {
                    ShowPartyMemberActionModal(hero);
                    return;
                }

                currentFocus = MenuFocus.Detail;
                selectedDetailIndex = 0;
                detailPageIndex = 0;
                BlockMenuInteractUntilRelease();
                return;
            }

            switch (currentScreen)
            {
                case MenuScreen.Items:
                    var items = hero.Items == null ? new List<ItemInstance>() : hero.Items.ToList();
                    if (selectedDetailIndex >= 0 && selectedDetailIndex < items.Count)
                    {
                        ShowPartyItemActionModal(hero, items[selectedDetailIndex]);
                    }
                    break;
                case MenuScreen.Spells:
                    var spells = GetKnownSpells(hero);
                    if (selectedDetailIndex >= 0 && selectedDetailIndex < spells.Count)
                    {
                        ShowSpellTargetPicker(hero, spells[selectedDetailIndex]);
                    }
                    break;
                case MenuScreen.Abilities:
                    ShowPartyMessage("Abilities are shown for reference.");
                    break;
                case MenuScreen.Equipment:
                    if (currentFocus == MenuFocus.Detail)
                    {
                        SelectEquipmentSlot(hero);
                    }
                    else if (currentFocus == MenuFocus.SubDetail)
                    {
                        EquipSelectedEquipmentCandidate(hero);
                    }
                    break;
            }
        }

        private void ActivateSelectedSaveSlot()
        {
            if (gameState == null)
            {
                return;
            }

            if (selectedRowIndex < 0)
            {
                return;
            }

            ShowSaveActionModal(selectedRowIndex);
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
                ActivatePartyDetailAction(activeMembers[selectedRowIndex]);
                return;
            }

            var inactiveMembers = party.InactiveMembers.ToList();
            var inactiveIndex = selectedRowIndex - activeMembers.Count;
            if (inactiveIndex >= 0 && inactiveIndex < inactiveMembers.Count)
            {
                ActivatePartyDetailAction(inactiveMembers[inactiveIndex]);
            }
        }

        private void ActivatePartyDetailAction(Hero hero)
        {
            if (hero == null)
            {
                return;
            }

            switch (currentPartyDetailTab)
            {
                case PartyDetailTab.Items:
                    ShowPartyItemPicker(hero);
                    break;
                case PartyDetailTab.Spells:
                    ShowPartySpellPicker(hero);
                    break;
                default:
                    ShowPartyMemberActionModal(hero);
                    break;
            }
        }

        private void ShowPartyMemberActionModal(Hero hero)
        {
            var party = GetParty();
            if (party == null || hero == null)
            {
                return;
            }

            var activeMembers = party.ActiveMembers.ToList();
            var choices = new List<string>();
            var actions = new List<Action>();
            if (hero.IsActive)
            {
                var activeIndex = activeMembers.IndexOf(hero);
                if (activeIndex > 0)
                {
                    choices.Add("Up");
                    actions.Add(() =>
                    {
                        ApplyPartyChange(() => gameState.MovePartyMemberUp(hero));
                        selectedRowIndex = Mathf.Max(0, selectedRowIndex - 1);
                    });
                }

                if (activeIndex >= 0 && activeIndex < activeMembers.Count - 1)
                {
                    choices.Add("Down");
                    actions.Add(() =>
                    {
                        ApplyPartyChange(() => gameState.MovePartyMemberDown(hero));
                        selectedRowIndex = Mathf.Min(Math.Max(GetSelectableRowCount() - 1, 0), selectedRowIndex + 1);
                    });
                }

                if (activeMembers.Count > 1)
                {
                    choices.Add("Reserve");
                    actions.Add(() =>
                    {
                        ApplyPartyChange(() => gameState.DeactivatePartyMember(hero));
                        selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(GetSelectableRowCount() - 1, 0));
                    });
                }
            }
            else if (activeMembers.Count < GetMaxPartyMembers())
            {
                choices.Add("Add To Party");
                actions.Add(() =>
                {
                    ApplyPartyChange(() => gameState.ActivatePartyMember(hero));
                    selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, Math.Max(GetSelectableRowCount() - 1, 0));
                });
            }

            choices.Add("Cancel");
            ShowMenuModal(hero.Name, "Choose an action.", choices, selectedIndex =>
            {
                if (selectedIndex >= 0 && selectedIndex < actions.Count)
                {
                    actions[selectedIndex]();
                }
            });
        }

        private void ShowPartyItemPicker(Hero hero)
        {
            if (hero == null || hero.Items == null || hero.Items.Count == 0)
            {
                ShowPartyMessage("No items.");
                return;
            }

            var labels = hero.Items.Select(item => item.NameWithStats + (item.IsEquipped ? " [E]" : "")).ToList();
            labels.Add("Cancel");
            ShowMenuModal(hero.Name + " Items", "Choose an item.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= hero.Items.Count)
                {
                    return;
                }

                selectedPartyItemIndex = selectedIndex;
                ShowPartyItemActionModal(hero, hero.Items[selectedIndex]);
            });
        }

        private void ShowPartyItemActionModal(Hero hero, ItemInstance item)
        {
            if (hero == null || item == null)
            {
                return;
            }

            var choices = new List<string>();
            var actions = new List<Action>();
            if (gameState != null && CanUseItemFromInventory(hero, item))
            {
                choices.Add("Use");
                actions.Add(() => ShowUseItemTargetPicker(hero, item));
            }

            if (item.IsEquipped)
            {
                choices.Add("Unequip");
                actions.Add(() => ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item)));
            }
            else if (hero.CanEquipItem(item))
            {
                choices.Add("Equip");
                actions.Add(() => ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item)));
            }

            if (HasTransferTarget(hero))
            {
                choices.Add("Transfer");
                actions.Add(() => ShowTransferItemTargetPicker(hero, item));
            }

            if (item.Type != ItemType.Quest)
            {
                choices.Add("Drop");
                actions.Add(() => ShowDropItemConfirmation(hero, item));
            }

            choices.Add("Cancel");
            ShowMenuModal(item.Name, "Choose an action.", choices, selectedIndex =>
            {
                if (selectedIndex >= 0 && selectedIndex < actions.Count)
                {
                    actions[selectedIndex]();
                }
            });
        }

        private void ShowPartySpellPicker(Hero hero)
        {
            var spells = GameDataCache.Current == null || GameDataCache.Current.Spells == null
                ? new List<Spell>()
                : hero.GetSpells(GameDataCache.Current.Spells)
                    .Where(spell => CanCastSpellFromPartyMenu(hero, spell))
                    .ToList();
            if (spells.Count == 0)
            {
                ShowPartyMessage("No spells can be cast.");
                return;
            }

            var labels = spells.Select(spell => spell.Name).ToList();
            labels.Add("Cancel");
            ShowMenuModal(hero.Name + " Spells", "Choose a spell.", labels, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= spells.Count)
                {
                    return;
                }

                ShowSpellTargetPicker(hero, spells[selectedIndex]);
            });
        }

        private Hero GetSelectedInventoryHero()
        {
            var party = GetParty();
            if (party == null)
            {
                return null;
            }

            var members = GetInventoryMembers(party);
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

            var members = GetInventoryMembers(party);
            selectedHeroIndex = (selectedHeroIndex + delta + members.Count) % members.Count;
            selectedRowIndex = 0;
        }

        private static List<Hero> GetInventoryMembers(Party party)
        {
            return party == null
                ? new List<Hero>()
                : party.Members.OrderBy(member => member.IsActive ? 0 : 1).ThenBy(member => member.Order).ToList();
        }

        private void ActivateSelectedInventoryItem()
        {
            var hero = GetSelectedInventoryHero();
            if (hero == null || selectedRowIndex < 0 || selectedRowIndex >= hero.Items.Count)
            {
                return;
            }

            var item = hero.Items[selectedRowIndex];
            if (gameState != null && gameState.CanUseHeroItem(hero, item))
            {
                ShowUseItemTargetPicker(hero, item);
            }
            else if (item.IsEquipped)
            {
                ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
            }
            else if (hero.CanEquipItem(item))
            {
                ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
            }
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

        private int GetMenuMoveX()
        {
            var pressed = InputManager.GetMoveXDown();
            if (pressed != 0)
            {
                repeatingMenuMoveX = pressed;
                nextMenuMoveXTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = InputManager.GetMoveX();
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
            var moveX = InputManager.GetMoveX();
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
            var pressed = InputManager.GetMoveYDown();
            if (pressed != 0)
            {
                repeatingMenuMoveY = pressed;
                nextMenuMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = InputManager.GetMoveY();
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
            ApplySettings(settings, false);
        }

        private void ApplySettings(Settings settings, bool refreshMap)
        {
            SettingsCache.Set(settings);
            ScheduleSettingsSave();
            DisplaySettings.Apply(settings);
            UiSettings.GetOrCreate().ApplySettings(settings);
            Audio.GetOrCreate().ApplySettings(settings);
            lastThemeSignature = null;
            if (!refreshMap)
            {
                return;
            }

            if (mapView == null)
            {
                mapView = FindAnyObjectByType<View>();
            }

            if (mapView != null)
            {
                mapView.RefreshRender();
            }
        }

        private void ApplyAudioSettings(Settings settings)
        {
            SettingsCache.Set(settings);
            Audio.GetOrCreate().ApplySettings(settings);
            ScheduleSettingsSave();
        }

        private void ScheduleSettingsSave()
        {
            pendingSettingsSave = true;
            pendingSettingsSaveTime = Time.unscaledTime + 0.5f;
        }

        private void UpdateDeferredSettingsSave()
        {
            if (!pendingSettingsSave || Time.unscaledTime < pendingSettingsSaveTime)
            {
                return;
            }

            pendingSettingsSave = false;
            SettingsCache.Save();
        }

        private static string GetThemeValue(string value, string fallback)
        {
            return UiTheme.GetThemeValue(value, fallback);
        }

        private static GUIStyle GetRarityStyle(ItemInstance item, GUIStyle baseStyle)
        {
            var style = new GUIStyle(baseStyle);
            if (item == null)
            {
                return style;
            }

            style.normal.textColor = GetRarityColor(item.Rarity, baseStyle.normal.textColor);
            style.hover.textColor = style.normal.textColor;
            style.active.textColor = style.normal.textColor;
            style.focused.textColor = style.normal.textColor;
            return style;
        }

        private static Color GetRarityColor(Rarity rarity, Color commonColor)
        {
            switch (rarity)
            {
                case Rarity.Uncommon:
                    return Color.green;
                case Rarity.Rare:
                    return Color.blue;
                case Rarity.Epic:
                    return new Color(0.55f, 0f, 0.75f);
                case Rarity.Legendary:
                    return new Color(1f, 0.5f, 0f);
                case Rarity.Common:
                default:
                    return commonColor;
            }
        }

        private static int GetBorderThickness(Settings settings)
        {
            return UiTheme.GetBorderThickness(settings);
        }

        private static int GetMaxPartyMembers()
        {
            return SettingsCache.Current == null || SettingsCache.Current.MaxPartyMembers <= 0
                ? 4
                : SettingsCache.Current.MaxPartyMembers;
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
                gameState = GameState.GetOrCreate();
            }

            if (messageBox == null)
            {
                messageBox = FindAnyObjectByType<MessageBox>();
                if (messageBox == null)
                {
                    messageBox = new GameObject("MessageBox").AddComponent<MessageBox>();
                }
            }

            if (uiSettings == null)
            {
                uiSettings = UiSettings.GetOrCreate();
            }

            if (mapView == null)
            {
                mapView = FindAnyObjectByType<View>();
            }
        }

        private void PrewarmUiAssets()
        {
            if (uiAssetsPrewarmed)
            {
                return;
            }

            EnsureReferences();
            UiAssetResolver.Preload(gameState == null ? null : gameState.Party);
            uiAssetsPrewarmed = true;
        }

        private MessageBox GetMessageBox()
        {
            EnsureReferences();
            return messageBox;
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = SettingsCache.Current;
            var themeSignature = UiTheme.GetSignature(settings);
            if (titleStyle != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            uiTheme = UiTheme.Create(settings, scale);
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
                uiSettings = UiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }
    }
}
