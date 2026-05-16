using Redpoint.DungeonEscape.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.ViewModels;
using UnityEngine;

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu : MonoBehaviour
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

        private enum MenuScreen
        {
            Main,
            Items,
            Spells,
            Equipment,
            Abilities,
            Status,
            Quests,
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

        private readonly GameMenuViewModel viewModel = new GameMenuViewModel();
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
        private Vector2 partyScrollPosition;
        private Vector2 inventoryScrollPosition;
        private Vector2 questScrollPosition;
        private Vector2 saveScrollPosition;
        private Vector2 settingsScrollPosition;
        private float menuContentHeight;
        private float menuBodyHeight;
        private int drawingRowIndex;
        private InputBinding rebindingInput;
        private string rebindingSlot;
        private int rebindingStartFrame;
        private Action<int> menuModalSelected;
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

        private MenuScreen currentScreen
        {
            get { return (MenuScreen)viewModel.CurrentScreen; }
            set { viewModel.SetCurrentScreen((int)value); }
        }

        private MenuScreen previousScreen
        {
            get { return (MenuScreen)viewModel.PreviousScreen; }
            set { viewModel.SetPreviousScreen((int)value); }
        }

        private MenuFocus currentFocus
        {
            get { return (MenuFocus)viewModel.CurrentFocus; }
            set { viewModel.SetCurrentFocus((int)value); }
        }

        private MenuTab currentTab
        {
            get { return (MenuTab)viewModel.CurrentTab; }
            set { viewModel.SetCurrentTab((int)value); }
        }

        private SettingsTab currentSettingsTab
        {
            get { return (SettingsTab)viewModel.CurrentSettingsTab; }
            set { viewModel.SetCurrentSettingsTab((int)value); }
        }

        private int selectedHeroIndex
        {
            get { return viewModel.SelectedHeroIndex; }
            set { viewModel.SetSelectedHeroIndex(value); }
        }

        private int selectedPartyItemIndex
        {
            get { return viewModel.SelectedPartyItemIndex; }
            set { viewModel.SetSelectedPartyItemIndex(value); }
        }

        private int selectedDetailIndex
        {
            get { return viewModel.SelectedDetailIndex; }
            set { viewModel.SetSelectedDetailIndex(value); }
        }

        private int selectedEquipmentItemIndex
        {
            get { return viewModel.SelectedEquipmentItemIndex; }
            set { viewModel.SetSelectedEquipmentItemIndex(value); }
        }

        private int selectedMainActionIndex
        {
            get { return viewModel.SelectedMainActionIndex; }
            set { viewModel.SetSelectedMainActionIndex(value); }
        }

        private int selectedPreviousScreenRowIndex
        {
            get { return viewModel.SelectedPreviousScreenRowIndex; }
            set { viewModel.SetSelectedPreviousScreenRowIndex(value); }
        }

        private int detailPageIndex
        {
            get { return viewModel.DetailPageIndex; }
            set { viewModel.SetDetailPageIndex(value); }
        }

        private int selectedRowIndex
        {
            get { return viewModel.SelectedRowIndex; }
            set { viewModel.SetSelectedRowIndex(value); }
        }

        private int selectedBindingSlotIndex
        {
            get { return viewModel.SelectedBindingSlotIndex; }
            set { viewModel.SetSelectedBindingSlotIndex(value); }
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
            menuContentHeight = areaHeight;
            menuBodyHeight = Mathf.Max(160f * scale, areaHeight);
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
            menuContentHeight = Screen.height;
            menuBodyHeight = Screen.height;
            var previousEnabled = GUI.enabled;
            var previousMenuControlsBlocked = menuControlsBlocked;
            menuControlsBlocked = rebindingInput != null || IsMenuModalVisible();
            SetMenuGuiEnabled(previousEnabled);
            var actions = currentScreen == MenuScreen.Misc ? MiscActionScreen.GetActions() : MainActionScreen.GetActions();
            if (currentScreen == MenuScreen.Main)
            {
                viewModel.ClampSelectedMainActionIndex(actions.Count);
            }

            viewModel.ClampSelectedRowIndex(actions.Count);
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

        private void DrawCurrentTab()
        {
            drawingRowIndex = 0;
            viewModel.ClampSelectedRowIndex(GetSelectableRowCount());
            switch (currentScreen)
            {
                case MenuScreen.Main:
                    MainActionScreen.Draw();
                    break;
                case MenuScreen.Items:
                    MemberDetailScreen.Draw("Items", DrawMenuItemsList, DrawSelectedMenuItemDetail);
                    break;
                case MenuScreen.Spells:
                    MemberDetailScreen.Draw("Spells", DrawMenuSpellsList, DrawSelectedMenuSpellDetail);
                    break;
                case MenuScreen.Abilities:
                    MemberDetailScreen.Draw("Abilities", DrawMenuAbilitiesList, DrawSelectedMenuAbilityDetail);
                    break;
                case MenuScreen.Equipment:
                    MemberDetailScreen.Draw("Equipment", DrawMenuEquipmentList, DrawSelectedMenuEquipmentDetail);
                    break;
                case MenuScreen.Status:
                    MemberDetailScreen.Draw("Status", null, DrawSelectedMenuStatusDetail);
                    break;
                case MenuScreen.Quests:
                    QuestScreen.Draw();
                    break;
                case MenuScreen.Party:
                    MemberDetailScreen.Draw("Party", null, DrawSelectedMenuStatusDetail);
                    break;
                case MenuScreen.Misc:
                    MiscActionScreen.Draw();
                    break;
                case MenuScreen.Save:
                    SaveScreen.Draw();
                    break;
                case MenuScreen.Load:
                    LoadScreen.Draw();
                    break;
                case MenuScreen.Settings:
                    SettingsScreen.Draw();
                    break;
            }
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

            viewModel.ClampSelectedDetailIndex(items.Count);
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

            viewModel.ClampSelectedDetailIndex(spells.Count);
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

            viewModel.ClampSelectedDetailIndex(skills.Count);
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
            viewModel.ClampSelectedDetailIndex(slots.Count);
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
            viewModel.ClampSelectedDetailIndex(items.Count);
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

            viewModel.ClampSelectedEquipmentItemIndex(candidates.Count);
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
            viewModel.ClampSelectedDetailIndex(count);
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
                var rowIndex = i;
                HandleDetailRowMouseClick(rowIndex, () => clicked(rowIndex));
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
            viewModel.ClampSelectedDetailIndex(spells.Count);
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
                var rowIndex = i;
                HandleDetailRowMouseClick(rowIndex, () =>
                {
                    selectedDetailIndex = rowIndex;
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

        private void DrawPartyStatusDetail(Hero hero)
        {
            if (hero == null)
            {
                return;
            }

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

        private void DrawProgressValue(string label, int value, int maxValue, string text, Color fillColor)
        {
            DrawProgressValue(label, (ulong)Math.Max(0, value), (ulong)Math.Max(0, maxValue), text, fillColor);
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
            var spells = GetKnownSpells(hero);
            var skills = GetKnownSkills(hero);
            if (spells.Count == 0 && skills.Count == 0)
            {
                return;
            }

            GUILayout.Space(8f * GetPixelScale());
            GUILayout.BeginHorizontal();
            if (spells.Count > 0)
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.Label("Spells", labelStyle);
                DrawStatusSpellList(spells);
                GUILayout.EndVertical();
                GUILayout.Space(12f * GetPixelScale());
            }

            if (skills.Count > 0)
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.Label("Abilities", labelStyle);
                DrawStatusSkillList(skills);
                GUILayout.EndVertical();
            }

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

        private bool CanCastSpellFromPartyMenu(Hero caster, Spell spell)
        {
            return gameState != null &&
                   spell != null &&
                   viewModel.CanCastSpellFromPartyMenu(
                       gameState.CanCastHeroSpell(caster, spell),
                       spell.Type,
                       gameState.CanCastOutside(),
                       gameState.CanCastReturn());
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

            switch (viewModel.GetCastSpellAction(spell))
            {
                case GameMenuUseAction.Outside:
                    ShowPartyMessage(gameState.CastOutsideSpell(caster, spell));
                    return;
                case GameMenuUseAction.Return:
                    ShowReturnLocationPicker(caster, spell);
                    return;
                case GameMenuUseAction.Open:
                case GameMenuUseAction.Object:
                    ShowPartyMessage(CastSpellOnFacingObject(caster, spell));
                    return;
                case GameMenuUseAction.Group:
                    ShowPartyMessage(gameState.CastHeroSpellOnParty(caster, spell));
                    return;
                case GameMenuUseAction.NoTarget:
                    ShowPartyMessage(gameState.CastHeroSpellWithoutTarget(caster, spell));
                    return;
                case GameMenuUseAction.Single:
                    ShowSingleSpellTargetPicker(caster, spell);
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
                case MenuScreen.Spells:
                case MenuScreen.Abilities:
                case MenuScreen.Equipment:
                    return viewModel.GetCurrentDetailCount(
                        (int)currentScreen,
                        hero,
                        GetKnownSpells(hero).Count,
                        GetKnownSkills(hero).Count);
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
            return viewModel.GetMenuMembers(party, (int)currentScreen, CanUseMapSpells, CanUseMapSkills);
        }

        private bool AnyMemberHasUsableMapSpells()
        {
            var party = GetParty();
            return viewModel.AnyMemberMatches(party, CanUseMapSpells);
        }

        private bool AnyMemberHasUsableMapAbilities()
        {
            var party = GetParty();
            return viewModel.AnyMemberMatches(party, CanUseMapSkills);
        }

        private bool CanManagePartyMembers()
        {
            var party = GetParty();
            return viewModel.CanManagePartyMembers(party);
        }

        private List<Slot> GetEquipmentSlots()
        {
            return viewModel.GetEquipmentSlots();
        }

        private List<ItemInstance> GetEquipmentCandidates(Hero hero, Slot slot)
        {
            return viewModel.GetEquipmentCandidates(hero, slot);
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
            viewModel.ClampSelectedEquipmentItemIndex(updatedCandidates.Count);
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

        private ItemInstance GetEquippedItem(Hero hero, Slot slot)
        {
            return viewModel.GetEquippedItem(hero, slot);
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

            switch (viewModel.GetUseItemAction(item))
            {
                case GameMenuUseAction.Outside:
                    ShowInventoryMessage(gameState.UseOutsideItem(hero, item));
                    ClampSelectedItemIndex(hero);
                    return;
                case GameMenuUseAction.Return:
                    ShowReturnLocationPicker(hero, item);
                    return;
                case GameMenuUseAction.Open:
                case GameMenuUseAction.Object:
                    ShowInventoryMessage(UseItemOnFacingObject(hero, item));
                    ClampSelectedItemIndex(hero);
                    return;
                case GameMenuUseAction.Group:
                    ShowInventoryMessage(gameState.UseHeroItemOnParty(hero, item));
                    ClampSelectedItemIndex(hero);
                    return;
                case GameMenuUseAction.NoTarget:
                    ShowInventoryMessage(gameState.UseHeroItem(hero, item, hero));
                    ClampSelectedItemIndex(hero);
                    return;
                case GameMenuUseAction.Single:
                    ShowSingleItemTargetPicker(hero, item);
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
                viewModel.ClampSelectedRowIndex(maxIndex + 1);
            }
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
                viewModel.ClampSelectedRowIndex(gameState.GetManualSaveSlots().Count + 1);
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
            return GameSaveFormatter.GetTitle(save);
        }

        private static bool IsUsableSave(GameSave save)
        {
            return GameSaveFormatter.IsUsableSave(save);
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

            ShowPartyMemberActionModal(hero);
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
                viewModel.ClampSelectedRowIndex(GetSelectableRowCount());
                    });
                }
            }
            else if (activeMembers.Count < GetMaxPartyMembers())
            {
                choices.Add("Add To Party");
                actions.Add(() =>
                {
                    ApplyPartyChange(() => gameState.ActivatePartyMember(hero));
            viewModel.ClampSelectedRowIndex(GetSelectableRowCount());
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

        private void ShowPartyItemActionModal(Hero hero, ItemInstance item)
        {
            if (hero == null || item == null)
            {
                return;
            }

            var choices = viewModel.GetPartyItemActionLabels(
                gameState != null && CanUseItemFromInventory(hero, item),
                item,
                hero.CanEquipItem(item),
                HasTransferTarget(hero));
            ShowMenuModal(item.Name, "Choose an action.", choices, selectedIndex =>
            {
                if (selectedIndex < 0 || selectedIndex >= choices.Count)
                {
                    return;
                }

                switch (choices[selectedIndex])
                {
                    case "Use":
                        ShowUseItemTargetPicker(hero, item);
                        break;
                    case "Unequip":
                        ApplyInventoryChange(() => gameState.UnequipHeroItem(hero, item));
                        break;
                    case "Equip":
                        ApplyInventoryChange(() => gameState.EquipHeroItem(hero, item));
                        break;
                    case "Transfer":
                        ShowTransferItemTargetPicker(hero, item);
                        break;
                    case "Drop":
                        ShowDropItemConfirmation(hero, item);
                        break;
                }
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

        private List<Hero> GetInventoryMembers(Party party)
        {
            return viewModel.GetInventoryMembers(party);
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

        private float GetMenuContentHeight()
        {
            return menuContentHeight > 0f ? menuContentHeight : menuBodyHeight;
        }
    }
}
